using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Singletons;
using ArchSitePlan.DebugModel;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.RhinoBHoMInterop;
using BH.Engine.Geometry;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using System.Windows;
using System.Windows.Input;

namespace ArchSitePlan.UI
{
    public class MainViewModel:ViewModelBase
    {
        public static RhinoDoc doc = RhinoDoc.ActiveDoc;
        private bool _buttonToggle = true;
        public string ButtonToggle
        {
            get
            {
                return _buttonToggle.ToString();
            }
            set
            {
                _buttonToggle = !_buttonToggle;
                OnPropertyChanged(nameof(ButtonToggle));
            }
        }

        private double _roadWidth = 3;
        public double RoadWidth
        {
            get => _roadWidth;
            set
            {
                if (InputHelpers.IsNumber(value.ToString()))
                    _roadWidth = value;
                else
                    RoadWidth = 0;

                OnPropertyChanged(nameof(RoadWidth));
            }
        }

        public ICommand ToggleValue_Cmd { get; }
        public ICommand SelectRoadCurve_Cmd { get; }
        public ICommand DrawRoadLine_Cmd { get; }
        public ICommand DrawRoadPolyline_Cmd { get; }
        public ICommand VisualizeModel_Cmd { get; }
        public MainViewModel()
        {
            Singletons.RoadNetwork = new RoadNetwork();
            this.ToggleValue_Cmd = new RelayCommand(ToggleValue_Exec);
            this.SelectRoadCurve_Cmd = new RelayCommand(SelectRoadCurve_Exec);
            this.DrawRoadLine_Cmd = new RelayCommand(DrawRoadLine_Exec);
            this.DrawRoadPolyline_Cmd = new RelayCommand(DrawRoadPolyline_Exec);
            this.VisualizeModel_Cmd = new RelayCommand(VisualizeModel_Exec);
        }

        private void VisualizeModel_Exec(object obj)
        {
            try
            {
                Output.OutputRoadsToRhino(RhinoDoc.ActiveDoc, Singletons.RoadNetwork);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void DrawRoadPolyline_Exec(object obj)
        {
        }

        private void DrawRoadLine_Exec(object obj)
        {

        }

        private void SelectRoadCurve_Exec(object obj)
        {
            int a = 0;
            int x = a + 25;
            try
            {
               var result = RhinoGet.GetOneObject(
                   UIMessages.SelectRoadCrv,
                   true,
                   ObjectType.Curve,
                   out ObjRef objRef);

                if (result != Rhino.Commands.Result.Success)
                    throw new Exception("No valid object selected");

                RhinoObject ob = objRef.Object();
                Guid id = ob.Id;
                Curve curve = ob.Geometry as Curve;

                if (curve.Degree > 2)
                    throw new Exception("Curve degree is too high, please select line, polyline or arc only");


                if(curve.TryGetPolyline(out Polyline polyline))
                {
                    var bhom = polyline.ToBHoM();
                    Singletons.RoadNetwork.AddRoad(polyline.ToBHoM(), id, 10);
                }
            }
            catch (Exception ex) 
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ToggleValue_Exec(object obj)
        {
            // Dummy set to trigger bool inversion
            ButtonToggle = "";
        }
    }
}
