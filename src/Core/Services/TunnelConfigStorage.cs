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
    OutcomeAsync<Unit> Init();

    OutcomeAsync<Seq<TunnelConfig>> All();
    OutcomeAsync<TunnelConfig>      Add(TunnelConfig config);
    OutcomeAsync<TunnelConfig>      Update(TunnelConfig config);
    OutcomeAsync<TunnelConfig>      Delete(Guid configId);

    IObservable<Change<TunnelConfig>> Changes { get; }
}

public class TunnelConfigStorage(ILogger logger, IUniqueId uniqueId) : ITunnelConfigStorage
{
    ConcurrentDictionary<Guid, TunnelConfig> inMemoryStorage = new();
    readonly Subject<Change<TunnelConfig>> changes = new();

    public IObservable<Change<TunnelConfig>> Changes => changes;

    public OutcomeAsync<Unit> Init() {
        logger.Information("Initializing storage...");
        return from _ in GetStore().use(Load)
                       | ifFail(AppErrors.InvalidData, e => {
                                                           logger.Error(e, "Data corrupted! Use new storage");
                                                           return Seq.empty<TunnelConfig>();
                                                       })
                       | @do<Seq<TunnelConfig>>(SaveInMemory)
               select unit;

        Unit SaveInMemory(Seq<TunnelConfig> configs) {
            var loadData = from config in configs select KeyValuePair.Create(config.Id!.Value, config);
            inMemoryStorage = new(loadData);
            logger.Information("Storage initialized");
            return unit;
        }
    }

    public OutcomeAsync<Seq<TunnelConfig>> All() =>
        inMemoryStorage.Values.ToSeq();

    public OutcomeAsync<TunnelConfig> Add(TunnelConfig config) =>
        Update(config);

    public OutcomeAsync<TunnelConfig> Update(TunnelConfig config) =>
        ChangeState(() => {
                        var existed = inMemoryStorage.Get(config.Id!.Value);
                        inMemoryStorage[config.Id!.Value] = config;
                        var message = existed.Match(o => Change<TunnelConfig>.Mapped(o, config),
                                                    () => Change<TunnelConfig>.Added(config));
                        changes.OnNext(message);
                        return config;
                    });

    public OutcomeAsync<TunnelConfig> Delete(Guid configId) =>
        ChangeState(() => {
                        var existed = inMemoryStorage.Remove(configId, out var v) ? Some(v) : None;
                        existed.Iter(item => changes.OnNext(Change<TunnelConfig>.Removed(item)));
                        return existed.ToOutcome(StandardErrors.NotFoundFromKey(configId.ToString()));
                    });

    OutcomeAsync<T> ChangeState<T>(Func<Outcome<T>> action) =>
        from result in action()
        from _ in GetStore().use(Save)
        select result;

    OutcomeAsync<T> ChangeState<T>(Func<T> action) =>
        from result in TryCatch(action)
        from _ in GetStore().use(Save)
        select result;

    static IsolatedStorageFile GetStore() => IsolatedStorageFile.GetUserStoreForApplication();

    OutcomeAsync<Seq<TunnelConfig>> Load(IsolatedStorageFile store) =>
        from file in OpenFile(store, FileMode.OpenOrCreate)
        from ret in file.use(Load)
        select ret;

    OutcomeAsync<Seq<TunnelConfig>> Load(Stream dataFile) =>
        from reader in SuccessOutcome(new StreamReader(dataFile))
        from data in TryCatch(async () => {
                                  var d = await reader.ReadToEndAsync();
                                  return string.IsNullOrEmpty(d) ? Seq.empty<TunnelConfig>() : DeserializeFromOldFormat(d);
                              })
        select data;

    Outcome<Seq<TunnelConfig>> DeserializeFromOldFormat(string data) =>
        from configs in TryDeserialize(data)
        let sanitized = from config in configs
                        select config.Id == Guid.Empty
                                   ? config with { Id = uniqueId.NewGuid() }
                                   : config
        select sanitized;

    static Outcome<Seq<TunnelConfig>> TryDeserialize(string data) =>
        TryCatch(() => JsonSerializer.Deserialize<TunnelConfig[]>(data).ToSeq())
           .Catch(e => AppErrors.InvalidData.WithMessage($"Cannot deserialize data:\n\nError: {e.Message}\n\nData:\n{data}"));

    OutcomeAsync<Unit> Save(IsolatedStorageFile store) =>
        from file in OpenFile(store, FileMode.Create)
        from ret in file.use(Save)
        select ret;

    OutcomeAsync<Unit> Save(Stream dataFile) =>
        TryCatch(async () => {
                     var writer = new StreamWriter(dataFile);
                     if (TrySerialize(inMemoryStorage.Values.ToSeq()).IfFail(out var error, out var data))
                         return FailedOutcome<Unit>(error);

                     Console.WriteLine($"Position {dataFile.Position}");
                     await writer.WriteAsync(data);
                     await writer.FlushAsync();
                     return unit;
                 });

    static Outcome<string> TrySerialize(Seq<TunnelConfig> data) =>
        TryCatch(() => JsonSerializer.Serialize(data));

    static Outcome<IsolatedStorageFileStream> OpenFile(IsolatedStorageFile store, FileMode mode) =>
        TryCatch(() => store.OpenFile("ssh-manager.json", mode));
}