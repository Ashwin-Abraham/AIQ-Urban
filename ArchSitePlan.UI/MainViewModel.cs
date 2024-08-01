using AA.RhinoHyparInterop;
using ArchSitePlan.Core.Singletons;
using ArchSitePlan.UI.Constants;
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

        public ICommand ToggleValue_Cmd { get; }
        public ICommand SelectRoadCurve_Cmd { get; }
        public ICommand DrawRoadLine_Cmd { get; }
        public ICommand DrawRoadPolyline_Cmd { get; }
        public MainViewModel()
        {
            this.ToggleValue_Cmd = new RelayCommand(ToggleValue_Exec);
            this.SelectRoadCurve_Cmd = new RelayCommand(SelectRoadCurve_Exec);
            this.DrawRoadLine_Cmd = new RelayCommand(DrawRoadLine_Exec);
            this.DrawRoadPolyline_Cmd = new RelayCommand(DrawRoadPolyline_Exec);
        }

        private void DrawRoadPolyline_Exec(object obj)
        {
        }

        private void DrawRoadLine_Exec(object obj)
        {
        }

        private void SelectRoadCurve_Exec(object obj)
        {
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
                Curve curve = ob.Geometry as Curve;

                if (curve.Degree > 2)
                    throw new Exception("Curve degree is too high, please select line, polyline or arc only");

                Singletons.RoadNetwork.AddRoad(curve.ToHyparPolycurve(), 2);
            }
            catch(Exception ex) 
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
