using MemoryPack;

namespace ConsoleApp.Contracts;

[MemoryPackable]
public partial class GetObjectsInAreaResponse
{
    public MapObjectDto[] Objects { get; set; } = [];
}