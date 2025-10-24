using Map.Library.Enums;

namespace Map.Library;

public sealed class ReadOnlyTileLayer(TileLayer inner) : IReadOnlyTileLayer
{
    private readonly TileLayer _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    public int Width => _inner.Width;
    public int Height => _inner.Height;
    public TileType GetTileType(int x, int y) => _inner.GetTileType(x, y);
    public void SetTileType(int x, int y, TileType type) => _inner.SetTileType(x, y, type);
    public void FillArea(int x, int y, int w, int h, TileType type) => _inner.FillArea(x, y, w, h, type);
    public bool CanPlaceObject(int x, int y) => _inner.CanPlaceObject(x, y);
    public bool CanPlaceObjectInArea(int x, int y, int w, int h) => _inner.CanPlaceObjectInArea(x, y, w, h);
    public long GetEstimatedMemoryUsageBytes() => _inner.GetEstimatedMemoryUsageBytes();
}