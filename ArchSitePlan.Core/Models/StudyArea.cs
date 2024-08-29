using BH.Engine.Geometry;
using BH.oM.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.Core.Models
{
    public class StudyArea
    {
        public Polyline Boundary { get; private set; }
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public double Area { get; private set; }

        public StudyArea(Polyline boundary, Guid id, string name)
        {
            Boundary = boundary;
            Id = id;
            Name = name;
            Area = Math.Abs(boundary.Area());
        }
    }
}
