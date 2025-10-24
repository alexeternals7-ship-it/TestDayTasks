using MagicOnion;

namespace ConsoleApp;

public interface IMapHub : IStreamingHub<IMapHub, IMapHubReceiver>
{
    Task JoinGroupAsync(string groupName);
    Task LeaveGroupAsync(string groupName);
}