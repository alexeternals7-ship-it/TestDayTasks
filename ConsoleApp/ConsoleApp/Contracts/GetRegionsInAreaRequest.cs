using MemoryPack;

namespace ConsoleApp.Contracts;

[MemoryPackable]
public partial class GetRegionsInAreaRequest
{
    public RectRequest Area { get; set; } = new();
}