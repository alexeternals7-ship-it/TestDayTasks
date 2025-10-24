using System.Text.Json;
using MapObjects.Library.Events;
using MapObjects.Library.Models;
using MapObjects.Library.Services.MapObjectRepository;
using StackExchange.Redis;

namespace MapObjects.Library;

public class MapObjectService
{
    private readonly IMapObjectRepository _repo;
    private readonly ISubscriber _sub;
    
    public event EventHandler<MapObjectChangedEventArgs>? OnObjectChanged;

    public MapObjectService(IMapObjectRepository repo, IConnectionMultiplexer conn)
    {
        _repo = repo;
        _sub = conn.GetSubscriber();
    }

    public async Task AddOrUpdateAsync(string mapId, MapObject obj, CancellationToken ct = default)
    {
        await _repo.AddOrUpdateAsync(mapId, obj, ct);
    }

    public async Task<MapObject?> GetByIdAsync(string mapId, string id, CancellationToken ct = default)
    {
        return await _repo.GetByIdAsync(mapId, id, ct);
    }

    public async Task<bool> DeleteAsync(string mapId, string id, CancellationToken ct = default)
    {
        return await _repo.DeleteAsync(mapId, id, ct);
    }

    public async Task<MapObject?> GetObjectAtTileAsync(string mapId, int tx, int ty, CancellationToken ct = default)
        => await _repo.GetObjectAtTileAsync(mapId, tx, ty, ct);

    public async Task SubscribeToRedisEventsAsync(string mapId, CancellationToken ct = default)
    {
        var channel = $"channel:map:objects:{mapId}";
        await _sub.SubscribeAsync(channel, (ch, val) =>
        {
            try
            {
                if (val.IsNullOrEmpty)
                {
                    Console.Error.WriteLine($"[Subscribe] empty payload on {channel}");
                    return;
                }
                
                using var doc = JsonDocument.Parse((string)val);
                var root = doc.RootElement;

                string type;
                if (root.TryGetProperty("type", out var typeElem) && typeElem.ValueKind == JsonValueKind.String)
                {
                    type = typeElem.GetString() ?? "";
                }
                else
                {
                    Console.Error.WriteLine($"[Subscribe] message missing 'type' field on {channel}: {val}");
                    return;
                }

                MapObject? obj = null;
                if (root.TryGetProperty("obj", out var objElem) && objElem.ValueKind != JsonValueKind.Null)
                {
                    try
                    {
                        obj = objElem.Deserialize<MapObject>();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Subscribe] failed to deserialize 'obj' element: {ex}; payload={val}");
                    }
                }

                OnObjectChanged?.Invoke(this, new MapObjectChangedEventArgs
                {
                    MapId = mapId,
                    EventType = type,
                    Object = obj
                });
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine($"Failed to handle pub/sub message on {channel}: {ex}");
            }
        });
    }
}