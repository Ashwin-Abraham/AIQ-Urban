using ArchSitePlan.Core.Metrics;
using ArchSitePlan.Core.Models;
using ArchSitePlan.UI.Extensions;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.Models;
using Rhino;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using SolidColorBrush =  System.Windows.Media.SolidColorBrush;
using static ArchSitePlan.Core.Constants.Enums;
using static ArchSitePlan.UI.Constants.Enums;
using ArchSitePlan.UI.Actions;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Geometry;
using ArchSitePlan.UI.RhinoBHoMInterop;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.Core.Extensions;
using LiveCharts;
using LiveCharts.Wpf;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;
using System.IO;
using ClosedXML.Excel;
using ArchSitePlan.Core.Constants;

namespace ArchSitePlan.UI
{
    public record VMActiveParcelsDetails(
        string Use,
        string TotalArea,
        string BUA,
        string CoveragePctg,
        string FAR);

    public class MainViewModel:ViewModelBase,INotifyPropertyChanged
    {
        #region VARIABLES
        public static RhinoDoc doc = RhinoDoc.ActiveDoc;

        private VMActiveParcelsDetails _activeParcelsMetrics;
        public VMActiveParcelsDetails ActiveParcelMetrics
        {
            get => _activeParcelsMetrics;
            set
            {
                _activeParcelsMetrics = value;
                OnPropertyChanged(nameof(ActiveParcelMetrics));
            }
        }
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
        public AppMode AppMode = AppMode.None;

        private bool _appEnabled  = false;
        public bool AppEnabled
        {
            get => _appEnabled;
            set
            {
                _appEnabled = value;
                OnPropertyChanged(nameof(AppEnabled));
            }
        }

        private bool _roadExpanderOpen = false;
        public bool RoadExpanderOpen
        {
            get => _roadExpanderOpen;
            set
            {
                _roadExpanderOpen = value;
                OnPropertyChanged(nameof(RoadExpanderOpen));
                SetSelectionMode(AppMode.Road, RoadExpanderOpen);
            }
        }

        private bool _landuseExpanderOpen = false;
        public bool LanduseExpanderOpen
        {
            get => _landuseExpanderOpen;
            set
            {
                _landuseExpanderOpen = value;
                OnPropertyChanged(nameof(LanduseExpanderOpen));
                SetSelectionMode(AppMode.LandUse, LanduseExpanderOpen);
            }
        }

        private bool _exportsExpanderOpen = false;
        public bool ExportsExpanderOpen
        {
            get => _exportsExpanderOpen;
            set
            {
                _exportsExpanderOpen = value;
                OnPropertyChanged(nameof(ExportsExpanderOpen));
                SetSelectionMode(AppMode.Export, ExportsExpanderOpen);
            }
        }

        private bool _buildingsExpanderOpen = false;
        public bool BuildingsExpanderOpen
        {
            get => _buildingsExpanderOpen;
            set
            {
                _buildingsExpanderOpen = value;
                OnPropertyChanged(nameof(BuildingsExpanderOpen));
                SetSelectionMode(AppMode.Building, BuildingsExpanderOpen);
            }
        }

        private bool _studyAreaExpanderOpen = false;
        public bool StudyAreaExpanderOpen
        {
            get => _studyAreaExpanderOpen;
            set
            {
                _studyAreaExpanderOpen = value;
                OnPropertyChanged(nameof(StudyAreaExpanderOpen));
                SetSelectionMode(AppMode.StudyArea, StudyAreaExpanderOpen);
            }
        }

        private int _activeBuildingFloorCount = 3;
        public int ActiveBuildingFloorCount
        {
            get => _activeBuildingFloorCount;
            set
            {
                _activeBuildingFloorCount = value;
                OnPropertyChanged(nameof(ActiveBuildingFloorCount));
            }
        }

        private double _activeBuildingFloorHeight = 3.0;
        public double ActiveBuildingFloorHeight
        {
            get => _activeBuildingFloorHeight;
            set
            {
                _activeBuildingFloorHeight = Math.Round(value, 2);
                OnPropertyChanged(nameof(ActiveBuildingFloorHeight));
            }
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
                UpdateSelectedLanduseColour(_selectedLanduse);
            }
        }

        private SolidColorBrush _selectedLandUseColour = new SolidColorBrush(
            Defaults.LandUseColours[Landuse.Undefined].ToWindowsColor());
        public SolidColorBrush SelectedLandUseColour
        {
            get => _selectedLandUseColour;
            set
            {
                _selectedLandUseColour = value;
                OnPropertyChanged(nameof(SelectedLandUseColour));
            }
        }

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

