using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.UrbanGraph;
public class GraphNode(Point3d point, List<Edge> edges)
{
    public List<Edge> Edges { get; } = edges;
    public Point3d Point { get; } = point;

}
