using MapObjects.Library.Models;

namespace MapObjects.Library.Services.MapObjectRepository;

public interface IMapObjectRepository
{
    Task AddOrUpdateAsync(string mapId, MapObject obj, CancellationToken ct = default);
    Task<MapObject?> GetByIdAsync(string mapId, string id, CancellationToken ct = default);
    Task<bool> DeleteAsync(string mapId, string id, CancellationToken ct = default);
    Task<IReadOnlyList<MapObject>> GetObjectsInRectangleAsync(string mapId, int x, int y, int width, int height, CancellationToken ct = default);
    Task<MapObject?> GetObjectAtTileAsync(string mapId, int tileX, int tileY, CancellationToken ct = default);
    Task PublishEventAsync(string mapId, string eventType, MapObject obj, CancellationToken ct = default);
}