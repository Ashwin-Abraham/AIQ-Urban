using System;
using Eto.Drawing;
using Eto.Forms;

namespace AIQ.Urban.RhinoPlugin.HTML_UI;

public static class UrbanHtmlUi
{
    private const int DefaultWindowWidth = 520;
    private const int DefaultWindowHeight = 900;
    private const int MinimumWindowWidth = 312;
    private const int MinimumWindowHeight = 630;

    private static Form? _window;

    public static Form OpenWindow(int httpPort, Action onClosed)
    {
        if (_window is not null)
            return _window;

        var webView = new WebView
        {
            Height = DefaultWindowHeight,
            Width = DefaultWindowWidth,
            Url = new Uri($"http://localhost:{httpPort}/index.html"),
        };

        var layout = new DynamicLayout { DefaultSpacing = new Size(5, 5), Padding = new Padding(0) };
        layout.AddSeparateRow(webView);
        layout.Add(null);

        var window = new Form
        {
            Title = "AIQ Urban UI",
            Width = DefaultWindowWidth,
            Height = DefaultWindowHeight,
            MinimumSize = new Size(MinimumWindowWidth, MinimumWindowHeight),
            Topmost = true,
            Content = layout,
        };

        _window = window;
        window.Shown += (_, _) => window.Topmost = false;
        window.Closed += (_, _) =>
        {
            _window = null;
            onClosed();
        };
        window.Show();

        return window;
    }
}
