namespace MapObjects.Library.Transformer;

public interface ITileToGeoTransformer
{
    /// <summary>
    /// Преобразовать тайловые координаты (x,y) в (lon, lat) для Redis GEO
    /// </summary>
    (double lon, double lat) ToLonLat(double x, double y);

    /// <summary>
    /// Обратное преобразование
    /// </summary>
    (double x, double y) FromLonLat(double lon, double lat);
}