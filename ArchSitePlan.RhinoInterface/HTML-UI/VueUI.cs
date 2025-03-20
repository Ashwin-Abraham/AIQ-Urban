using Eto.Drawing;
using Eto.Forms;

namespace ArchSitePlan.RhinoInterface.HTML_UI;

public static class VueUI
{
    public static void OpenWindow()
    {
        WebView webView = new WebView { Height = 1000 };

        var layout = new DynamicLayout { DefaultSpacing = new Size(5, 5), Padding = new Padding(0) };
        webView.Url = new Uri(@"http://localhost:6300/index.html");

        layout.AddSeparateRow(webView);
        layout.Add(null);

        var dialog = new Form
        {
            Title = "Rhino Web Plugin",
            Width = 350,
            Height = 800,
            Topmost = true
        };

        dialog.Content = layout;
        dialog.Show();
    }
}
