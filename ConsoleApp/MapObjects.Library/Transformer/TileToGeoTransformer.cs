namespace MapObjects.Library.Transformer;

public class TileToGeoTransformer : ITileToGeoTransformer
{
    private readonly double _lon0, _lat0, _lonSpan, _latSpan;
    private readonly double _mapWidth, _mapHeight;

    /// <summary>
    /// mapWidth/mapHeight: ширина/высота карты в тайлах
    /// lon0/lat0 — левый верхний угол карты в геокоординатах
    /// lonSpan/latSpan — насколько в градусах покрывает карта по долготе и широте
    /// </summary>
    public TileToGeoTransformer(double mapWidth, double mapHeight,
        double lon0, double lat0,
        double lonSpan = 0.1, double latSpan = 0.1)
    {
        if (mapWidth <= 0 || mapHeight <= 0) throw new ArgumentException("map size positive");
        _mapWidth = mapWidth;
        _mapHeight = mapHeight;
        _lon0 = lon0;
        _lat0 = lat0;
        _lonSpan = lonSpan;
        _latSpan = latSpan;
    }

    public (double lon, double lat) ToLonLat(double x, double y)
    {
        var u = x / _mapWidth; 
        var v = y / _mapHeight;
        var lon = _lon0 + u * _lonSpan;
        var lat = _lat0 - v * _latSpan;
        return (lon, lat);
    }

    public (double x, double y) FromLonLat(double lon, double lat)
    {
        var u = (lon - _lon0) / _lonSpan;
        var v = (_lat0 - lat) / _latSpan;
        var x = u * _mapWidth;
        var y = v * _mapHeight;
        return (x, y);
    }
}