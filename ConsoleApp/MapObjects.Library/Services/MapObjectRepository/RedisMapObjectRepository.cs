using System.Text.Json;
using MapObjects.Library.Models;
using MapObjects.Library.Transformer;
using StackExchange.Redis;

namespace MapObjects.Library.Services.MapObjectRepository;

public class RedisMapObjectRepository : IMapObjectRepository
{
    private readonly IDatabase _db;
    private readonly ISubscriber _sub;
    private readonly ITileToGeoTransformer _transform;
    private const string GeoKeyPrefix = "geo:map:";
    private const string ObjKeyPrefix = "obj:";
    private const string PubChannelPrefix = "channel:map:objects:";

    private const int MaxRetries = 3;
    private readonly TimeSpan _baseDelay = TimeSpan.FromMilliseconds(50);

    public RedisMapObjectRepository(IConnectionMultiplexer redis, ITileToGeoTransformer transform)
    {
        var redis1 = redis ?? throw new ArgumentNullException(nameof(redis));
        _db = redis1.GetDatabase();
        _sub = redis1.GetSubscriber();
        _transform = transform;
    }

    private static string GeoKey(string mapId) => $"{GeoKeyPrefix}{mapId}:objects";
    private static string ObjKey(string mapId, string objId) => $"{ObjKeyPrefix}{mapId}:{objId}";
    private static string PubChannel(string mapId) => $"{PubChannelPrefix}{mapId}";

    public async Task AddOrUpdateAsync(string mapId, MapObject obj, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(obj);
        var (cx, cy) = obj.CenterInTiles();
        var (lon, lat) = _transform.ToLonLat(cx, cy);

        var objKey = ObjKey(mapId, obj.Id);
        var geoKey = GeoKey(mapId);

        var json = JsonSerializer.Serialize(obj);

        await RetryAsync(async () =>
        {
            await _db.StringSetAsync(objKey, json);
            await _db.GeoAddAsync(geoKey, lon, lat, obj.Id);

            return true;
        }, ct);
        await PublishEventAsync(mapId, "created_or_updated", obj, ct);
    }

    public async Task<MapObject?> GetByIdAsync(string mapId, string id, CancellationToken ct = default)
    {
        var key = ObjKey(mapId, id);
        return await RetryAsync(async () =>
        {
            var val = await _db.StringGetAsync(key);
            return !val.HasValue ? null : JsonSerializer.Deserialize<MapObject>(val!);
        }, ct);
    }

    public async Task<bool> DeleteAsync(string mapId, string id, CancellationToken ct = default)
    {
        var key = ObjKey(mapId, id);
        var geoKey = GeoKey(mapId);
        var deleted = false;
        await RetryAsync(async () =>
        {
            var tran = _db.CreateTransaction();
            _ = tran.KeyDeleteAsync(key);
            _ = tran.SortedSetRemoveAsync(geoKey, id);
            deleted = await tran.ExecuteAsync();
            return true;
        }, ct);

        await PublishEventAsync(mapId, "deleted", new MapObject { Id = id }, ct);
        return deleted;
    }

