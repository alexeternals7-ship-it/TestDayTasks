using ConsoleApp.Contracts;
using ConsoleApp.Mappings;
using MagicOnion;
using MagicOnion.Server;
using MapObjects.Library;
using MapObjects.Library.Services.MapObjectRepository;
using Region.Library;

namespace ConsoleApp;

public class MapService(IMapObjectRepository repo, RegionsLayer regionsLayer, string mapId = "default")
    : ServiceBase<IMapService>, IMapService
{
    private readonly IMapObjectRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    private readonly RegionsLayer _regionsLayer = regionsLayer ?? throw new ArgumentNullException(nameof(regionsLayer));

    public async UnaryResult<GetObjectsInAreaResponse> GetObjectsInArea(GetObjectsInAreaRequest req)
    {
        var minX = Math.Min(req.Area.X1, req.Area.X2);
        var minY = Math.Min(req.Area.Y1, req.Area.Y2);
        var maxX = Math.Max(req.Area.X1, req.Area.X2);
        var maxY = Math.Max(req.Area.Y1, req.Area.Y2);
        var width = maxX - minX + 1;
        var height = maxY - minY + 1;

        var objs = await _repo.GetObjectsInRectangleAsync(mapId, minX, minY, width, height).ConfigureAwait(false);
        var dto = objs.Select(o => o.ToDto()).ToArray();
        
        return new GetObjectsInAreaResponse { Objects = dto };
    }

    public UnaryResult<GetRegionsInAreaResponse> GetRegionsInArea(GetRegionsInAreaRequest req)
    {
        var minX = Math.Min(req.Area.X1, req.Area.X2);
        var minY = Math.Min(req.Area.Y1, req.Area.Y2);
        var maxX = Math.Max(req.Area.X1, req.Area.X2);
        var maxY = Math.Max(req.Area.Y1, req.Area.Y2);
        var width = maxX - minX + 1;
        var height = maxY - minY + 1;

        var query = new Rect(minX, minY, width, height);
        var regions = _regionsLayer.GetRegionsIntersecting(query);
        var dto = regions.Select(r => r.ToDto()).ToArray();
        
        return new UnaryResult<GetRegionsInAreaResponse>(new GetRegionsInAreaResponse { Regions = dto });
    }
}