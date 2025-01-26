using System.IO;
using System.Reflection;
using ArchSitePlan.UI.Helpers;
using Eto.Drawing;
using Eto.Forms;

namespace ArchSitePlan.UI.HTML_UI;

public class HtmlForm : Form
{
    public HtmlForm()
    {
        ClientSize = new Size(800, 600);

        // Create a WebView to display HTML content
        var webView = new WebView();

        // Load HTML from a string (you can also load from a file or URL)
        string filePath = PathHelpers.GetResourcePath("index.html");

        string htmlContent = File.ReadAllText(filePath);

        webView.LoadHtml(htmlContent, null);

        // Add the WebView to the form
        Content = webView;
    }
}
