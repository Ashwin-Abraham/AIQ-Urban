using Eto.Forms;
using Eto.Drawing;
using ArchSitePlan.Core.Models;
using BH.Engine.Data;
using BH.Engine.Geometry;
using Rhino.Display;
using Rhino;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using WebSocketSharp.Server;
using ArchSitePlan.RhinoInterface.HTML_UI.Websockets;

namespace ArchSitePlan.RhinoInterface.NewFolder;
public class MetricsPopUp:Form
{
    private TableLayout _dataTable = new();

    public MetricsPopUp(Point? origin = null)
    {
        if(origin != null)
            Location = origin.Value;
        
        WindowStyle = WindowStyle.Utility;
        Title = "Main Window";
        ClientSize = new Size(400, 300);
        Topmost = true;
        SetContent(0, "Unknown", new());
    }

    private void SetContent(int objectsSelected, string objectType, TableLayout layout)
    {
        string selectionMessage = $"Selected {objectsSelected} {objectType}(s)";
        Content = new StackLayout
        {
            Padding = 10,
            Items =
            {
                selectionMessage,
                layout
            }
        };
    }

    private TableLayout CreateTableLayout(params (string Title, string Data)[] items)
    {
        var bold = new Font(SystemFont.Bold);
        int columnIndex = 0;
        List<TableRow> rows = new();
        var currentRow = new TableRow();

        foreach (var item in items)
        {

            var title = new Label { Text = item.Title };
            var data = new Label { Text = item.Data , Font = bold };

            currentRow.Cells.Add(title);
            currentRow.Cells.Add(data);

            columnIndex += 2;
            if(columnIndex > 2)
            {
                rows.Add(currentRow);
                currentRow = new TableRow();
                columnIndex = 0;
            }
        }

        return new TableLayout(rows)
        {
            Padding = 10,
            Spacing = new Size(10, 10),
        };
    }

    internal void ConfigureBuildings(List<Building> buildings)
    {
        var bua = buildings.Sum(x => x.BUA()).Round(1);
        var uses = buildings.Select(x => x.Use.Name).Distinct().ToList();
        string use = "Multiple";
        if (uses.Count == 1)
            use = uses[0];

        var footprintArea = buildings.Sum(x => x.FootprintArea()).Round(1);

        (string Title, string Data)[] items = [
            ("Use: ", use),
            ("Built Area: ", bua.ToString()),
            ("Footprint Area: ", footprintArea.ToString())
            ];

        SetContent(
            buildings.Count,
            "building",
            CreateTableLayout(items));

    }

    public void BroadcastMessage(
        string selectedObjectType,
        List<(string Title, string Data)> metrics)
    {
        var message = new SelectedObjectMetricsMessage(selectedObjectType, metrics);
        WebSocketComms.BroadcastMessage(message);
    }

    internal void ConfigureLanduse(List<Parcel> parcels)
    {
        var uses = parcels.Select(x => x.LandUse.Name).Distinct().ToList();
        string use = "Multiple";
        if (uses.Count == 1)
            use = uses[0];

        var bua = parcels.Sum(x => x.BUA()).Round(1);
        var siteArea = parcels.Sum(x => x.Area()).Round(1);

        var far = (bua / siteArea).Round(2);
        var perimeter = parcels.Sum(x => x.Perimeter()).Round(1);

        (string Title, string Data)[] items = [
            ("Use: ", use),
            ("Perimeter: ", perimeter.ToString()),
            ("Site Area: ", siteArea.ToString()),
            ("Built Area: ", bua.ToString()),
            ("FAR: ", far.ToString()),
           ];

        SetContent(
            parcels.Count,
            "parcel",
            CreateTableLayout(items));

        BroadcastMessage(
            selectedObjectType: "Parcel",
            metrics: items.ToList());
    }

    internal void ConfigureRoads(List<Road> roads)
    {
        var widths = roads.Select(x => x.Width).Distinct().ToList();
        string width = "Multiple";
        if (widths.Count == 1)
            width = widths[0].ToString();

        var length = roads.Sum(x => x.Centerline.Length()).Round(1);

        (string Title, string Data)[] items = [
            ("Width: ", width),
            ("Length: ", length.ToString()),
           ];

        SetContent(
            roads.Count,
            "road",
            CreateTableLayout(items));
    }

    internal void OutputToRhino()
    {
        RhinoApp.WriteLine("message", System.Drawing.Color.Blue);
    }
}