using MapObjects.Library.Events;

namespace ConsoleApp.Controllers;

public interface IMapObjectEventSource
{
    event EventHandler<MapObjectChangedEventArgs>? OnObjectChanged;
}