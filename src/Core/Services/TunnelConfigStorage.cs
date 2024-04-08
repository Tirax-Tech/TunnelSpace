using System.Collections.Concurrent;
using System.IO.IsolatedStorage;
using System.Reactive.Subjects;
using System.Text.Json;
using Serilog;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.EffHelpers;
using Seq = LanguageExt.Seq;

namespace Tirax.TunnelSpace.Services;

public interface ITunnelConfigStorage
{
    Aff<Seq<TunnelConfig>> All { get; }
    Aff<TunnelConfig> Add(TunnelConfig config);

    IObservable<Change<TunnelConfig>> Changes { get; }
}

public class TunnelConfigStorage : ITunnelConfigStorage
{
    readonly IUniqueId uniqueId;
    ConcurrentDictionary<Guid, TunnelConfig> inMemoryStorage = new();
    readonly Subject<Change<TunnelConfig>> changes = new();

    public TunnelConfigStorage(ILogger logger, IUniqueId uniqueId) {
        this.uniqueId = uniqueId;
        All = Eff(() => inMemoryStorage.Values.ToSeq());

        var init =
            from configs in use(Eff(IsolatedStorageFile.GetUserStoreForApplication), Load)
            let loadData = from config in configs
                           select KeyValuePair.Create(config.Id, config)
            from _ in Eff(() => inMemoryStorage = new(loadData))
            select unit;

        var initLogging =
            from ______1 in logger.InformationEff("Initializing storage...")
            from logging in init.Match(logger.InformationEff("Storage initialized"),
                                       ex => logger.ErrorEff(ex, "Failed to initialize storage"))
            from ______2 in logging
            select unit;

        initLogging.IgnoreAsync();
    }

    public IObservable<Change<TunnelConfig>> Changes => changes;

    public Aff<Seq<TunnelConfig>> All { get; }

    public Aff<TunnelConfig> Add(TunnelConfig config) =>
        Eff(() => {
            inMemoryStorage[config.Id] = config;
            changes.OnNext(Change<TunnelConfig>.Added(config));
            return config;
        });

    static Eff<Seq<TunnelConfig>> TryDeserialize(string data) =>
        Eff(() => JsonSerializer.Deserialize<Seq<TunnelConfig>>(data));

    Eff<Seq<TunnelConfig>> DeserializeFromOldFormat(string data) =>
        from configs in TryDeserialize(data)
        let sanitized = from config in configs
                        select config.Id == Guid.Empty
                                   ? from newId in uniqueId.NewGuid
                                     select config with {Id = newId}
                                   : SuccessEff(config)
        from final in sanitized.Sequence()
        select final;

    Aff<Seq<TunnelConfig>> Load(Stream dataFile) =>
        from data in Aff(new StreamReader(dataFile).ReadToEndAsync().ToValue)
        from result in string.IsNullOrEmpty(data)
                           ? SuccessEff(Seq.empty<TunnelConfig>())
                           : DeserializeFromOldFormat(data)
        select result;

    Aff<Seq<TunnelConfig>> Load(IsolatedStorageFile store) =>
        use(store.OpenFileEff("ssh-manager.json", FileMode.OpenOrCreate),
            Load);
}