using System.Collections.Concurrent;
using System.IO.IsolatedStorage;
using System.Reactive.Subjects;
using System.Text.Json;
using RZ.Foundation.Functional;
using Serilog;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.Helpers;
using Seq = LanguageExt.Seq;

namespace Tirax.TunnelSpace.Services;

public interface ITunnelConfigStorage
{
    Aff<Unit> Init();

    Aff<Seq<TunnelConfig>> All();
    Aff<TunnelConfig>      Add(TunnelConfig config);
    Aff<TunnelConfig>      Update(TunnelConfig config);
    Aff<TunnelConfig>      Delete(Guid configId);

    IObservable<Change<TunnelConfig>> Changes { get; }
}

public class TunnelConfigStorage(ILogger logger, IUniqueId uniqueId) : ITunnelConfigStorage
{
    ConcurrentDictionary<Guid, TunnelConfig> inMemoryStorage = new();
    readonly Subject<Change<TunnelConfig>> changes = new();

    public IObservable<Change<TunnelConfig>> Changes => changes;

    public Aff<Unit> Init() {
        return from _1 in eff(() => logger.Information("Initializing storage..."))
               from data in use(GetStore, Load)
                          | @catch(AppErrors.InvalidData, e => {
                                                              logger.Error(e, "Data corrupted! Use new storage");
                                                              return Seq.empty<TunnelConfig>();
                                                          })
               let _ = SaveInMemory(data)
               select unit;

        Unit SaveInMemory(Seq<TunnelConfig> configs) {
            var loadData = from config in configs select KeyValuePair.Create(config.Id!.Value, config);
            inMemoryStorage = new(loadData);
            logger.Information("Storage initialized");
            return unit;
        }
    }

    public Aff<Seq<TunnelConfig>> All() =>
        SuccessAff(inMemoryStorage.Values.ToSeq());

    public Aff<TunnelConfig> Add(TunnelConfig config) =>
        Update(config);

    public Aff<TunnelConfig> Update(TunnelConfig config) =>
        ChangeState(() => {
                        var existed = inMemoryStorage.Get(config.Id!.Value);
                        inMemoryStorage[config.Id!.Value] = config;
                        var message = existed.Match(o => Change<TunnelConfig>.Mapped(o, config),
                                                    () => Change<TunnelConfig>.Added(config));
                        changes.OnNext(message);
                        return config;
                    });

    public Aff<TunnelConfig> Delete(Guid configId) =>
        ChangeState(() => {
                        var existed = inMemoryStorage.Remove(configId, out var v) ? Some(v) : None;
                        existed.Iter(item => changes.OnNext(Change<TunnelConfig>.Removed(item)));
                        return existed.ToEff(StandardErrors.NotFoundFromKey(configId.ToString()));
                    });

    Aff<T> ChangeState<T>(Func<Eff<T>> action) =>
        from result in action()
        from _ in use(GetStore, Save)
        select result;

    Aff<T> ChangeState<T>(Func<T> action) =>
        from result in Eff(action)
        from _ in use(GetStore, Save)
        select result;

    static readonly Eff<IsolatedStorageFile> GetStore = Eff(IsolatedStorageFile.GetUserStoreForApplication);

    Aff<Seq<TunnelConfig>> Load(IsolatedStorageFile store) =>
        from file in SuccessEff(OpenFile(store, FileMode.OpenOrCreate))
        from ret in use(file, Load) | @catch(StandardErrors.NotFound, Seq.empty<TunnelConfig>())
        select ret;

    Aff<Seq<TunnelConfig>> Load(Stream dataFile) =>
        from reader in SuccessEff(new StreamReader(dataFile))
        from content in Aff(async () => await reader.ReadToEndAsync())
        from ____ in guardnot(string.IsNullOrEmpty(content), StandardErrors.NotFound)
        from data in DeserializeFromOldFormat(content)
        select data;

    Eff<Seq<TunnelConfig>> DeserializeFromOldFormat(string data) =>
        from configs in TryDeserialize(data)
        let sanitized = from config in configs
                        select config.Id == Guid.Empty
                                   ? config with { Id = uniqueId.NewGuid() }
                                   : config
        select sanitized;

    static Eff<Seq<TunnelConfig>> TryDeserialize(string data) =>
        Eff(() => JsonSerializer.Deserialize<TunnelConfig[]>(data).ToSeq())
      | @catchOf<Error>(e => AppErrors.InvalidData.WithMessage($"Cannot deserialize data:\n\nError: {e.Message}\n\nData:\n{data}"));

    Aff<Unit> Save(IsolatedStorageFile store) =>
        from file in SuccessEff(OpenFile(store, FileMode.Create))
        from ____ in use(file, Save)
        select unit;

    Aff<Unit> Save(Stream dataFile) =>
        from writer in SuccessEff(new StreamWriter(dataFile))
        from data in TrySerialize(inMemoryStorage.Values.ToSeq())
        from ____ in Aff(async () => {
                             await writer.WriteAsync(data);
                             await writer.FlushAsync();
                             return unit;
                         })
        select unit;

    static Eff<string> TrySerialize(Seq<TunnelConfig> data) =>
        Eff(() => JsonSerializer.Serialize(data));

    static Eff<IsolatedStorageFileStream> OpenFile(IsolatedStorageFile store, FileMode mode) =>
        Eff(() => store.OpenFile("ssh-manager.json", mode));
}