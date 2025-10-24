using System.Collections.Concurrent;
using ConsoleApp.Contracts;
using MagicOnion.Server.Hubs;

namespace ConsoleApp;

public class MapHub : StreamingHubBase<IMapHub, IMapHubReceiver>, IMapHub
{
    private static readonly ConcurrentDictionary<string, IGroup<IMapHubReceiver>> GroupsByName = new();

    // Клиент вызывает: JoinGroupAsync("map:default")
    public async Task JoinGroupAsync(string groupName)
    {
        // AddAsync возвращает IGroup<IMapHubReceiver>
        var group = await Group.AddAsync(groupName).ConfigureAwait(false);

        // сохраняем ссылку на группу в словаре
        GroupsByName.TryAdd(groupName, group);
    }

    // Клиент вызывает: LeaveGroupAsync("map:default")
    public async Task LeaveGroupAsync(string groupName)
    {
        // чтобы удалить текущий connection из группы, получаем group (через AddAsync можно получить ту же группу)
        var group = await Group.AddAsync(groupName).ConfigureAwait(false);

        // удаляем конкретный ConnectionId из группы
        await group.RemoveAsync(Context).ConfigureAwait(false);
    }

    public static Task BroadcastAdded(IGroup<IMapHubReceiver> group, ObjectEventPayload payload)
    {
        try
        {
            ((dynamic)group).ObjectAdded(payload);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"BroadcastAdded failed: {ex}");
        }

        return Task.CompletedTask;
    }

    public static Task BroadcastUpdated(IGroup<IMapHubReceiver> group, ObjectEventPayload payload)
    {
        try
        {
            ((dynamic)group).ObjectUpdated(payload);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"BroadcastUpdated failed: {ex}");
        }

        return Task.CompletedTask;
    }

    public static Task BroadcastDeleted(IGroup<IMapHubReceiver> group, ObjectEventPayload payload)
    {
        try
        {
            ((dynamic)group).ObjectDeleted(payload);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"BroadcastDeleted failed: {ex}");
        }

        return Task.CompletedTask;
    }
    
    public static bool TryGetGroup(string groupName, out IGroup<IMapHubReceiver>? group)
    {
        return GroupsByName.TryGetValue(groupName, out group);
    }
}