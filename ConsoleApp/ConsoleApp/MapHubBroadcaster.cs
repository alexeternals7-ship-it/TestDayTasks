using ConsoleApp.Contracts;

namespace ConsoleApp;

public class MapHubBroadcaster : IMapBroadcaster
{
    public Task BroadcastAddedAsync(string groupName, ObjectEventPayload payload)
    {
        if (MapHub.TryGetGroup(groupName, out var group) && group != null)
        {
            return MapHub.BroadcastAdded(group, payload);
        }
        return Task.CompletedTask;
    }

    public Task BroadcastUpdatedAsync(string groupName, ObjectEventPayload payload)
    {
        if (MapHub.TryGetGroup(groupName, out var group) && group != null)
        {
            return MapHub.BroadcastUpdated(group, payload);
        }
        return Task.CompletedTask;
    }

    public Task BroadcastDeletedAsync(string groupName, ObjectEventPayload payload)
    {
        if (MapHub.TryGetGroup(groupName, out var group) && group != null)
        {
            return MapHub.BroadcastDeleted(group, payload);
        }
        return Task.CompletedTask;
    }
}