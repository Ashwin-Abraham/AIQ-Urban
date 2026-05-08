using System.Drawing;
using System.Reflection;

namespace AIQ.GH.Exports.Utils;

public static class IconLoader
{
    public static Bitmap? LoadIcon(string resourceName)
    {
        string namespaceName = "AIQ.GH.Exports";
        string resourcePath = $"{namespaceName}.Resources.{resourceName}";
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath);

        return stream != null ? new Bitmap(stream) : null;
    }
}
