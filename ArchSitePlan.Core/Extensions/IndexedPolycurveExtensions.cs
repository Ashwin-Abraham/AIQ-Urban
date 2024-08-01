using Elements.Geometry;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.Core.Extensions
{
    public static class IndexedPolycurveExtensions
    {
        //public static List<IndexedPolycurve> Offset(this IndexedPolycurve curve, double distance)
        //{
        //    Circle c;
            
        //    List<IndexedPolycurve> offsetSegments = new List<IndexedPolycurve>();

        //    for (int i = 0; i < curve.CurveIndices.Count; i++) 
        //    {
        //        if(curve.TryGetLine(i, out Line line))
        //        {
        //            var offsetLine = line.Thicken(distance * 2);
        //        }

        //        if(curve.TryGetArc(i, out Arc arc))
        //        {
        //            var offsetArc = arc.Offset(distance);
        //        }
        //    }
        //}

        public static bool TryGetLine(this IndexedPolycurve curve, int crvNumber, out Line line)
        {
            line = null;
            if (crvNumber > curve.CurveIndices.Count) return false;

            List<int> crvIndices = curve.CurveIndices[crvNumber].ToList();
            List<Vector3> crvVertices = crvIndices.Select(i => curve.Vertices[i]).ToList();

            if (crvVertices.Count != 2 || crvVertices[0].IsAlmostEqualTo(crvVertices[1])) return false;

            line = new Line(crvVertices[0], crvVertices[1]);
            return true;
        }

        public static bool TryGetArc(this IndexedPolycurve curve, int crvNumber, out Arc arc)
        {
            arc = null;
            if (crvNumber > curve.CurveIndices.Count) return false;

            List<int> crvIndices = curve.CurveIndices[crvNumber].ToList();
            List<Vector3> crvVertices = crvIndices.Select(i => curve.Vertices[i]).ToList();

            if (crvVertices.Count != 3 || crvVertices[0].IsAlmostEqualTo(crvVertices[1])) return false;

            arc = ArcExtensions.FromThreePoints(crvVertices[0], crvVertices[1], crvVertices[2]);
            return true;
        }
    }
}