    public async Task<IReadOnlyList<MapObject>> GetObjectsInRectangleAsync(string mapId, int x, int y, int width,
        int height, CancellationToken ct = default)
    {
        // Преобразовать прямоугольник в географический прямоугольник
        // центр + радиус, охватывающий прямоугольник для грубого поиска кандидатов
        var cxRect = x + width / 2.0;
        var cyRect = y + height / 2.0;
        var (lonCenter, latCenter) = _transform.ToLonLat(cxRect, cyRect);

        // вычислить диагональ в плитках -> приблизительные метры?
        // В Redis GEO используются градусы, но единицы измерения расстояния преобразуются с помощью аргументов
        var corners = new[]
        {
            _transform.ToLonLat(x, y),
            _transform.ToLonLat(x + width, y),
            _transform.ToLonLat(x, y + height),
            _transform.ToLonLat(x + width, y + height),
        };
        double maxDegDist = 0;
        foreach (var (lon, lat) in corners)
        {
            var dd = DistanceDeg(lonCenter, latCenter, lon, lat);
            if (dd > maxDegDist) maxDegDist = dd;
        }

        // Redis GEORADIUS требует измерения в метрах; преобразем градусы -> метры приблизительно: 1 град ~ 111_000 м
        var radiusMeters = maxDegDist * 111000.0;

        var geoKey = GeoKey(mapId);

        var candidateIds = Array.Empty<RedisValue>();
        await RetryAsync(async () =>
        {
            var entries = await _db.GeoRadiusAsync(geoKey, lonCenter, latCenter, radiusMeters);
            candidateIds = entries.Select(e => e.Member).ToArray();
            return true;
        }, ct);

        var result = new List<MapObject>();
        foreach (var idVal in candidateIds)
        {
            var obj = await GetByIdAsync(mapId, idVal!, ct);
            if (obj == null) continue;
            if (obj.IntersectsRect(x, y, width, height)) result.Add(obj);
        }

        return result;
    }

    public async Task<MapObject?> GetObjectAtTileAsync(string mapId, int tileX, int tileY,
        CancellationToken ct = default)
    {
        // запрашиваемые точки: центр плитки
        var (lon, lat) = _transform.ToLonLat(tileX + 0.5, tileY + 0.5);

        var (lonTile, latTile) = _transform.ToLonLat(tileX, tileY + 0.5);
        var (lonTilePlus1, latTilePlus1) = _transform.ToLonLat(tileX + 1, tileY + 0.5);

        var degHor = Math.Abs(lonTilePlus1 - lonTile);
        var degVer = Math.Abs(latTilePlus1 - latTile);

        var oneTileDeg = Math.Sqrt(degHor * degHor + degVer * degVer);

        // радиус поиска в плитках — для надежности используем значение чуть больше 1 = безопаснее 2.0
        const double radiusTiles = 2.0;
        var radiusMeters = Math.Max(1.0, oneTileDeg * radiusTiles * 111000.0);

        var geoKey = GeoKey(mapId);
        var candidateIds = Array.Empty<RedisValue>();

        await RetryAsync(async () =>
        {
            var entries = await _db.GeoRadiusAsync(geoKey, lon, lat, radiusMeters, GeoUnit.Meters);
            candidateIds = entries.Select(e => e.Member).ToArray();
            return true;
        }, ct);

        foreach (var idVal in candidateIds)
        {
            var obj = await GetByIdAsync(mapId, idVal!, ct);
            if (obj == null) continue;
            if (obj.ContainsPoint(tileX, tileY)) return obj;
        }

        return null;
    }

    public async Task PublishEventAsync(string mapId, string eventType, MapObject obj, CancellationToken ct = default)
    {
        var channel = PubChannel(mapId);
        var payload = JsonSerializer.Serialize(new
        {
            type = eventType,
            objectId = obj.Id,
            obj
        });

        await RetryAsync(async () =>
        {
            var s2 = await _sub.PublishAsync(channel, payload);
            return true;
        }, ct);
    }

    private static double DistanceDeg(double lon1, double lat1, double lon2, double lat2)
    {
        var dx = lon1 - lon2;
        var dy = lat1 - lat2;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private async Task<T> RetryAsync<T>(Func<Task<T>> func, CancellationToken ct)
    {
        var attempt = 0;
        Exception? lastEx = null;
        while (attempt < MaxRetries)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                return await func();
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                lastEx = ex;
                attempt++;
                var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                await Task.Delay(delay, ct);
            }
        }

        throw lastEx ?? new InvalidOperationException("Retry failed");
    }

    private static bool IsTransient(Exception ex)
    {
        return ex is RedisTimeoutException or RedisConnectionException or TimeoutException;
    }
}