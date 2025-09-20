using ArchSitePlan.RhinoInterface.CoreModelInterface;
using Eto.Drawing;
using Eto.Forms;

namespace ArchSitePlan.RhinoInterface.HTML_UI;

public static class VueUI
{
    public static bool WindowIsOpen { get; set; } = false;
    public static Form? OpenWindow()
    {
        if(WindowIsOpen)
            return null;

        WebView webView = new WebView { 
            Height = 1000 ,
            Width = 420
        };

        var layout = new DynamicLayout { DefaultSpacing = new Size(5, 5), Padding = new Padding(0) };
        webView.Url = new Uri(@"http://localhost:6300/index.html");

        layout.AddSeparateRow(webView);
        layout.Add(null);

        var dialog = new Form
        {
            Title = "Rhino Web Plugin",
            Width = 350,
            Height = 800,
            Topmost = true,
            Opacity=0.5
        };

        dialog.Content = layout;
        dialog.Show();

        return dialog;
    }

    private static void ToggleWindowOpenVariable(object? sender, EventArgs e)
    {
        WindowIsOpen = false;
        RhinoEventHandler.UnsubscribeModelEvents();
    }
}
