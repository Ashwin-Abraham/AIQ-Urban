using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.BHoM.Geometry.Models;
using ArchSitePlan.DebugModel;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Test.ArchSitePlan.BHoM.Geometry.Extensions;
public class PlanarSurfaceExtensionTestData
{
    public record SurfaceTestCase(SurfaceSet Surfaces, int ResultCount, double ResultArea);
    public record SurfaceSet(List<PlanarSurface> Set1, List<PlanarSurface> Set2);
    
    public Dictionary<string, SurfaceTestCase> DifferencedSurfaceTestCases = new()
    {
        {
            "TwoLinearSrfs",
            new SurfaceTestCase(
            /* Surfaces */ TwoLinearSrfs, 
            /* unionedCount */ 2, 
            /* unionedArea */ 16 )
        },

        {
            "FourLinearSrfs_WithHole",
            new SurfaceTestCase(
            /* Surfaces */ FourLinearSrfs_WithHole, 
            /* unionedCount */ 3, 
            /* unionedArea */ 12 )
        },

        {
            "TwoSquares",
            new SurfaceTestCase(
            /* Surfaces */ TwoSquares, 
            /* unionedCount */ 1, 
            /* unionedArea */ 75 )
        },

        {
            "TwoSquaresWithHoles",
            new SurfaceTestCase(
            /* Surfaces */ TwoSquaresWithHoles, 
            /* unionedCount */ 2, 
            /* unionedArea */ 56 )
        },

        {
            "SquareWithinSquare",
            new SurfaceTestCase(
            /* Surfaces */ SquareWithinSquare, 
            /* unionedCount */ 1, 
            /* unionedArea */ 64 )
        },

        {
            "AdjacentSquares",
            new SurfaceTestCase(
            /* Surfaces */ AdjacentSquares, 
            /* unionedCount */ 1, 
            /* unionedArea */ 100 )
        },

        {
            "IsolatedSquares",
            new SurfaceTestCase(
            /* Surfaces */ IsolatedSquares, 
            /* unionedCount */ 1, 
            /* unionedArea */ 4 )
        },

        {
            "OverlappingCircles",
            new SurfaceTestCase(
            /* Surfaces */ OverlappingCircles, 
            /* unionedCount */ 1, 
            /* unionedArea */ 29.54372 )
        },

        {
            "ThreeNestedSquares",
            new SurfaceTestCase(
            /* Surfaces */ ThreeNestedSquares, 
            /* unionedCount */ 1, 
            /* unionedArea */ 36 )
        },

        {
            "SquareWithinHole",
            new SurfaceTestCase(
            /* Surfaces */ SquareWithinHole, 
            /* unionedCount */ 1, 
            /* unionedArea */ 36 )
        },

        {
            "SquareInsideHoledSquare",
            new SurfaceTestCase(
            /* Surfaces */ SquareInsideHoledSquare, 
            /* unionedCount */ 1, 
            /* unionedArea */ 64 )
        },

        {
            "HoledSquareInsideSquare",
            new SurfaceTestCase(
            /* Surfaces */ HoledSquareInsideSquare, 
            /* unionedCount */ 2, 
            /* unionedArea */ 68 )
        },
    };

