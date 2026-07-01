using System.Drawing;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.Core.Models.UrbanGraph;
using Rhino;
using Rhino.Geometry;

namespace AIQ.Urban.DebugViz.Converters;

public class GraphConverter(double sphereRadius = 0.5) : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is Graph || @object is IEnumerable<Graph>;

    public void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null)
    {
        if (@object is Graph graph)
            AddToPreviewDoc(graph, doc, color);

        if (@object is IEnumerable<Graph> graphs)
            foreach (var g in graphs)
                AddToPreviewDoc(g, doc, color);
    }

    private void AddToPreviewDoc(Graph graph, RhinoDoc doc, Color? overrideColor)
    {
        foreach (var node in graph.Nodes)
        {
            var nodeColor = overrideColor ?? (node.SourceObject switch
            {
                Building => Color.DarkRed,
                Parcel => Color.Orange,
                _ => Color.Gray,
            });

            var attribs = RhinoHelper.MakeAttributes(doc, PreviewConstants.GraphNodesLayer, nodeColor);
            var sphere = new Sphere(node.Point, sphereRadius);
            doc.Objects.AddBrep(sphere.ToBrep(), attribs);
        }

        foreach (var edge in graph.Edges)
        {
            var edgeColor = overrideColor ?? (edge.SourceElement switch
            {
                Transit => Color.Black,
                Building => Color.IndianRed,
                _ => Color.Gray,
            });

            var attribs = RhinoHelper.MakeAttributes(doc, PreviewConstants.GraphEdgesLayer, edgeColor);
            var line = new Line(edge.Start.Point, edge.End.Point);
            doc.Objects.AddLine(line, attribs);
        }
    }
}
