using ArchSitePlan.Core.Metrics;
using ArchSitePlan.Core.Models;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.Models;
using Rhino;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using static ArchSitePlan.Core.Constants.Enums;
using static ArchSitePlan.UI.Constants.Enums;

namespace ArchSitePlan.UI
{
    public class MainViewModel:ViewModelBase,INotifyPropertyChanged
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
            }
        }
        public RhinoSelectionMode SelectionMode = RhinoSelectionMode.None;

        private bool _roadCLSelectionModeActive = false;
        public bool RoadCLSelectionModeActive
        {
            get => _roadCLSelectionModeActive;
            set
            {
                _roadCLSelectionModeActive = value;
                OnPropertyChanged(nameof(RoadCLSelectionModeActive));

                if (_roadCLSelectionModeActive)
                    ParcelSelectionModeActive = false;
             
                SetSelectionMode();
            }
        }

        private bool _parcelSelectionModeActive = false;
        public bool ParcelSelectionModeActive
        {
            get => _parcelSelectionModeActive;
            set
            {
                _parcelSelectionModeActive = value;
                OnPropertyChanged(nameof(ParcelSelectionModeActive));

                if (_parcelSelectionModeActive)
                    RoadCLSelectionModeActive = false;

                SetSelectionMode();

            }
        }

        private void SetSelectionMode()
        {
            if (RoadCLSelectionModeActive)
                SelectionMode = RhinoSelectionMode.Road;
            else if (ParcelSelectionModeActive)
                SelectionMode = RhinoSelectionMode.Parcel;
            else
                SelectionMode = RhinoSelectionMode.None;
        }

        public List<Landuse> LandUses { get; set; } = Enum.GetValues(typeof(Landuse)).Cast<Landuse>().ToList();
        
        private Landuse _selectedLanduse = Landuse.Undefined;
        public Landuse SelectedLandUse
        {
            get => _selectedLanduse;
            set
            {
                _selectedLanduse = value;
                OnPropertyChanged(nameof(SelectedLandUse));
                //UpdateSelectedLanduseColour(_selectedLanduse);
            }
        }
        
        //private string _selectedLandUseColour = Defaults.LandUseColours[Landuse.Undefined].Name.ToString();
        //public string SelectedLandUseColour 
        //{
        //    get => _selectedLandUseColour;
        //    set
        //    {
        //        _selectedLandUseColour = value;
        //        OnPropertyChanged(nameof(SelectedLandUseColour));
        //    }
        //}

        private List<HorizontalGraphData> _landUseGraphData = new();
        public List<HorizontalGraphData> LandUseGraphData
        {
            get => _landUseGraphData;
            set
            {
                _landUseGraphData = value;
                OnPropertyChanged(nameof(LandUseGraphData));
            }
        }

        #endregion

        #region COMMANDS
        public ICommand RefreshRoads_Cmd { get; }
        public ICommand SelectRoadCurve_Cmd { get; }
        public ICommand VisualizeModel_Cmd { get; }
        #endregion

        public MainViewModel()
        {
            this.RefreshRoads_Cmd = new RelayCommand(RefreshRoads_Exec);
            this.SelectRoadCurve_Cmd = new RelayCommand(SelectRoadCurve_Exec);
        }

      
        //private void VisualizeModel_Exec(object obj)
        //{
        //    try
        //    {
        //        OutputFromCoreModel.OutputCoreModelToRhino(RhinoDoc.ActiveDoc);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }
        //}

        //private void UpdateSelectedLanduseColour(Landuse use)
        //{
        //    if (Defaults.LandUseColours.TryGetValue(use, out Color color))
        //        SelectedLandUseColour = color..ToString();
        //    else
        //        SelectedLandUseColour = Defaults.LandUseColours[Landuse.Undefined].Name.ToString();
        //}
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

        internal List<HorizontalGraphData> ComputeLandUseGraphData(ProjectMetrics projectMetrics)
        {
            var allUses = Enum.GetValues<Landuse>();
            List<HorizontalGraphData> graphData = new();
            var areaPerUse = projectMetrics.Parcels
                .GroupBy(p => p.Use)
                .ToDictionary(grp => grp.Key, grp => grp.Sum(p => p.Area));

            double maxArea = areaPerUse.Max(kvp => kvp.Value);

            foreach(var use in allUses)
            {
                double useArea = projectMetrics.Parcels.Where(p => p.Use == use)
                    .Sum(p => p.Area);

                Color useColor = Defaults.LandUseColours[Landuse.Undefined];

                if (Defaults.LandUseColours.TryGetValue(use, out Color color))
                    useColor = color;

                graphData.Add( new HorizontalGraphData(
                    useArea, 
                    use.ToString(), 
                    color,
                    maxArea));
            }
            return graphData;
        }
    }
}