    public Dictionary<string, SurfaceTestCase> UnionSurfaceTestCases = new()
    {
        {
            "TwoLinearSrfs",
            new SurfaceTestCase(
            /* Surfaces */ TwoLinearSrfs, 
            /* unionedCount */ 1, 
            /* unionedArea */ 76 )
        },


        {
            "FourLinearSrfs_WithHole",
            new SurfaceTestCase(
            /* Surfaces */ FourLinearSrfs_WithHole, 
            /* unionedCount */ 1, 
            /* unionedArea */ 96 )
        },

        {
            "TwoSquares",
            new SurfaceTestCase(
            /* Surfaces */ TwoSquares, 
            /* unionedCount */ 1, 
            /* unionedArea */ 175 )
        },

        {
            "TwoSquaresWithHoles",
            new SurfaceTestCase(
            /* Surfaces */ TwoSquaresWithHoles, 
            /* unionedCount */ 1, 
            /* unionedArea */ 120 )
        },

        {
            "SquareWithinSquare",
            new SurfaceTestCase(
            /* Surfaces */ SquareWithinSquare, 
            /* unionedCount */ 1, 
            /* unionedArea */ 100 )
        },

        {
            "AdjacentSquares",
            new SurfaceTestCase(
            /* Surfaces */ AdjacentSquares, 
            /* unionedCount */ 1, 
            /* unionedArea */ 116 )
        },

        {
            "IsolatedSquares",
            new SurfaceTestCase(
            /* Surfaces */ IsolatedSquares, 
            /* unionedCount */ 2, 
            /* unionedArea */ 8 )
        },

        {
            "OverlappingCircles",
            new SurfaceTestCase(
            /* Surfaces */ OverlappingCircles, 
            /* unionedCount */ 1, 
            /* unionedArea */ 108.083537 )
        },

        {
            "ThreeNestedSquares",
            new SurfaceTestCase(
            /* Surfaces */ ThreeNestedSquares, 
            /* unionedCount */ 1, 
            /* unionedArea */ 100 )
        },

        {
            "SquareWithinHole",
            new SurfaceTestCase(
            /* Surfaces */ SquareWithinHole, 
            /* unionedCount */ 2, 
            /* unionedArea */ 40 )
        },

        {
            "SquareInsideHoledSquare",
            new SurfaceTestCase(
            /* Surfaces */ SquareInsideHoledSquare, 
            /* unionedCount */ 1, 
            /* unionedArea */ 100 )
        },

        {
            "HoledSquareInsideSquare",
            new SurfaceTestCase(
            /* Surfaces */ HoledSquareInsideSquare, 
            /* unionedCount */ 1, 
            /* unionedArea */ 100 )
        },

        {
        "HoledSquareInsideHoledSquarePlusLineAcross",
            new SurfaceTestCase(
            /* Surfaces */ HoledSquareInsideHoledSquarePlusLineAcross, 
            /* unionedCount */ 1, 
            /* unionedArea */ 280 )
        },

        {
        "SquareWithMultipleIntersectionPolyline",
            new SurfaceTestCase(
            /* Surfaces */ SquareWithMultipleIntersectionPolyline, 
            /* unionedCount */ 1, 
            /* unionedArea */ 77.12672 )
        },

                {
        "ThreeNestedHoledSquaresPlusLineAcross",
            new SurfaceTestCase(
            /* Surfaces */ ThreeNestedHoledSquaresPlusLineAcross, 
            /* unionedCount */ 1, 
            /* unionedArea */ 280 )
        },
    };

    public Dictionary<string, SurfaceTestCase> IntersectionSurfaceTestCases = new()
    {
        {
            "TwoLinearSrfs",
            new SurfaceTestCase(
            /* Surfaces */ TwoLinearSrfs, 
            /* unionedCount */ 1, 
            /* unionedArea */ 4 )
        },

        {
            "FourLinearSrfs_WithHole",
            new SurfaceTestCase(
            /* Surfaces */ FourLinearSrfs_WithHole, 
            /* unionedCount */ 2, 
            /* unionedArea */ 8 )
        },

        {
            "TwoSquares",
            new SurfaceTestCase(
            /* Surfaces */ TwoSquares, 
            /* unionedCount */ 1, 
            /* unionedArea */ 25 )
        },

        {
            "TwoSquaresWithHoles",
            new SurfaceTestCase(
            /* Surfaces */ TwoSquaresWithHoles, 
            /* unionedCount */ 2, 
            /* unionedArea */ 8 )
        },

        {
            "SquareWithinSquare",
            new SurfaceTestCase(
            /* Surfaces */ SquareWithinSquare, 
            /* unionedCount */ 1, 
            /* unionedArea */ 36 )
        },

        {
            "AdjacentSquares",
            new SurfaceTestCase(
            /* Surfaces */ AdjacentSquares, 
            /* unionedCount */ 0, 
            /* unionedArea */ 0 )
        },

        {
            "IsolatedSquares",
            new SurfaceTestCase(
            /* Surfaces */ IsolatedSquares, 
            /* unionedCount */ 0, 
            /* unionedArea */ 0 )
        },

        {
            "OverlappingCircles",
            new SurfaceTestCase(
            /* Surfaces */ OverlappingCircles, 
            /* unionedCount */ 1, 
            /* unionedArea */ 48.99609 )
        },

        {
            "ThreeNestedSquares",
            new SurfaceTestCase(
            /* Surfaces */ ThreeNestedSquares, 
            /* unionedCount */ 2, 
            /* unionedArea */ 100 )
        },

        {
            "SquareWithinHole",
            new SurfaceTestCase(
            /* Surfaces */ SquareWithinHole, 
            /* unionedCount */ 0, 
            /* unionedArea */ 0 )
        },

        {
            "SquareInsideHoledSquare",
            new SurfaceTestCase(
            /* Surfaces */ SquareInsideHoledSquare, 
            /* unionedCount */ 1, 
            /* unionedArea */ 32 )
        },

        {
            "HoledSquareInsideSquare",
            new SurfaceTestCase(
            /* Surfaces */ HoledSquareInsideSquare, 
            /* unionedCount */ 1, 
            /* unionedArea */ 32 )
        },
    };

