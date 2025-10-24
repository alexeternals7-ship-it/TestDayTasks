using Map.Library.Enums;

namespace Map.Library;

public interface IReadOnlyTileLayer
{
    int Width { get; }
    int Height { get; }
    TileType GetTileType(int x, int y);
    void SetTileType(int x, int y, TileType type);
    void FillArea(int x, int y, int w, int h, TileType type);
    bool CanPlaceObject(int x, int y);
    bool CanPlaceObjectInArea(int x, int y, int w, int h);
    long GetEstimatedMemoryUsageBytes();
}