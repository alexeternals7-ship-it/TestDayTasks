using Map.Library.Enums;

namespace Map.Library;

public class TileLayer : IReadOnlyTileLayer
{
    private readonly ulong[] _bits;
    public int Width { get; }
    public int Height { get; }

    public TileLayer(int width, int height)
    {
        int count;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        Width = width;
        Height = height;
        checked
        {
            count = width * height;
        }

        var words = (count + 63) >> 6;
        _bits = new ulong[words];
    }
    
    public static TileLayer FromArray(int width, int height, TileType[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length != width * height) throw new ArgumentException("Length mismatch", nameof(data));
        
        var layer = new TileLayer(width, height);
        for (var i = 0; i < data.Length; i++)
        {
            if (data[i] == TileType.Mountain)
                layer.SetBitByIndex(i, true);
        }

        return layer;
    }
    
    public void SetTileType(int x, int y, TileType type)
    {
        var idx = GetIndexChecked(x, y);
        SetBitByIndex(idx, type == TileType.Mountain);
    }
    
    public TileType GetTileType(int x, int y)
    {
        var idx = GetIndexChecked(x, y);
        return GetBitByIndex(idx) ? TileType.Mountain : TileType.Plain;
    }
    
    public bool CanPlaceObject(int x, int y)
    {
        return GetTileType(x, y) == TileType.Plain;
    }
    
    public bool CanPlaceObjectInArea(int x, int y, int w, int h)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(w);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(h);
        if (x < 0 || y < 0 || x + w > Width || y + h > Height)
            throw new ArgumentOutOfRangeException("Area out of bounds");
        
        for (var i = y; i < y + h; i++)
        {
            var baseIdx = i * Width + x;
            var end = baseIdx + w;
            for (var idx = baseIdx; idx < end; idx++)
            {
                if (GetBitByIndex(idx)) 
                    return false;
            }
        }

        return true;
    }
    
    public void FillArea(int x, int y, int w, int h, TileType type)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(w);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(h);
        if (x < 0 || y < 0 || x + w > Width || y + h > Height)
            throw new ArgumentOutOfRangeException("Area out of bounds");

        var setToOne = type == TileType.Mountain;

        for (var i = y; i < y + h; i++)
        {
            var baseIdx = i * Width + x;
            var end = baseIdx + w;
            for (var idx = baseIdx; idx < end; idx++)
            {
                SetBitByIndex(idx, setToOne);
            }
        }
    }
    
    public IReadOnlyTileLayer AsReadOnly() => new ReadOnlyTileLayer(this);
    
    public long GetEstimatedMemoryUsageBytes() => (long)_bits.Length * sizeof(ulong);
    
    
    private void SetBitByIndex(int idx, bool value)
    {
        var word = idx >> 6; // idx / 64
        var bit = idx & 63;
        var mask = 1UL << bit;
        if (value)
        {
            Interlocked.Or(ref _bits[word], mask);
        }
        else
        {
            _bits[word] &= ~mask;
        }
    }

    private bool GetBitByIndex(int idx)
    {
        var word = idx >> 6;
        var bit = idx & 63;
        return ((_bits[word] >> bit) & 1UL) != 0;
    }

    private int GetIndexChecked(int x, int y)
    {
        if ((uint)x >= (uint)Width) throw new ArgumentOutOfRangeException(nameof(x));
        if ((uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(nameof(y));
        return y * Width + x;
    }

}