using MapObjects.Library.Models;

namespace MapObjects.Library.Events;

public class MapObjectChangedEventArgs : EventArgs
{
    public string MapId { get; init; }= "";
    public string EventType { get; init; } = "";
    public MapObject? Object { get; init; }
}