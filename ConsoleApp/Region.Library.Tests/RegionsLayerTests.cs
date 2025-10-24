using NUnit.Framework;

namespace Region.Library.Tests;

[TestFixture]
public class RegionsLayerTests
{
    [Test]
    public void Generate_CoversAllTiles_And_RegionsEqualArea()
    {
        // Arrange 
        const int width = 40;
        const int height = 30;
        
        // Act
        var layer = RegionsLayer.Generate(width, height, i => $"R{i}");

        // Assert
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var id = layer.GetRegionId(x, y);
                Assert.That(id, Is.GreaterThan(0));
            }
        }

        var areas = layer.Regions.Select(r => r.Bounds.Width * r.Bounds.Height).Distinct().ToArray();
        Assert.That(1, Is.EqualTo(areas.Length), "All regions must have equal area");

        var total = layer.Regions.Sum(r => (long)r.Bounds.Width * r.Bounds.Height);
        Assert.That((long)width * height, Is.EqualTo(total));
    }


    [Test]
    public void GetRegionsIntersecting_ReturnsCorrectRegions()
    {
        // Arrange 
        const int width = 20;
        const int height = 20;
        var layer = RegionsLayer.Generate(width, height);
        var q = new Rect(5, 5, 4, 4);

        // Act
        var regs = layer.GetRegionsIntersecting(q);

        // Assert
        Assert.That(regs, Is.Not.Empty);
        foreach (var r in regs)
            Assert.That(r.Bounds.Intersects(q), Is.True);
    }

    [Test]
    public void TileBelongsToRegion_Works()
    {
        // Arrange 
        const int width = 10;
        const int height = 10;
        
        // Act
        var layer = RegionsLayer.Generate(width, height);
        
        // Assert
        var id = layer.GetRegionId(0, 0);
        Assert.That(layer.TileBelongsToRegion(0, 0, id), Is.True);
        Assert.That(layer.TileBelongsToRegion(1, 1, id + 9999), Is.False);
    }


    [Test]
    public void OutOfBounds_Throws()
    { 
        // Arrange 
        var layer = RegionsLayer.Generate(8, 8);
        
        // Act
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => layer.GetRegionId(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => layer.GetRegionId(0, 8));
    }
}