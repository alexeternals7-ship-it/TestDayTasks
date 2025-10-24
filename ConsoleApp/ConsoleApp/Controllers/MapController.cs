using ConsoleApp.Contracts;
using ConsoleApp.Mappings;
using MapObjects.Library;
using MapObjects.Library.Events;
using MapObjects.Library.Models;
using MapObjects.Library.Services.MapObjectRepository;

namespace ConsoleApp.Controllers;

public class MapController : IDisposable
{
    private readonly IMapObjectRepository _repo;
    private readonly IMapObjectEventSource? _eventSource;
    private readonly IMapBroadcaster _broadcaster;
    private readonly string _mapId;

    private readonly string mapGroupName = "map:";

    public MapController(IMapObjectRepository repo, IMapBroadcaster broadcaster,
        IMapObjectEventSource? eventSource = null, string mapId = "default")
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _broadcaster = broadcaster ?? throw new ArgumentNullException(nameof(broadcaster));
        _eventSource = eventSource;
        _mapId = mapId;

        if (_eventSource != null)
        {
            _eventSource.OnObjectChanged += HandleExternalObjectChanged;
        }
    }

    public async Task AddOrUpdateObjectAsync(MapObject obj)
    {
        await _repo.AddOrUpdateAsync(_mapId, obj).ConfigureAwait(false);
        var payload = obj.ToEventPayload("added");
        await _broadcaster.BroadcastAddedAsync($"{mapGroupName}{_mapId}", payload).ConfigureAwait(false);
    }

    public async Task<bool> DeleteObjectAsync(string id)
    {
        var existed = await _repo.GetByIdAsync(_mapId, id).ConfigureAwait(false);
        var deleted = await _repo.DeleteAsync(_mapId, id).ConfigureAwait(false);
        if (deleted)
        {
            var payload = existed?.ToEventPayload("deleted") ?? new ObjectEventPayload
                { Id = id, EventType = "deleted" };
            await _broadcaster.BroadcastDeletedAsync($"{mapGroupName}{_mapId}", payload).ConfigureAwait(false);
        }

        return deleted;
    }

    public void Dispose()
    {
        if (_eventSource != null) _eventSource.OnObjectChanged -= HandleExternalObjectChanged;
    }

    private void HandleExternalObjectChanged(object? s, MapObjectChangedEventArgs e)
    {
        if (e.MapId != _mapId) return;

        var payload = new ObjectEventPayload
        {
            Id = e.Object?.Id ?? "",
            Object = e.Object?.ToDto(),
            EventType = e.EventType
        };

        _ = e.EventType.ToLowerInvariant() switch
        {
            "created" or "created_or_updated" or "added" => _broadcaster.BroadcastAddedAsync($"{mapGroupName}{_mapId}", payload),
            "deleted" or "remove" or "removed" => _broadcaster.BroadcastDeletedAsync($"{mapGroupName}{_mapId}", payload),
            _ => _broadcaster.BroadcastUpdatedAsync($"{mapGroupName}{_mapId}", payload)
        };
    }
}