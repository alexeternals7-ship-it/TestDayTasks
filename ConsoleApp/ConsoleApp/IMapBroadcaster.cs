using ConsoleApp.Contracts;

namespace ConsoleApp;

public interface IMapBroadcaster
{
    Task BroadcastAddedAsync(string groupName, ObjectEventPayload payload);
    Task BroadcastUpdatedAsync(string groupName, ObjectEventPayload payload);
    Task BroadcastDeletedAsync(string groupName, ObjectEventPayload payload);
}