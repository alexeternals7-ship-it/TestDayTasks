namespace Region.Library;

public sealed class RegionsLayer
{
    private readonly int _mapWidth;
    private readonly int _mapHeight;
    private readonly uint[] _regionIds;
    private readonly RegionMeta[] _regionsByIndex; 
    private readonly Dictionary<uint, RegionMeta> _regionsById;
    
    public IReadOnlyCollection<RegionMeta> Regions => _regionsByIndex;


    private RegionsLayer(int mapWidth, int mapHeight, uint[] regionIds, RegionMeta[] regions)
    {
        _mapWidth = mapWidth;
        _mapHeight = mapHeight;
        _regionIds = regionIds ?? throw new ArgumentNullException(nameof(regionIds));
        _regionsByIndex = regions ?? throw new ArgumentNullException(nameof(regions));
        _regionsById = new Dictionary<uint, RegionMeta>(_regionsByIndex.Length);
        foreach (var r in _regionsByIndex) _regionsById[r.Id] = r;
    }

    public static RegionsLayer Generate(int mapWidth, int mapHeight, Func<int, string>? nameProvider = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(mapWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(mapHeight);
        nameProvider ??= (i) => $"Region #{i}";

        var widthDivs = GetDivisors(mapWidth);
        var heightDivs = GetDivisors(mapHeight);

        var bestCols = 1;
        var bestRows = 1;
        var bestDiff = int.MaxValue;
        foreach (var cols in widthDivs)
        {
            foreach (var rows in heightDivs)
            {
                var regionW = mapWidth / cols;
                var regionH = mapHeight / rows;
                var diff = Math.Abs(regionW - regionH);
                
                if (diff >= bestDiff) continue;
                
                bestDiff = diff;
                bestCols = cols;
                bestRows = rows;
            }
        }

        var regionWidth = mapWidth / bestCols;
        var regionHeight = mapHeight / bestRows;

        var regions = new List<RegionMeta>(bestCols * bestRows);
        uint idCounter = 1;
        for (var ry = 0; ry < bestRows; ry++)
        {
            for (var cx = 0; cx < bestCols; cx++)
            {
                var x = cx * regionWidth;
                var y = ry * regionHeight;
                var bounds = new Rect(x, y, regionWidth, regionHeight);
                regions.Add(new RegionMeta(idCounter, nameProvider((int)idCounter), bounds));
                idCounter++;
            }
        }

        var tileIds = new uint[mapWidth * mapHeight];
        foreach (var r in regions)
        {
            for (var yy = r.Bounds.Y; yy < r.Bounds.Y + r.Bounds.Height; yy++)
            {
                for (var xx = r.Bounds.X; xx < r.Bounds.X + r.Bounds.Width; xx++)
                {
                    tileIds[yy * mapWidth + xx] = r.Id;
                }
            }
        }

        return new RegionsLayer(mapWidth, mapHeight, tileIds, regions.ToArray());
    }
    
    public uint GetRegionId(int x, int y)
    {
        if ((uint)x >= (uint)_mapWidth || (uint)y >= (uint)_mapHeight)
            throw new ArgumentOutOfRangeException("Tile out of bounds");
        return _regionIds[y * _mapWidth + x];
    }
    
    public RegionMeta GetRegionMeta(uint id)
    {
        if (_regionsById.TryGetValue(id, out var m)) return m;
        throw new KeyNotFoundException($"Region id {id} not found");
    }

    public bool TileBelongsToRegion(int x, int y, uint regionId)
    {
        return GetRegionId(x, y) == regionId;
    }
    
    public IReadOnlyList<RegionMeta> GetRegionsIntersecting(Rect query)
    {
        return _regionsByIndex.Where(r => r.Bounds.Intersects(query)).ToList();
    }
    
    private static List<int> GetDivisors(int n)
    {
        var res = new List<int>();
        for (var i = 1; i <= n; i++)
        {
            if (n % i == 0)
                res.Add(i);
        }

        return res;
    }
}