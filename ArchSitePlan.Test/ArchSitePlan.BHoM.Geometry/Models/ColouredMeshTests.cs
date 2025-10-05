using ArchSitePlan.BHoM.Geometry.DebugHelpers;
using BH.oM.Geometry;
using CreateGeom = BH.Engine.Geometry.Create;
using ColorSys = System.Drawing.Color;
using ArchSitePlan.DebugModel;
using ArchSitePlan.BHoM.Geometry.Models;

namespace ArchSitePlan.Test.ArchSitePlan.BHoM.Geometry.Models;
public class ColouredMeshTests
{
    private static Mesh _sampleMesh = CreateGeom.Mesh(
           vertices: new List<Point>
           {
                CreateGeom.Point(0,0,0),
                CreateGeom.Point(10,0,0),
                CreateGeom.Point(10,10,0),
                CreateGeom.Point(0,10,0),
                CreateGeom.Point(0,0,10),
                CreateGeom.Point(10,0,10),
                CreateGeom.Point(10,10,10),
                CreateGeom.Point(0,10,10),
           },

           faces: new List<Face>
           {
                CreateGeom.Face(3,2,1,0),
                CreateGeom.Face(0,1,5,4),
                CreateGeom.Face(1,2,6,5),
                CreateGeom.Face(2,3,7,6),
                CreateGeom.Face(3,0,4,7),
                CreateGeom.Face(4,5,6,7),
           }
           );


    [Fact]
    public void CanVizBhomMesh()
    {
        var box = CreateGeom.RandomBoundingBox();

        var coloredMesh = new ColoredMesh(
        mesh: _sampleMesh,
        faceColors: new List<ColorSys>
        {
                ColorSys.FromArgb(255,0,0),
                ColorSys.FromArgb(0, 255, 0),
                ColorSys.FromArgb(0, 0, 255),
                ColorSys.FromArgb(255, 255, 0),
                ColorSys.FromArgb(0, 255, 255),
                ColorSys.FromArgb(255, 0, 255),
        });

        coloredMesh.Debug().OpenAsGlb();
    }
}
