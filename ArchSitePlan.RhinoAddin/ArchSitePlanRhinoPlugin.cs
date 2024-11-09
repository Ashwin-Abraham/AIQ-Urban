using ArchSitePlan.UI;
using ArchSitePlan.UI.Actions;
using Eto.Forms;
using Rhino;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace ArchSitePlan.RhinoAddin
{
    ///<summary>
    /// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
    /// class. DO NOT create instances of this class yourself. It is the
    /// responsibility of Rhino to create an instance of this class.</para>
    /// <para>To complete plug-in information, please also see all PlugInDescription
    /// attributes in AssemblyInfo.cs (you might need to click "Project" ->
    /// "Show All Files" to see it in the "Solution Explorer" window).</para>
    ///</summary>
    public class ArchSitePlanRhinoPlugin : Rhino.PlugIns.PlugIn
    {
        public ArchSitePlanRhinoPlugin()
        {
            Instance = this;
        }

        ///<summary>Gets the only instance of the ArchSitePlanRhinoPlugin plug-in.</summary>
        public static ArchSitePlanRhinoPlugin Instance { get; private set; }
        protected override Rhino.PlugIns.LoadReturnCode OnLoad(ref string errorMessage)
        {
            Type mainPanel = typeof(MainViewHost);
            var outPutDirectory = Path.GetDirectoryName(Assembly.Location);
            string path = outPutDirectory + "\\EmbeddedResources\\LOGO.ico";
            Icon icon = new Icon(path, new Size(16,16));
            Rhino.UI.Panels.RegisterPanel(this,
                mainPanel,
                "SiteArchitecturePlanner",
                icon);

            return Rhino.PlugIns.LoadReturnCode.Success;
        }
    }

    [Guid("0874C691-A083-4C05-9E31-96C2C63F7C7E")]
    public class MainViewHost : RhinoWindows.Controls.WpfElementHost

    {
        public static MainViewModel viewModel = new MainViewModel();
        public static Main view = new Main();
        public MainViewHost() : base(view, viewModel)
        {
            RhinoEventHandler.ViewModel = viewModel;
        }

        /// <summary>
        /// Returns the ID of this panel.
        /// </summary>
        public static System.Guid PanelId
        {
            get
            {
                return typeof(MainViewHost).GUID;
            }
        }
    }
}