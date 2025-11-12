using System.Drawing;

namespace ArchSitePlan.Core.Utils;
public record GraphData(
    string Label,
    double Data,
    string Color);

public static class GraphDataExtensions
{
    public static Color GetColor(this GraphData data)
    {
        var rgba = data.Color
            .Substring(4)
            .Split(',')
            .Select(x => x.TrimEnd(')'))
            .ToArray();

        if (rgba.Length != 4)
            return Color.FromArgb(1, 0, 0, 0);

        Int32.TryParse(rgba[0], out int r);
        Int32.TryParse(rgba[1], out int g);
        Int32.TryParse(rgba[2], out int b);
        Int32.TryParse(rgba[3], out int a);

        return Color.FromArgb(a, r, g, b);
    }

    public static string GetColorHex(this GraphData data, bool withHash = false)
    {
        var color = GetColor(data);
        var hex = ColorTranslator.ToHtml(color);

        return withHash? hex: hex.TrimStart('#');
    }

    /// <summary>
    /// Checks the graph data and adds new data if absent. This allows us to draw charts in powerpoint where the api is limited
    /// The powerpoint chart drawing code needs all charts
    /// </summary>
    /// <param name="data">List to add essentials to if not present</param>
    /// <param name="essentials">Essential items. The number is ignored and set to 0 in the output</param>
    /// <returns></returns>
    public static List<GraphData> EnsureItIncludes(
        this List<GraphData> data, 
        List<GraphData> essentials)
    {
        foreach(var item in essentials)
        {
            if (data.Any(x => x.Label.Equals(item.Label)))
                continue;

            data.Add(new GraphData(item.Label, 0, item.Color));
        }

        return data;
    }

    public static List<GraphData> InAplhabeticalOrder(this List<GraphData> data) => data.OrderBy(x => x.Label).ToList();
}

