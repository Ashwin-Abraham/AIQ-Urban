using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.BHoM.Geometry.Models;
using ArchSitePlan.DebugModel;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Test.ArchSitePlan.BHoM.Geometry.Extensions;
public class ICurveExtensionsTests
{
    [Fact]
    public void CanTrim_Simple()
    {
        // Arrange
        var line = Create.Line(Point.Origin, Create.Point(10, 0));
        var trimmer = new Rectangle(Create.Point(5, -1), Create.Point(11, 1));

        // Act
        var trimmed = line.Trim([trimmer]);

        // Viz
        line.Debug();
        trimmer.Debug();
        trimmed.Debug(DebugMaterials.Blue, 0.1);
        DEBUGMODEL.OpenAsGlb();

        // Assert
        var length = trimmed.Sum(x => x.ILength());
        Assert.Equal(5, length);
    }

    [InlineData(true, 5)]
    [InlineData(false, 7)]
    [Theory]
    public void CanTrim_OnEdgeAndWithin(bool trimOnEdge, double expectedLength)
    {
        // Arrange
        var line = Create.Line(Point.Origin, Create.Point(10, 0));
        var trimmer = Create.Polygon(new()
        {
            Create.Point(5,0),
            Create.Point(7,0),
            Create.Point(7,-1),
            Create.Point(12,-1),
            Create.Point(12,3),
            Create.Point(5,3),
        });

        // Act
        var trimmed = line.Trim([trimmer], trimOnEdge);

        // Viz
        trimmer.Debug();
        line.Debug(zMovt: 0.2);
        trimmed.Debug(DebugMaterials.Blue, 0.3);
        DEBUGMODEL.OpenAsGlb();

        // Assert
        var length = trimmed.Sum(x => x.ILength());
        Assert.Equal(expectedLength, length);
    }

    [Fact]
    public void CanTrim_Multiple()
    {
        // Arrange
        var line = Create.Line(Point.Origin, Create.Point(10, 0));
        var trimmer1 = new Rectangle(Create.Point(6, -2), Create.Point(12, 2));
        var trimmer2 = new Rectangle(Create.Point(-2, -2), Create.Point(2, 2));

        // Act
        var trimmed = line.Trim([trimmer1, trimmer2]);

        // Viz
        trimmer1.Debug();
        trimmer2.Debug();
        line.Debug(zMovt: 0.2);
        trimmed.Debug(DebugMaterials.Blue, 0.3);
        DEBUGMODEL.OpenAsGlb();

        // Assert
        var length = trimmed.Sum(x => x.ILength());
        Assert.Equal(4, length);
    }
}
