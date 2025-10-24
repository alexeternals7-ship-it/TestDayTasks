using MemoryPack;

namespace ConsoleApp.Contracts;

[MemoryPackable]
public partial class GetObjectsInAreaRequest
{
    public RectRequest Area { get; set; } = new();
}