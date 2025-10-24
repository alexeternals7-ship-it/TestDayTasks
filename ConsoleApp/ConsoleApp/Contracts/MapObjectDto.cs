using MemoryPack;

namespace ConsoleApp.Contracts;

[MemoryPackable]
public partial class MapObjectDto
{
    public string Id { get; set; } = null!;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Type { get; set; } = "";
    public string MetadataJson { get; set; } = "{}";
    public long UpdatedAtUnixMs { get; set; }
}