        public BuildingSelectorMode BuildingSelectorMode = BuildingSelectorMode.None;
        

        private bool _buildingFootprintSelectorModeActive = true;
        public bool BuildingFootprintSelectorModeActive
        {
            get => _buildingFootprintSelectorModeActive;
            set
            {
                _buildingFootprintSelectorModeActive = value;
                BuildingSelectorMode = BuildingSelectorMode.Footprint;
                OnPropertyChanged(nameof(BuildingFootprintSelectorModeActive));
            }
        }

        private bool _buildingMassSelectorModeActive = false;
        public bool BuildingMassSelectorModeActive
        {
            get => _buildingMassSelectorModeActive;
            set
            {
                BuildingSelectorMode = BuildingSelectorMode.Mass;
                _buildingMassSelectorModeActive = value;
                OnPropertyChanged(nameof(BuildingMassSelectorModeActive));
            }
        }

        private static readonly string _defaultStudyAreaNameTextBox = "Enter name";
        private string _activeStudyAreaName = _defaultStudyAreaNameTextBox;
        public string ActiveStudyAreaName
        {
            get => _activeStudyAreaName;
            set
            {
                _activeStudyAreaName = value;
                ActiveStudy = CoreModel.Studies.Find(s => s.Name == value);
                OnPropertyChanged(nameof(ActiveStudyAreaName));
            }
        }

        private List<string> _studyAreasNames = new();
        public List<string> StudyAreasNames
        {
            get => _studyAreasNames;
            set
            {
                _studyAreasNames = value;
                OnPropertyChanged(nameof(StudyAreasNames));
            }
        }

        private StudyArea _activeStudy;
        public StudyArea ActiveStudy
        {
            get => _activeStudy;
            set
            {
                _activeStudy = value;
                UpdateDisplayedStudyAreaData(_activeStudy);
                OnPropertyChanged(nameof(ActiveStudy));
            }
        }
        private SeriesCollection _roadLengthSeries;
        public SeriesCollection RoadLengthSeries
        { 
            get => _roadLengthSeries;
            set
            {
                _roadLengthSeries = value;
                OnPropertyChanged(nameof(RoadLengthSeries));
            }
        }

        private SeriesCollection _roadAreaSeries;
        public SeriesCollection RoadAreaSeries
        {
            get => _roadAreaSeries;
            set
            {
                _roadAreaSeries = value;
                OnPropertyChanged(nameof(RoadAreaSeries));
            }
        }

        private SeriesCollection _parcelSeriesCollection;
        public SeriesCollection ParcelSeriesCollection
        {
            get => _parcelSeriesCollection;
            set
            {
                _parcelSeriesCollection = value;
                OnPropertyChanged(nameof(ParcelSeriesCollection));
            }
        }

        private double _graphHeight = 100;
        public double GraphHeight
        {
            get => _graphHeight;
            set
            {
                _graphHeight = value;
                OnPropertyChanged(nameof(GraphHeight));
            }
        }

        private bool _parcelsOrTransit;
        public bool ParcelsOrTransit
        {
            get => _parcelsOrTransit;
            set
            {
                _parcelsOrTransit = value;
                OnPropertyChanged(nameof(ParcelsOrTransit));
            }
        }

        private string _excelPath;
        public string ExcelPath
        {
            get => _excelPath;
            set
            {
                _excelPath = value;
                OnPropertyChanged(nameof(ExcelPath));
            }
        }

        #endregion

        #region COMMANDS
        public ICommand AddOrUpdateRoads_Cmd { get; }
        public ICommand VisualizeModel_Cmd { get; }
        public ICommand UpdateBuilding_Cmd { get; }
        public ICommand AddStudyArea_Cmd { get; }
        public ICommand ChangeParcelUse_Cmd { get; }
        public ICommand ExportExcel_Cmd { get; }
        public ICommand SetExcelPath_Cmd { get; }
        public ICommand Test_Cmd { get; }
        public ICommand ToggleEnableApp_Cmd { get; }
        #endregion

