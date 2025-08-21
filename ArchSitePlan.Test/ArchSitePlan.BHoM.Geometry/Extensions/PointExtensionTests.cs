using ArchSitePlan.BHoM.Geometry.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Test.ArchSitePlan.BHoM.Geometry.Extensions;

public class PointExtensionTests
{
    [Theory]
    [MemberData(nameof(GetColinearTestData))]
    public void AreColinear_WorksAsExpected(IEnumerable<Point> points, bool expected)
    {
        // Act
        bool result = points.AreColinear2D();

        // Assert
        Assert.Equal(expected, result);
    }

    public static IEnumerable<object[]> GetColinearTestData()
    {
        yield return new object[] // Horizontal line
        {
            new List<Point>
            {
                Create.Point(0, 0),
                Create.Point(1, 0),
                Create.Point(2, 0),
                Create.Point(3, 0)
            },
            true
        };

        yield return new object[] // Vertical line
        {
            new List<Point>
            {
                Create.Point(5, 1),
                Create.Point(5, 2),
                Create.Point(5, 3)
            },
            true
        };

        yield return new object[] // Diagonal line
        {
            new List<Point>
            {
                Create.Point(0, 0),
                Create.Point(1, 1),
                Create.Point(2, 2),
                Create.Point(3, 3)
            },
            true
        };

        yield return new object[] // Not collinear
        {
            new List<Point>
            {
                Create.Point(0, 0),
                Create.Point(1, 1),
                Create.Point(2, 3) // Breaks the line
            },
            false
        };

        yield return new object[] // Less than 3 points (should be true)
        {
            new List<Point>
            {
                Create.Point(0, 0),
                Create.Point(1, 2)
            },
            true
        };
    }
}
