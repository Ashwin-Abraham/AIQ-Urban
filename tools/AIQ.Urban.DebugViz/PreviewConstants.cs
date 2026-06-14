using System.Text;

namespace AIQ.Urban.DebugViz;
internal class PreviewConstants
{
    internal static readonly string DefaultLayer = "Default";
    internal static readonly string DefaultCurveLayer = CombineLayers(DefaultLayer, "Curves");

    private static string CombineLayers(params string[] pathFragments)
    {
        if(pathFragments.Length == 0) return "";

        var pathBuilder = new StringBuilder(pathFragments[0]);

        for(int i = 1; i < pathFragments.Length; i++)
        {
            pathBuilder.Append(":");
            pathBuilder.Append(pathFragments[i]);
        }

        return pathBuilder.ToString();
    }
}
