using ConsoleApp.Contracts;
using MapObjects.Library.Models;
using Region.Library;

namespace ConsoleApp.Mappings;

public static class DomainToDtoMapper
{
    public static MapObjectDto ToDto(this MapObject o)
    {
        return new MapObjectDto
        {
            Id = o.Id,
            X = o.X,
            Y = o.Y,
            Width = o.Width,
            Height = o.Height,
            Type = o.Type,
            MetadataJson = o.MetadataJson,
            UpdatedAtUnixMs = o.UpdatedAt.ToUnixTimeMilliseconds()
        };
    }

    public static ObjectEventPayload ToEventPayload(this MapObject? o, string eventType)
    {
        return new ObjectEventPayload
        {
            Id = o?.Id ?? "",
            Object = o?.ToDto(),
            EventType = eventType
        };
    }
    
    public static RegionDto ToDto(this RegionMeta r)
    {
        return new RegionDto
        {
            Id = r.Id,
            Name = r.Name,
            X = r.Bounds.X,
            Y = r.Bounds.Y,
            Width = r.Bounds.Width,
            Height = r.Bounds.Height
        };
    }
}