namespace Region.Library;

public readonly struct Rect
{
    public readonly int X;
    public readonly int Y;
    public readonly int Width;
    public readonly int Height;
    public int Right => X + Width - 1;
    public int Bottom => Y + Height - 1;

    public Rect(int x, int y, int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }


    public bool Intersects(in Rect other)
    {
        return !(other.X > Right || other.Right < X || other.Y > Bottom || other.Bottom < Y);
    }


    public bool Contains(int x, int y)
    {
        return x >= X && x <= Right && y >= Y && y <= Bottom;
    }
}