    public static SurfaceSet TwoLinearSrfs = new(
        Set1: new List<PlanarSurface>()
        {
            Create.PlanarSurface(new Rectangle(Create.Point(), Create.Point(10,2))),
        },

        Set2: new List<PlanarSurface>()
        {
            Create.PlanarSurface(new Rectangle(Create.Point(6,15), Create.Point(8,-15))),
        });

    public static SurfaceSet FourLinearSrfs_WithHole = new(
    Set1: new List<PlanarSurface>()
        {
            Create.PlanarSurface(new Rectangle(Create.Point(), Create.Point(10,2))),
        },

        Set2: new List<PlanarSurface>()
        {
            Create.PlanarSurface(new Rectangle(Create.Point(2,-3), Create.Point(4,15))),
            Create.PlanarSurface(new Rectangle(Create.Point(7,-3), Create.Point(9,15))),
            Create.PlanarSurface(new Rectangle(Create.Point(0, 10), Create.Point(10,12))),
        });

    public static SurfaceSet TwoSquares = new(
    Set1: new List<PlanarSurface>()
        {
            Create.PlanarSurface(new Rectangle(Create.Point(), Create.Point(10,10))),
        },

        Set2: new List<PlanarSurface>()
        {
            Create.PlanarSurface(new Rectangle(Create.Point(5, 5), Create.Point(15,15))),
        });

    public static SurfaceSet TwoSquaresWithHoles = new(
    Set1: new List<PlanarSurface>()
    {
        Create.PlanarSurface(
            externalBoundary: new Rectangle(Create.Point(), Create.Point(10,10)),
            internalBoundaries: [new Rectangle(Create.Point(2,2), Create.Point(8,8))])
    },

    Set2: new List<PlanarSurface>()
    {
        Create.PlanarSurface(
            externalBoundary: new Rectangle(Create.Point(5,5), Create.Point(15,15)),
            internalBoundaries: [new Rectangle(Create.Point(7,7), Create.Point(13,13))])
    });

    public static SurfaceSet SquareWithinSquare = new(
        Set1: new List<PlanarSurface>()
        {
            Create.PlanarSurface(new Rectangle(Create.Point(), Create.Point(10,10))),
        },

        Set2: new List<PlanarSurface>()
        {
            Create.PlanarSurface(new Rectangle(Create.Point(2, 2), Create.Point(8,8))),
        });

    public static SurfaceSet AdjacentSquares = new(
    Set1: new List<PlanarSurface>()
    {
                Create.PlanarSurface(new Rectangle(Create.Point(), Create.Point(10,10))),
    },

    Set2: new List<PlanarSurface>()
    {
                Create.PlanarSurface(new Rectangle(Create.Point(10, 0), Create.Point(12,8))),
    });

    public static SurfaceSet IsolatedSquares = new(
    Set1: new List<PlanarSurface>()
    {
        Create.PlanarSurface(new Rectangle(Create.Point(), Create.Point(2,2))),
    },

    Set2: new List<PlanarSurface>()
    {
        Create.PlanarSurface(new Rectangle(Create.Point(3, 0), Create.Point(5,2))),
    });

    public static SurfaceSet OverlappingCircles = new(
    Set1: new List<PlanarSurface>()
    {
        Create.PlanarSurface(Create.Circle(Point.Origin, 5)),
    },

    Set2: new List<PlanarSurface>()
    {
        Create.PlanarSurface(Create.Circle(Create.Point(3,0), 5)),
    });

