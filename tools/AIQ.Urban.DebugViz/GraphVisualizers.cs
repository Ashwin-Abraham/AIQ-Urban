using System.Drawing;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.Core.Models.UrbanGraph;
using Rhino;
using Rhino.Geometry;

namespace AIQ.Urban.DebugViz;

public static class GraphVisualizers
{
    public static void VisualizeGraph(this Graph graph, double sphereRadius = 0.5)
    {
        var doc = RhinoDoc.CreateHeadless(null);

        foreach (var node in graph.Nodes)
        {
            var color = node.SourceObject switch
            {
                Building => Color.DarkRed,
                Parcel => Color.Orange,
                _ => Color.Gray,
            };

            var attribs = RhinoHelper.MakeAttributes(doc, $"GraphNodes::{color.Name}", color);
            var sphere = new Sphere(node.Point, sphereRadius);
            doc.Objects.AddBrep(sphere.ToBrep(), attribs);
        }

        foreach (var edge in graph.Edges)
        {
            var color = edge.SourceElement switch
            {
                Transit => Color.Black,
                Building => Color.IndianRed,
                _ => Color.Gray,
            };

            var attribs = RhinoHelper.MakeAttributes(doc, $"GraphEdges::{color.Name}", color);
            var line = new Line(edge.Start.Point, edge.End.Point);
            doc.Objects.AddLine(line, attribs);
        }

        Visualizer.View(doc);
    }
}
