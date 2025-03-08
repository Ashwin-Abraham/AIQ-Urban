using AIQ.Primitives.Extensions;
using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.BHoM.Geometry.Models;
using ArchSitePlan.DebugModel;
using ArchSitePlan.DebugModel.BHoMExtensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Test.Geometry.Models;

public class RectangleTests
{
    [Fact]
    public void GetLargeSegments_Typical()
    {
        // Arrange
        Rectangle rect = new Rectangle(Point.Origin, Create.Point(3, 2));
        
        // Act
        var largeSegments = rect.GetLargeSegments();

        // Assert
        Assert.Equal(2, largeSegments.Count);
        Assert.Equal(3, largeSegments[0].Length());
        Assert.Equal(3, largeSegments[1].Length());
    }

    [Fact]
    public void GetLargeSegments_Square()
    {
        // Arrange
        Rectangle rect = new Rectangle(Point.Origin, Create.Point(2, 2));

        // Act
        var largeSegments = rect.GetLargeSegments();

        // Assert
        Assert.Equal(4, largeSegments.Count);
        Assert.Equal(2, largeSegments[0].Length());
        Assert.Equal(2, largeSegments[1].Length());
        Assert.Equal(2, largeSegments[2].Length());
        Assert.Equal(2, largeSegments[3].Length());
    }

    [InlineData("Square", 2.0, 3.0, 1.0,1.0)]
    [InlineData("Rectangle", 3.0, 4.0, 1.0,1.0)]
    [InlineData("Rotated", 3.0, 4.0, 0.0,1.414, 45)]
    [Theory]
    public void CanContour(
        string caseName,
        double y,
        int contourCount,
        double? expectedMidOfSecondX,
        double? expectedMidOfSecondY,
        double rotation = 0)
    {
        // Arrange
        Rectangle rect = new Rectangle(Point.Origin, Create.Point(2, y))
            .Rotate(Point.Origin, Vector.ZAxis, rotation.ToRadians());

        var expectedMidOfSecond = expectedMidOfSecondX.HasValue ?
            Create.Point(expectedMidOfSecondX.Value, expectedMidOfSecondY.Value)
            : null;

        // Act
        var contours = rect.Contour(contourCount);

        // Preview
        DEBUGMODEL.AddBoundedCurves(contours.ToHypar(), DebugMaterials.Red, 0.05);
        DEBUGMODEL.AddBoundedCurves(rect.ToHypar());
        DEBUGMODEL.OpenAsGlb();

        // Assert
        var midOfSecond = contours[1].Mid();
        Assert.Equal(contourCount, contours.Count);

        if(midOfSecond != null)
            Assert.True(midOfSecond.AlmostEquals(expectedMidOfSecond, 1e-03));
    }
}
