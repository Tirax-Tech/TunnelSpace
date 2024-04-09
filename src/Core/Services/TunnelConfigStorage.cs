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
    readonly ILogger logger;
    readonly IUniqueId uniqueId;
    ConcurrentDictionary<Guid, TunnelConfig> inMemoryStorage = new();
    readonly Subject<Change<TunnelConfig>> changes = new();

    static readonly Eff<IsolatedStorageFile> GetStore = Eff(IsolatedStorageFile.GetUserStoreForApplication);

    public TunnelConfigStorage(ILogger logger, IUniqueId uniqueId) {
        this.logger = logger;
        this.uniqueId = uniqueId;
        All = Eff(() => inMemoryStorage.Values.ToSeq());

        var init =
            from configs in use(GetStore, Load)
            let loadData = from config in configs select KeyValuePair.Create(config.Id, config)
            from _ in Eff(() => inMemoryStorage = new(loadData))
            select unit;

        var initLogging =
            from ______1 in logger.InformationEff("Initializing storage...")
            from logging in init.Match(logger.InformationEff("Storage initialized"),
                                       ex => logger.ErrorEff(ex, "Failed to initialize storage"))
            from ______2 in logging
            select unit;

        initLogging.RunUnit();
    }

    public IObservable<Change<TunnelConfig>> Changes => changes;

    public Aff<Seq<TunnelConfig>> All { get; }

    public Aff<TunnelConfig> Add(TunnelConfig config) =>
        from _1 in eff(() => {
            inMemoryStorage[config.Id] = config;
            changes.OnNext(Change<TunnelConfig>.Added(config));
        })
        from _2 in use(GetStore, Save).IfFailAff(e => logger.ErrorEff(e, "Failed to save data"))
        select config;

    static Eff<Seq<TunnelConfig>> TryDeserialize(string data) =>
        Eff(() => JsonSerializer.Deserialize<TunnelConfig[]>(data).ToSeq());

    static Eff<string> TrySerialize(Seq<TunnelConfig> data) =>
        Eff(() => JsonSerializer.Serialize(data));

    Eff<Seq<TunnelConfig>> DeserializeFromOldFormat(string data) =>
        from configs in TryDeserialize(data)
        let sanitized = from config in configs
                        select config.Id == Guid.Empty
                                   ? from newId in uniqueId.NewGuid
                                     select config with {Id = newId}
                                   : SuccessEff(config)
        from final in sanitized.Sequence()
        select final;

    Eff<Seq<TunnelConfig>> Load(Stream dataFile) =>
        from reader in StreamEff.NewStreamReader(dataFile)
        from data in reader.ReadToEndEff()
        from result in string.IsNullOrEmpty(data)
                           ? SuccessEff(Seq.empty<TunnelConfig>())
                           : DeserializeFromOldFormat(data)
        select result;

    Aff<Unit> Save(Stream dataFile) =>
        from writer in StreamEff.NewStreamWriter(dataFile)
        from data in TrySerialize(inMemoryStorage.Values.ToSeq())
        from _1 in writer.WriteAsyncEff(data)
        from _2 in writer.FlushAsyncEff()
        select unit;

    static Eff<IsolatedStorageFileStream> OpenFile(IsolatedStorageFile store, FileMode mode) =>
        store.OpenFileEff("ssh-manager.json", mode);

    Eff<Seq<TunnelConfig>> Load(IsolatedStorageFile store) =>
        use(OpenFile(store, FileMode.OpenOrCreate), Load);

    Aff<Unit> Save(IsolatedStorageFile store) =>
        use(OpenFile(store, FileMode.Create), Save);
}