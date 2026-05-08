using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.DebugModel;
using BH.Engine.Geometry;

namespace ArchSitePlan.Test.ArchSitePlan.BHoM.Geometry.Extensions;
public class PlanarSurfaceExtensionsTests
{
    [Theory]
    [InlineData("TwoLinearSrfs")]
    [InlineData("FourLinearSrfs_WithHole")]
    [InlineData("TwoSquares")]
    [InlineData("TwoSquaresWithHoles")]
    [InlineData("SquareWithinSquare")]
    [InlineData("AdjacentSquares")]
    [InlineData("IsolatedSquares")]
    [InlineData("OverlappingCircles")]
    [InlineData("ThreeNestedSquares")]
    [InlineData("SquareWithinHole")]
    [InlineData("SquareInsideHoledSquare")]
    [InlineData("HoledSquareInsideSquare")]
    [InlineData("HoledSquareInsideHoledSquarePlusLineAcross")]
    [InlineData("SquareWithMultipleIntersectionPolyline")]
    [InlineData("ThreeNestedHoledSquaresPlusLineAcross")]
    public void CanRunBooleanUnion(string testCaseName)
    {
        // Arrange
        var testCase = new PlanarSurfaceExtensionTestData().UnionSurfaceTestCases[testCaseName];
        var toUnion = testCase.Surfaces.Set1.Concat(testCase.Surfaces.Set2).ToList();

        // Act
        var unioned = toUnion.BooleanUnion();

        // Visualize
        unioned.ForEach(x => x.Debug());
        unioned.SelectMany(x => x.InternalBoundaries)
            .Concat(unioned.Select(x => x.ExternalBoundary))
            .Debug(zMovt: 0.15);
        toUnion.SelectMany(x => x.InternalBoundaries)
            .Concat(toUnion.Select(x => x.ExternalBoundary))
            .Debug(DebugMaterials.Gray, zMovt: 0.1);
        DEBUGMODEL.OpenAsGlb();

        // Assert
        Assert.Equal(testCase.ResultCount, unioned.Count);
        Assert.Equal(testCase.ResultArea, unioned.Sum(x => x.Area()), 1e-03);
    }

    [Theory]
    [InlineData("TwoLinearSrfs")]
    [InlineData("FourLinearSrfs_WithHole")]
    [InlineData("TwoSquares")]
    [InlineData("TwoSquaresWithHoles")]
    [InlineData("SquareWithinSquare")]
    [InlineData("AdjacentSquares")]
    [InlineData("IsolatedSquares")]
    [InlineData("OverlappingCircles")]
    [InlineData("ThreeNestedSquares")]
    [InlineData("SquareWithinHole")]
    [InlineData("SquareInsideHoledSquare")]
    [InlineData("HoledSquareInsideSquare")]

    public void CanRunBooleanDifference(string testCaseName)
    {
        // Arrange
        var testCase = new PlanarSurfaceExtensionTestData().DifferencedSurfaceTestCases[testCaseName];

        // Act
        var diffed = testCase.Surfaces.Set1[0].BooleanDifference(testCase.Surfaces.Set2);

        // Visualize
        diffed.ForEach(x => x.Debug());
        diffed.SelectMany(x => x.InternalBoundaries)
            .Concat(diffed.Select(x => x.ExternalBoundary))
            .Debug(zMovt: 0.15);

        var input = testCase.Surfaces.Set1.Concat(testCase.Surfaces.Set2).ToList();

        input.SelectMany(x => x.InternalBoundaries)
            .Concat(input.Select(x => x.ExternalBoundary))
            .Debug(DebugMaterials.Gray, zMovt: 0.1);
        DEBUGMODEL.OpenAsGlb();

        // Assert
        Assert.Equal(testCase.ResultCount, diffed.Count);
        Assert.Equal(testCase.ResultArea, diffed.Sum(x => x.Area()), 1e-03);
    }

    [Theory]
    [InlineData("TwoLinearSrfs")]
    [InlineData("FourLinearSrfs_WithHole")]
    [InlineData("TwoSquares")]
    [InlineData("TwoSquaresWithHoles")]
    [InlineData("SquareWithinSquare")]
    [InlineData("AdjacentSquares")]
    [InlineData("IsolatedSquares")]
    [InlineData("OverlappingCircles")]
    [InlineData("ThreeNestedSquares")]
    [InlineData("SquareWithinHole")]
    [InlineData("SquareInsideHoledSquare")]
    [InlineData("HoledSquareInsideSquare")]
    public void CanRunBooleanIntersection(string testCaseName)
    {
        // Arrange
        var testCase = new PlanarSurfaceExtensionTestData().IntersectionSurfaceTestCases[testCaseName];

        // Act
        var intersected = testCase.Surfaces.Set1[0].BooleanIntersection(testCase.Surfaces.Set2);

        // Visualize
        DEBUGMODEL.Reset();
        intersected.ForEach(x => x.Debug());
        intersected.SelectMany(x => x.InternalBoundaries)
            .Concat(intersected.Select(x => x.ExternalBoundary))
            .Debug(zMovt: 0.15);

        var input = testCase.Surfaces.Set1.Concat(testCase.Surfaces.Set2).ToList();

        input.SelectMany(x => x.InternalBoundaries)
            .Concat(input.Select(x => x.ExternalBoundary))
            .Debug(DebugMaterials.Gray, zMovt: 0.1);
        DEBUGMODEL.OpenAsGlb();

        // Assert
        Assert.Equal(testCase.ResultCount, intersected.Count);
        Assert.Equal(testCase.ResultArea, intersected.Sum(x => x.Area()), 1e-03);
    }
}