    public static SurfaceSet ThreeNestedSquares = new(
    Set1: new List<PlanarSurface>()
    {
        Create.PlanarSurface(new Rectangle(Point.Origin, Create.Point(10, 10))),
    },

    Set2: new List<PlanarSurface>()
    {
        Create.PlanarSurface(new Rectangle(Create.Point(1,1), Create.Point(9, 9))),
        Create.PlanarSurface(new Rectangle(Create.Point(2,2), Create.Point(8, 8))),
    });

    public static SurfaceSet SquareWithinHole = new(
    Set1: new List<PlanarSurface>()
    {
        Create.PlanarSurface(new Rectangle(Point.Origin, Create.Point(10, 10)),
            internalBoundaries: [new Rectangle(Create.Point(1,1), Create.Point(9,9))])
    },

    Set2: new List<PlanarSurface>()
    {
        Create.PlanarSurface(new Rectangle(Create.Point(4,4), Create.Point(6, 6))),
    });

    public static SurfaceSet SquareInsideHoledSquare = new(
    Set1: new List<PlanarSurface>()
    {
        Create.PlanarSurface(new Rectangle(Point.Origin, Create.Point(10, 10)),
            internalBoundaries: [new Rectangle(Create.Point(4,4), Create.Point(6,6))])
    },

    Set2: new List<PlanarSurface>()
    {
        Create.PlanarSurface(new Rectangle(Create.Point(2,2), Create.Point(8, 8))),
    });

    public static SurfaceSet HoledSquareInsideSquare = new(
    Set1: new List<PlanarSurface>()
    {
            Create.PlanarSurface(new Rectangle(Point.Origin, Create.Point(10, 10)))
    },

    Set2: new List<PlanarSurface>()
    {
            Create.PlanarSurface(new Rectangle(Create.Point(2,2), Create.Point(8, 8)),
                internalBoundaries: [new Rectangle(Create.Point(4,4), Create.Point(6,6))]),
    });

    public static SurfaceSet HoledSquareInsideHoledSquarePlusLineAcross = new(
    Set1: new List<PlanarSurface>()
    {
            Create.PlanarSurface(new Rectangle(Point.Origin, Create.Point(25, 25)),
                internalBoundaries: [new Rectangle(Create.Point(2,2), Create.Point(23, 23))])
    },

    Set2: new List<PlanarSurface>()
    {
            Create.PlanarSurface(new Rectangle(Create.Point(8,8), Create.Point(18, 18)),
                internalBoundaries: [new Rectangle(Create.Point(10,10), Create.Point(15,15))]),
            Create.PlanarSurface(new Rectangle(Create.Point(-5,12), Create.Point(30,13)))
    });

    public static SurfaceSet ThreeNestedHoledSquaresPlusLineAcross = new(
    Set1: new List<PlanarSurface>()
    {
            Create.PlanarSurface(new Rectangle(Point.Origin, Create.Point(25, 25)),
                internalBoundaries: [new Rectangle(Create.Point(2,2), Create.Point(23, 23))])
    },

    Set2: new List<PlanarSurface>()
    {
            Create.PlanarSurface(new Rectangle(Create.Point(5,5), Create.Point(20, 20)),
                internalBoundaries: [new Rectangle(Create.Point(6,6), Create.Point(19,19))]),
            Create.PlanarSurface(new Rectangle(Create.Point(10,10), Create.Point(16, 16)),
                internalBoundaries: [new Rectangle(Create.Point(11,11), Create.Point(15,15))]),
            Create.PlanarSurface(new Rectangle(Create.Point(-5,13), Create.Point(30,14)))
    });

    public static SurfaceSet SquareWithMultipleIntersectionPolyline = new(
       Set1: new List<PlanarSurface>()
       {
            Create.PlanarSurface(new Rectangle(Create.Point(), Create.Point(10,10)),
                internalBoundaries: [new Rectangle(Create.Point(2,2), Create.Point(8,8))])
       },

       Set2: new List<PlanarSurface>()
       {
           Create.PlanarSurface(
           Create.Polyline([
               Create.Point(0,0),
               Create.Point(-4, 3),
               Create.Point(6,14),
               Create.Point(10,10)
               ]).Thicken(0.3, EndCapType.Butt, Vector.ZAxis))
       });

}
