using ConsoleApp.Contracts;
using MapObjects.Library;
using MapObjects.Library.Models;
using MapObjects.Library.Services.MapObjectRepository;
using Moq;
using NUnit.Framework;
using Region.Library;

namespace ConsoleApp.Tests;

public class MapServiceTests
{
    [Test]
    public async Task GetObjectsInArea_ReturnsObjects()
    {
        // Assert
        var repoMock = new Mock<IMapObjectRepository>();
        var sample = new MapObject { Id = "o1", X = 2, Y = 2, Width = 2, Height = 2 };
        repoMock.Setup(r => r.GetObjectsInRectangleAsync(It.IsAny<string>(), 2, 2, 1, 1, CancellationToken.None))
            .ReturnsAsync(new List<MapObject> { sample });

        var regions = RegionsLayer.Generate(10, 10);
        var mapService = new MapService(repoMock.Object, regions, "default");

        var res = await mapService.GetObjectsInArea(new GetObjectsInAreaRequest
        {
            Area = new RectRequest
            {
                X1 = 2,
                Y1 = 2,
                X2 = 2,
                Y2 = 2
            }
        });

        // Act
        var result = res.Objects.Where(x => x.Id == "o1").ToList();
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task GetRegionsInArea_ReturnsRegions()
    {
        // Assert
        var repoMock = new Mock<IMapObjectRepository>();
        var regions = RegionsLayer.Generate(20, 10);
        var mapService = new MapService(repoMock.Object, regions, "default");
        
        // Act
        var res = await mapService.GetRegionsInArea(new GetRegionsInAreaRequest
        {
            Area = new RectRequest
            {
                X1 = 0,
                Y1 = 0,
                X2 = 19,
                Y2 = 9
            }
        });
        
        // Assert
        Assert.That(res.Regions, Is.Not.Empty);
    }
}