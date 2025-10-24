using ConsoleApp.Contracts;
using ConsoleApp.Controllers;
using MapObjects.Library;
using MapObjects.Library.Events;
using MapObjects.Library.Models;
using MapObjects.Library.Services.MapObjectRepository;
using Moq;
using NUnit.Framework;

namespace ConsoleApp.Tests;

public class MapControllerTests
{
    [Test]
    public async Task AddOrUpdate_CallsRepoAndBroadcast()
    {
        // Assert
        var repoMock = new Mock<IMapObjectRepository>();
        var broadcasterMock = new Mock<IMapBroadcaster>();
        var mapController = new MapController(repoMock.Object, broadcasterMock.Object, null, "default");

        var mapObject = new MapObject { Id = "o1", X = 0, Y = 0, Width = 1, Height = 1 };
        repoMock.Setup(r => r.AddOrUpdateAsync("default", mapObject, CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        await mapController.AddOrUpdateObjectAsync(mapObject);

        // Assert
        repoMock.Verify(r => r.AddOrUpdateAsync("default", mapObject, CancellationToken.None), Times.Once);
        broadcasterMock.Verify(b => b.BroadcastAddedAsync("map:default", It.IsAny<ObjectEventPayload>()), Times.Once);
    }

    [Test]
    public async Task Delete_CallsRepoAndBroadcast()
    {
        // Assert
        var repoMock = new Mock<IMapObjectRepository>();
        var broadcasterMock = new Mock<IMapBroadcaster>();
        repoMock.Setup(r => r.GetByIdAsync("default", "o1", CancellationToken.None))
            .ReturnsAsync(new MapObject { Id = "o1" });
        repoMock.Setup(r => r.DeleteAsync("default", "o1", CancellationToken.None)).ReturnsAsync(true);
        var mapController = new MapController(repoMock.Object, broadcasterMock.Object, null, "default");

        // Act
        var deleted = await mapController.DeleteObjectAsync("o1");

        // Assert
        Assert.That(deleted, Is.True);
        broadcasterMock.Verify(b => b.BroadcastDeletedAsync("map:default", It.IsAny<ObjectEventPayload>()), Times.Once);
    }

    [Test]
    public void ExternalEvent_IsForwardedToBroadcaster()
    {
        // Assert
        var repoMock = new Mock<IMapObjectRepository>();
        var broadcasterMock = new Mock<IMapBroadcaster>();

        var eventSourceMock = new Mock<IMapObjectEventSource>();
        _ = new MapController(repoMock.Object, broadcasterMock.Object, eventSourceMock.Object, "default");

        var args = new MapObjectChangedEventArgs
            { MapId = "default", EventType = "created", Object = new MapObject { Id = "oX" } };

        // Act
        eventSourceMock.Raise(s => s.OnObjectChanged += null, eventSourceMock.Object, args);

        // Assert
        broadcasterMock.Verify(b => b.BroadcastAddedAsync(
                "map:default",
                It.Is<ObjectEventPayload>(p => p.Id == "oX")),
            Times.Once);
    }
}