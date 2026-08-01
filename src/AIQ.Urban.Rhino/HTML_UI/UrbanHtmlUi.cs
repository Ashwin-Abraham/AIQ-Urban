using System;
using Eto.Drawing;
using Eto.Forms;

namespace AIQ.Urban.RhinoPlugin.HTML_UI;

public static class UrbanHtmlUi
{
    private const int DefaultWindowWidth = 520;
    private const int DefaultWindowHeight = 1000;

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

        _window = new Form
        {
            Title = "AIQ Urban UI",
            Width = DefaultWindowWidth,
            Height = DefaultWindowHeight,
            Topmost = true,
            Content = layout,
        };

        _window.Closed += (_, _) =>
        {
            _window = null;
            onClosed();
        };
        _window.Show();

        return _window;
    }
}
