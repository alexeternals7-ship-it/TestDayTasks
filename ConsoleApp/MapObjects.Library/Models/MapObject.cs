namespace MapObjects.Library.Models;

public record MapObject
{
    public string Id { get; init; }

    // Координаты верхнего левого угла в тайлах (целые)
    public int X { get; init; }
    public int Y { get; init; }

    // Размеры в тайлах
    public int Width { get; init; }
    public int Height { get; init; }
    
    public string Type { get; init; } = "generic";
    public string MetadataJson { get; init; } = "{}";
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;

    // Возвращает центр объекта в тайлах (double)
    public (double cx, double cy) CenterInTiles()
    {
        var cx = X + (Width - 1) / 2.0;
        var cy = Y + (Height - 1) / 2.0;
        return (cx, cy);
    }

    // Проверка попадания точки (tileX, tileY) внутрь объекта включая границы
    public bool ContainsPoint(int tileX, int tileY)
    {
        return tileX >= X && tileX < X + Width && tileY >= Y && tileY < Y + Height;
    }

    // Проверка пересечения с областью (rect - x,y,w,h)
    public bool IntersectsRect(int rx, int ry, int rWidth, int rHeight)
    {
        int ax2 = X + Width, ay2 = Y + Height;
        int bx2 = rx + rWidth, by2 = ry + rHeight;
        var noOverlap = ax2 <= rx || bx2 <= X || ay2 <= ry || by2 <= Y;
        return !noOverlap;
    }
}