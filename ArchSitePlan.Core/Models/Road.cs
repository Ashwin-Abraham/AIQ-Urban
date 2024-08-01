using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Extensions;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.Core.Models
{
    public class Road
    {
        public Guid CenterLineId { get; init; }

        /// <summary>
        /// Contains a list of arcs and lines representing the centerline
        /// </summary>
        public IndexedPolycurve Centerline {  get; init; }
        public Polygon RoWs {  get; private set; }

        public double Width { get; private set; }

        public Road(IndexedPolycurve centerline, double width)
        {
            Centerline = centerline; 
            Width = width;

            //RoWs = CalculateRoWs();
        }

        //private void ValidateCenterLine(IndexedPolycurve centerline)
        //{
        //    for(int i =0; i< centerline.Count - 1; i++)
        //    {
        //        if (centerline[i].End.DistanceTo(centerline[i+1].Start) > Vector3.EPSILON) 
        //            throw new Exception(Messages.InvalidCL);
        //    }
        //}

        //private List<IndexedPolycurve> CalculateRoWs()
        //{
        //    var curves = Centerline.ToArcsAndLines();

        //    var offset = Centerline.Offset(Width / 2);
        //    foreach(var crv in Centerline)
        //    {
        //        if(crv.TryGetArc(out Arc arc))
        //            arc.

        //    }
        //    List<>
        //}
    }
}
