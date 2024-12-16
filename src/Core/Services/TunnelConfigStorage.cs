using System.Collections.Concurrent;
using System.IO.IsolatedStorage;
using System.Reactive.Subjects;
using System.Text.Json;
using RZ.Foundation.Types;
using Serilog;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.Helpers;

namespace Tirax.TunnelSpace.Services;

public interface ITunnelConfigStorage
{
    Task Init();

    IEnumerable<TunnelConfig> All();
    Task<TunnelConfig>      Add(TunnelConfig config);
    Task<TunnelConfig>      Update(TunnelConfig config);
    Task<TunnelConfig>      Delete(Guid configId);

    IObservable<Change<TunnelConfig>> Changes { get; }
}

public class TunnelConfigStorage(ILogger logger, IUniqueId uniqueId) : ITunnelConfigStorage
{
    ConcurrentDictionary<Guid, TunnelConfig> inMemoryStorage = new();
    readonly Subject<Change<TunnelConfig>> changes = new();

    public IObservable<Change<TunnelConfig>> Changes => changes;

    public async Task Init() {
        logger.Information("Initializing storage...");
        using var store = GetStore();
        var (error, result) = await Try(store, Load);
        if (error is ErrorInfoException { Code: AppErrors.InvalidData }){
            logger.Error(error, "Data corrupted! Use new storage");
            result = [];
            SaveInMemory(result);
        }
        else if (error is not null)
            throw new ErrorInfoException(AppErrors.InvalidData, "Invalid storage file data", innerException: error);
        return;

        void SaveInMemory(IEnumerable<TunnelConfig> configs) {
            var loadData = from config in configs select KeyValuePair.Create(config.Id!.Value, config);
            inMemoryStorage = new(loadData);
            logger.Information("Storage initialized");
        }
    }

    public IEnumerable<TunnelConfig> All() =>
        inMemoryStorage.Values;

    public Task<TunnelConfig> Add(TunnelConfig config) =>
        Update(config);

    public async Task<TunnelConfig> Update(TunnelConfig config) {
        try{
            var existed = inMemoryStorage.Get(config.Id!.Value);
            inMemoryStorage[config.Id!.Value] = config;
            var message = existed.Match(o => Change<TunnelConfig>.Mapped(o, config),
                                        () => Change<TunnelConfig>.Added(config));
            changes.OnNext(message);
            return config;
        }
        finally{
            await Save();
        }
    }

    public async Task<TunnelConfig> Delete(Guid configId) {
        try{
            if (inMemoryStorage.Remove(configId, out var v)){
                changes.OnNext(Change<TunnelConfig>.Removed(v));
                return v;
            }
            else
                throw new ErrorInfoException(StandardErrorCodes.NotFound, $"Config with id {configId} not found");
        }
        finally{
            await Save();
        }
    }

    static IsolatedStorageFile GetStore() => IsolatedStorageFile.GetUserStoreForApplication();

    async Task<IEnumerable<TunnelConfig>> Load(IsolatedStorageFile store) {
        await using var file = OpenFile(store, FileMode.OpenOrCreate);
        return await Load(file);
    }

    async Task<IEnumerable<TunnelConfig>> Load(Stream dataFile) {
        var reader = new StreamReader(dataFile);
        var d = await reader.ReadToEndAsync();
       return string.IsNullOrEmpty(d) ? [] : DeserializeFromOldFormat(d);
    }

    IEnumerable<TunnelConfig> DeserializeFromOldFormat(string data) {
        var configs = TryDeserialize(data);
        return from config in configs
               select config.Id == Guid.Empty
                          ? config with { Id = uniqueId.NewGuid() }
                          : config;
    }

    static TunnelConfig[] TryDeserialize(string data) {
        try{
            return JsonSerializer.Deserialize<TunnelConfig[]>(data) ?? throw new ErrorInfoException(AppErrors.InvalidData, "Invalid storage file data");
        }
        catch (Exception e){
            throw new ErrorInfoException(AppErrors.InvalidData, "Invalid storage file data", innerException: e);
        }
    }

    async Task Save() {
        using var store = GetStore();
        await Save(store);
    }

    async Task Save(IsolatedStorageFile store) {
        await using var file = OpenFile(store, FileMode.Create);
        await Save(file);
    }

    async Task Save(Stream dataFile) {
        var writer = new StreamWriter(dataFile);
        var data = TrySerialize(inMemoryStorage.Values);

        Console.WriteLine($"Position {dataFile.Position}");
        await writer.WriteAsync(data);
        await writer.FlushAsync();
    }

    static string TrySerialize(IEnumerable<TunnelConfig> data)
        => Catch(data, d => JsonSerializer.Serialize(d), AppErrors.InvalidData, "Invalid file data structure");

    static IsolatedStorageFileStream OpenFile(IsolatedStorageFile store, FileMode mode) =>
        store.OpenFile("ssh-manager.json", mode);
}