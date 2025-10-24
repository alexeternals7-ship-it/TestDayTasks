using MapObjects.Library;
using MapObjects.Library.Events;

namespace ConsoleApp.Controllers;

public class MapObjectServiceAdapter : IMapObjectEventSource
{
    private readonly MapObjectService _mapObjectService;
    public event EventHandler<MapObjectChangedEventArgs>? OnObjectChanged;

    public MapObjectServiceAdapter(MapObjectService mapObjectService)
    {
        _mapObjectService = mapObjectService ?? throw new ArgumentNullException(nameof(mapObjectService));
        _mapObjectService.OnObjectChanged += (s, e) => OnObjectChanged?.Invoke(s, e);
    }
}