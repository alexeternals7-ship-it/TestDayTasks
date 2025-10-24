using MemoryPack;

namespace ConsoleApp.Contracts;

[MemoryPackable]
public partial class ObjectEventPayload
{
    public string Id { get; set; } = "";
    public MapObjectDto? Object { get; set; }
    public string EventType { get; set; } = "";
}