using Eto.Forms;
using Eto.Drawing;

namespace ArchSitePlan.RhinoInterface.NewFolder;
public class MetricsPopUp:Form
{
    private static Window _popUp;

    public MetricsPopUp()
    {
        Title = "Main Window";
        ClientSize = new Size(400, 300);

        var showPopUpButton = new Button { Text = "Show Popup" };

        Content = new StackLayout
        {
            Padding = 10,
            Items =
            {
                new Label { Text = "Click the button to open a new window." },
                showPopUpButton
            }
        };
    }
}
