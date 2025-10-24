using ConsoleApp.Contracts;
using MagicOnion;

namespace ConsoleApp;

public interface IMapService : IService<IMapService>
{
    UnaryResult<GetObjectsInAreaResponse> GetObjectsInArea(GetObjectsInAreaRequest req);
    UnaryResult<GetRegionsInAreaResponse> GetRegionsInArea(GetRegionsInAreaRequest req);
}