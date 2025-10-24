namespace Region.Library;

public class RegionMeta(uint id, string name, Rect bounds)
{
    public readonly uint Id = id;
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
    public Rect Bounds { get; } = bounds;
}