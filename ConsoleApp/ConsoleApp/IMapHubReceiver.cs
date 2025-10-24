using ConsoleApp.Contracts;

namespace ConsoleApp;

public interface IMapHubReceiver
{
    void ObjectAdded(ObjectEventPayload payload);
    void ObjectUpdated(ObjectEventPayload payload);
    void ObjectDeleted(ObjectEventPayload payload);
}