        public MainViewModel()
        {
            this.AddOrUpdateRoads_Cmd = new RelayCommand(AddOrUpdateRoads_Exec);
            this.UpdateBuilding_Cmd = new RelayCommand(UpdateBuilding_Exec);
            this.AddStudyArea_Cmd = new RelayCommand(AddStudyArea_Exec);
            this.Test_Cmd = new RelayCommand(Test_Exec);
            this.ChangeParcelUse_Cmd = new RelayCommand(ChangeParcelUse_Exec);
            this.ExportExcel_Cmd = new RelayCommand(ExportExcel_Exec);
            this.ToggleEnableApp_Cmd = new RelayCommand(ToggleEnableApp_Exec);
            this.SetExcelPath_Cmd = new RelayCommand(SetExcelPath_Exec);

            RhinoToCoreModel.ResetStudyAreas();
            ActiveStudy = CoreModel.Studies.FirstOrDefault();
            StudyAreasNames = CoreModel.Studies.Select(s => s.Name).ToList();
            ActiveStudyAreaName = ActiveStudy.Name;
        }

        private void ToggleEnableApp_Exec(object obj)
        {
            try
            {
                AppEnabled = !AppEnabled;

                bool expanderOpen = AppMode != AppMode.None;
                AppMode = AppMode.None;

                SetSelectionMode(AppMode, expanderOpen);

                if (!AppEnabled)
                {
                    // Hard lock to avoid complex refactor
                    CoreModelToRhino.ToggleLockModelLayers(RhinoDoc.ActiveDoc, AppEnabled);
                    return;
                }

                RhinoToCoreModel.ReloadProjectFromRhino(RhinoDoc.ActiveDoc);
                ActiveStudy = CoreModel.Studies.First();
                ActiveStudyAreaName = ActiveStudy.Name;
                StudyAreasNames = CoreModel.Studies.Select(s => s.Name).ToList();
                CoreModelToRhino.RefreshStudyAreaMetrics(true);
                CoreModelToRhino.OutputCoreModelToRhino(RhinoDoc.ActiveDoc);

            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }

        private void SetExcelPath_Exec(object obj)
        {
            try
            {
                using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
                {
                    folderBrowserDialog.Description = "Set export location";
                    folderBrowserDialog.ShowNewFolderButton = false;  

                    if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                    {
                        // Get the selected folder path
                        ExcelPath = Path.Combine(
                            folderBrowserDialog.SelectedPath,
                            "ProjectReport.xlsx");
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ExportExcel_Exec(object obj)
        {
            try
            {
                var pr = new ProjectReport(ExcelPath, CoreModel.Parcels, CoreModel.Studies);
                pr.WriteExcelFile();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ChangeParcelUse_Exec(object o)
        {
            RhinoDoc doc = RhinoDoc.ActiveDoc;
            int parcelLayerIndex = LayerHelpers.CreateOrGetLayerIndex(
                doc,
                LayerConstants.ParcelsLayer);

            // Try to select a curve
            var srfObjs = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false)
                .Where(obj => obj.ObjectType == ObjectType.Brep
                || obj.ObjectType == ObjectType.Surface)
                .ToList();

            foreach (var obj in srfObjs)
            {
                RhinoToCoreModel.TryUpdateParcelUse(obj, SelectedLandUse);
            }

            CoreModelToRhino.OutputParcelsToRhino(RhinoDoc.ActiveDoc);
            RefreshActiveStudyAndMetrics();
        }

        private void UpdateLandUseGraphData(StudyAreaMetrics metrics)
        {
            var groupedByUse = metrics.ParcelMetrics.GroupBy(p => p.Use);
            ParcelSeriesCollection = new SeriesCollection();

            foreach(var landUseGroup in groupedByUse)
            {
                var area = Math.Round(landUseGroup.Sum(p => p.Area));
                var use = landUseGroup.First().Use;

                var newRow = new ColumnSeries()
                {
                    Title = landUseGroup.First().Use.ToString(),
                    Values = new ChartValues<double> { area },
                    Fill = new SolidColorBrush(Defaults.LandUseColours[use].ToWindowsColor()),
                    Height = 40
                };

                ParcelSeriesCollection.Add(newRow);
            }
            GraphHeight = groupedByUse.Count() * 12 + 50;
        }

        private void UpdateRoadLengthGraphData(StudyAreaMetrics metrics)
        {
            var groupedByWidth = metrics.TransitMetrics.GroupBy(p => Math.Round(p.RoadWidth,1));
            RoadLengthSeries = new SeriesCollection();

            foreach (var transitGroup in groupedByWidth)
            {
                var length = Math.Round(transitGroup.Sum(p => p.RunningLength));
                var label = "Width (" + Math.Round(transitGroup.First().RoadWidth, 1).ToString() +"m)";

                var newRow = new PieSeries()
                {
                    Title = label,
                    Values = new ChartValues<double> { length },
                };

                RoadLengthSeries.Add(newRow);
            }
        }

        private void UpdateRoadAreaGraphData(StudyAreaMetrics metrics)
        {
            var groupedByWidth = metrics.TransitMetrics.GroupBy(p => Math.Round(p.RoadWidth, 1));
            RoadAreaSeries = new SeriesCollection();

            foreach (var transitGroup in groupedByWidth)
            {
                var area = Math.Round(transitGroup.Sum(p => p.Area));
                var label = "Width (" + Math.Round(transitGroup.First().RoadWidth, 1).ToString() + "m)";

                var newRow = new PieSeries()
                {
                    Title = label,
                    Values = new ChartValues<double> { area },
                };

                RoadAreaSeries.Add(newRow);
            }
        }

        private void Test_Exec(object obj)
        {
            try
            {
                var result = RhinoGet.GetOneObject("Select an object",
                true,
                ObjectType.Mesh,
                out ObjRef objRef);

                var bhomMesh = (objRef.Object().Geometry as Mesh).ToBHoM();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void AddStudyArea_Exec(object o)
        {
            try
            {
                var result = RhinoGet.GetOneObject("Select a closed curve as the study area",
                true,
                ObjectType.Curve,
                out ObjRef objRef);
                
                if (result != Rhino.Commands.Result.Success) return;

                var rhinoObj = objRef.Object();
                var boundary = rhinoObj.Geometry as Curve;

                if (!boundary.IsClosed)
                    throw new Exception("Error. Please select closed curve instead");
                if (ActiveStudyAreaName == _defaultStudyAreaNameTextBox
                    || ActiveStudyAreaName == string.Empty)
                    throw new Exception("Error. Please enter a study name");

                if (!boundary.TryGetPolyline(out Polyline rhinoPline))
                    throw new Exception("Select polylines only");

                RhinoToCoreModel.TryAddStudyArea(
                        rhinoPline.ToBHoM(),
                        rhinoObj.Id,
                        ActiveStudyAreaName);

                ActiveStudy = CoreModel.Studies.FirstOrDefault(s => rhinoObj.Id == s.Id);
                CoreModelToRhino.RefreshStudyAreaMetrics();

                CoreModelToRhino.OutputCoreModelToRhino(RhinoDoc.ActiveDoc);
                RefreshActiveStudyAndMetrics();
                StudyAreasNames = CoreModel.Studies.Select(s => s.Name).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateBuilding_Exec(object obj)
        {
            try
            {
                RhinoDoc doc = RhinoDoc.ActiveDoc;

                List<ObjectType> allowedTypes = new();
                string layerName;
                Color layerColor;

                if (BuildingFootprintSelectorModeActive)
                {
                    allowedTypes = RHGeneralConstants.BuildingFootprintObjectTypes;
                    //layerName = LayerConstants.BuildingFootprintLayer;
                    //layerColor = LayerConstants.BuildingFootprintColour;
                }
                else
                {
                    allowedTypes = RHGeneralConstants.BuildingEnvelopeObjectTypes;
                    //layerName = LayerConstants.BuildingMassLayer;
                    //layerColor = LayerConstants.BuildingMassColour;
                }

                var rhinoObjs = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false)
                    .Where(obj => allowedTypes.Contains(obj.ObjectType))
                    .ToList();

                //int index = LayerHelpers.CreateOrGetLayerIndex(doc, layerName, layerColor);
                //rhinoObjs.ForEach(o => LayerHelpers.ChangeLayer(o, index));

                CoreModelToRhino.ToggleLockModelLayers(doc, false);
                DocObjectHelpers.DeleteRhinoObjectsIfPresent(doc, rhinoObjs.Select(o => o.Id));
                RhinoEventHandler.UpdateSelectedBuildings(rhinoObjs,
                    ActiveBuildingFloorCount,
                    ActiveBuildingFloorHeight);

                CoreModelToRhino.ToggleLayerStateBasedOnAppMode(doc, AppMode.Building);
                doc.Views.Redraw();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }

        private void SetSelectionMode(AppMode changedExpander, bool expanderOpen)
        {
            // Set current mode
            if (!expanderOpen)
            {
                // If expander is closed while the app mode is unchanged, it means wwe have closed all expanders
                if (AppMode == changedExpander) AppMode = AppMode.None;
                if (AppMode == AppMode.None)
                    CoreModelToRhino.ToggleLockModelLayers(RhinoDoc.ActiveDoc, false);

                // Early return to avoid cyclic SetSelectionMode calls for expander closure
                return;
            }
            if (expanderOpen)
                AppMode = changedExpander;

            // Close other expanders
            if (AppMode != AppMode.Road && RoadExpanderOpen)
                RoadExpanderOpen = false;
            if (AppMode != AppMode.LandUse && LanduseExpanderOpen)
                LanduseExpanderOpen = false;
            if (AppMode != AppMode.Building && BuildingsExpanderOpen)
                BuildingsExpanderOpen = false;
            if (AppMode != AppMode.StudyArea && StudyAreaExpanderOpen)
                StudyAreaExpanderOpen = false;
            if (AppMode != AppMode.Export && ExportsExpanderOpen)
                ExportsExpanderOpen = false;

            if(expanderOpen)
                CoreModelToRhino.ToggleLayerStateBasedOnAppMode(RhinoDoc.ActiveDoc, changedExpander);
        }

        private void UpdateSelectedLanduseColour(Landuse use)
        {
            if (Defaults.LandUseColours.TryGetValue(use, out Color color))
                SelectedLandUseColour = new SolidColorBrush(color.ToWindowsColor());
            else
                SelectedLandUseColour = new SolidColorBrush(Defaults.LandUseColours[Landuse.Undefined]
                    .ToWindowsColor());
        }

        private void AddOrUpdateRoads_Exec(object obj)
        {
            try
            {
                RhinoDoc doc = RhinoDoc.ActiveDoc;
                int roadLayerIndex = LayerHelpers.CreateOrGetLayerIndex(
                    doc, 
                    LayerConstants.RoadCLLayer,
                    LayerConstants.RoadColour);

                // Try to select a curve
                var rhinoObjs = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false)
                    .Where(obj => obj.ObjectType == ObjectType.Curve)
                    .ToList();

                int roadIndex = Constants.RhinoObjAttributeConstants.RoadGeometryAttributes.LayerIndex;

                List<(BH.oM.Geometry.Polyline, Guid, double)> plineObjs = new();

                foreach(var rhinoObj in rhinoObjs)
                {
                    if (!(rhinoObj.Geometry as Curve).TryGetPolyline(out Polyline pline)) continue;

                    plineObjs.Add((pline.ToBHoM(), rhinoObj.Id, RoadWidth));
                    LayerHelpers.ChangeLayer(rhinoObj, roadLayerIndex);
                }

                if (!plineObjs.Any()) return;

                CoreModelToRhino.ToggleLockModelLayers(doc, false);
                RhinoToCoreModel.AddOrUpdateRoads(plineObjs);
                CoreModel.RefreshStudyAreaMetrics();
                RefreshActiveStudyAndMetrics();
                CoreModelToRhino.OutputCoreModelToRhino(doc);
                CoreModelToRhino.ToggleLayerStateBasedOnAppMode(doc, AppMode.Road);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateDisplayedStudyAreaData(StudyArea study)
        {
            try
            {
                if (study == null) return;

                UpdateLandUseGraphData(study.Metrics);
                UpdateRoadLengthGraphData(study.Metrics);
                UpdateRoadAreaGraphData(study.Metrics);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        public void RefreshActiveStudyAndMetrics()
        {
            if(CoreModel.TryGetStudyArea(_activeStudy.Id, out StudyArea study))
                ActiveStudy = study;

            UpdateDisplayedStudyAreaData(ActiveStudy);
        }

        private void UpdateLandUseGraphData_OLD(StudyAreaMetrics studyAndMetric)
        { 
            var allUses = Enum.GetValues<Landuse>();
            List<HorizontalGraphData> graphData = new();

            if(!studyAndMetric.ParcelMetrics.Any()) return;

            var areaPerUse = studyAndMetric.ParcelMetrics
                .GroupBy(p => p.Use)
                .ToDictionary(grp => grp.Key, grp => grp.Sum(p => p.Area));

            double maxArea = areaPerUse.Max(kvp => kvp.Value);

            foreach(var use in allUses)
            {
                double useArea = studyAndMetric.ParcelMetrics.Where(p => p.Use == use)
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

            LandUseGraphData = graphData;
        }
    }
}
