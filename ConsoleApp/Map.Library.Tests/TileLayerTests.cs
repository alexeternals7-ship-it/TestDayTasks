using Map.Library.Enums;
using NUnit.Framework;

namespace Map.Library.Tests;

[TestFixture]
public class TileLayerTests
{
    [Test]
    public void CreateAndGetSet_Working()
    {
        // Arrange
        var layer = new TileLayer(10, 5);

        //Act
        //Assert
        Assert.That(10, Is.EqualTo(layer.Width));
        Assert.That(5, Is.EqualTo(layer.Height));

        layer.SetTileType(2, 3, TileType.Mountain);
        Assert.That(TileType.Mountain, Is.EqualTo(layer.GetTileType(2, 3)));
        Assert.That(layer.CanPlaceObject(2, 3), Is.False);

        layer.SetTileType(2, 3, TileType.Plain);
        Assert.That(TileType.Plain, Is.EqualTo(layer.GetTileType(2, 3)));
        Assert.That(layer.CanPlaceObject(2, 3), Is.True);
    }

    [Test]
    public void FromArray_CreatesCorrectly()
    {
        // Arrange
        const int width = 4;
        const int height = 4;
        var arr = new TileType[width * height];

        for (var y = 0; y < height; y++) arr[y * width + y] = TileType.Mountain;
        var layer = TileLayer.FromArray(width, height, arr);

        //Act
        //Assert
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var expected = x == y ? TileType.Mountain : TileType.Plain;
                Assert.That(expected, Is.EqualTo(layer.GetTileType(x, y)));
            }
        }
    }

    [Test]
    public void OutOfBounds_Throws()
    {
        // Arrange
        var layer = new TileLayer(3, 3);

        //Act
        //Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => layer.GetTileType(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => layer.GetTileType(0, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => layer.SetTileType(3, 0, TileType.Plain));
    }

    [Test]
    public void FillAreaAndCanPlaceInArea_Works()
    {
        // Arrange
        var layer = new TileLayer(10, 10);

        //Act
        layer.FillArea(2, 2, 4, 4, TileType.Mountain);

        //Assert
        Assert.That(layer.CanPlaceObjectInArea(2, 2, 4, 4), Is.False);
        Assert.That(layer.CanPlaceObjectInArea(0, 0, 2, 2), Is.True);
    }

    [Test]
    public void MemoryUsage_IsUnderLimitForMillionTiles()
    {
        // Arrange
        var size = 1000;
        var layer = new TileLayer(size, size);

        //Act
        var bytes = layer.GetEstimatedMemoryUsageBytes();

        //Assert
        Assert.That(bytes, Is.LessThan(8L * 1024 * 1024), $"Memory usage too high: {bytes} bytes");
    }

    [Test]
    public void MassRead_NoAllocations()
    {
        // Arrange
        var layer = new TileLayer(1000, 1000);

        layer.FillArea(0, 0, 1000, 1000, TileType.Plain);
        layer.SetTileType(500, 500, TileType.Mountain);

        //Act
        for (var i = 0; i < 1000; i++) layer.GetTileType(i % 1000, (i * 7) % 1000);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        var t = TileType.Plain;
        for (var y = 0; y < 1000; y++)
        {
            for (var x = 0; x < 1000; x++)
            {
                t = layer.GetTileType(x, y);
            }
        }

        var after = GC.GetAllocatedBytesForCurrentThread();
        var diff = after - before;

        //Assert
        Assert.That(diff, Is.LessThan(1024 * 16), $"Too many allocations during mass-read: {diff} bytes");
        Assert.That(t is TileType.Plain or TileType.Mountain, Is.True);
    }

    [Test]
    public void ReadOnlyView_IsSafeForParallelReads()
    {
        // Arrange
        var layer = new TileLayer(500, 500);
        layer.FillArea(0, 0, 500, 500, TileType.Plain);
        layer.SetTileType(123, 321, TileType.Mountain);

        var readOnlyTileLayer = layer.AsReadOnly();

        //Act
        //Assert
        var tasks = Environment.ProcessorCount * 4;
        var run = Task.WhenAll(Enumerable.Range(0, tasks).Select(_ => Task.Run(() =>
        {
            for (var y = 0; y < readOnlyTileLayer.Height; y++)
            {
                for (var x = 0; x < readOnlyTileLayer.Width; x++)
                {
                    var tileType = readOnlyTileLayer.GetTileType(x, y);
                    if (x == 123 && y == 321)
                    {
                        Assert.That(TileType.Mountain, Is.EqualTo(tileType));
                    }
                }
            }
        })));

        Assert.DoesNotThrowAsync(async () => await run);
    }
}