using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Singletons;
using ArchSitePlan.DebugModel;
using ArchSitePlan.UI.Actions;
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
        #region VARIABLES
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
                    _roadWidth = 3;

                OnPropertyChanged(nameof(RoadWidth));
                RhinoEventHandler.ActiveRoadWidth = _roadWidth;
            }
        }

        List<Guid> _roadCenterlineGuids = Singletons.RoadNetwork.Roads
            .Select(r => r.CenterLineId)
            .ToList();

        private bool _roadCLSelectionModeActive = false;
        public bool RoadCLSelectionModeActive
        {
            get => _roadCLSelectionModeActive;
            set
            {
                _roadCLSelectionModeActive = value;
                RhinoEventHandler.RoadCLSelectionModeActive = value;
                RhinoEventHandler.ActivateRoadSelectionFilter(value);
                OnPropertyChanged(nameof(RoadCLSelectionModeActive));
            }
        }
        #endregion

        #region COMMANDS
        public ICommand RefreshRoads_Cmd { get; }
        public ICommand SelectRoadCurve_Cmd { get; }
        public ICommand DrawRoadLine_Cmd { get; }
        public ICommand DrawRoadPolyline_Cmd { get; }
        public ICommand VisualizeModel_Cmd { get; }
        #endregion

        #region EVENTS
        
        #endregion

        public MainViewModel()
        {
            Singletons.RoadNetwork = new RoadNetwork();
            this.RefreshRoads_Cmd = new RelayCommand(RefreshRoads_Exec);
            this.SelectRoadCurve_Cmd = new RelayCommand(SelectRoadCurve_Exec);
            this.DrawRoadLine_Cmd = new RelayCommand(DrawRoadLine_Exec);
            this.DrawRoadPolyline_Cmd = new RelayCommand(DrawRoadPolyline_Exec);
            this.VisualizeModel_Cmd = new RelayCommand(VisualizeModel_Exec);
        }

        private void VisualizeModel_Exec(object obj)
        {
            try
            {
                OutputFromCoreModel.OutputRoadsToRhino(RhinoDoc.ActiveDoc, Singletons.RoadNetwork);
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
            try
            {
                ////var result = RhinoGet.GetOneObject(
                ////    UIMessages.SelectRoadCrv,
                ////    true,
                ////    ObjectType.Curve,
                ////    out ObjRef objRef);

                //// if (result != Rhino.Commands.Result.Success)
                ////     throw new Exception("No valid object selected");

                //// CoreModelInput.AddRoad(objRef.Object());

                //var result = RhinoGet.GetMultipleObjects(
                //    UIMessages.SelectRoadCrv,
                //    true,
                //    ObjectType.Curve,
                //    out ObjRef[] objRefs);

                //var selectedObjs = objRefs.Select(o => o.Object())
                //    .Where(o => o != null && !(o.Geometry is BrepEdge))
                //    .ToList();

                //selectedObjs.ForEach(obj => obj.Select(true));

                //selectedObjs.ForEach (obj => CoreModelInput.AddRoad(obj));

                //selectedObjs.ForEach(obj => obj.Select(false));
            }
            catch (Exception ex) 
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void RefreshRoads_Exec(object obj)
        {

        }
    }
}
