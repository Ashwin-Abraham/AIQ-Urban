using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Metrics;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using ArchSitePlan.UI.Actions;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.CoreModelInterface;
using ArchSitePlan.UI.Extensions;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.InputValidation;
using ArchSitePlan.UI.Models;
using ArchSitePlan.UI.RhinoBHoMInterop;
using BH.Engine.Geometry;
using LiveCharts;
using LiveCharts.Wpf;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using static ArchSitePlan.Core.Constants.Enums;
using static ArchSitePlan.UI.Constants.Enums;
using MessageBox = System.Windows.Forms.MessageBox;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace ArchSitePlan.UI
{
    public record VMActiveParcelsDetails(
        string Use,
        string TotalArea,
        string BUA,
        string CoveragePctg,
        string FAR);

    public class MainViewModel : ViewModelBase, INotifyPropertyChanged
    {
        #region VARIABLES

        public static string RoadInstruction { get; set; } = UIMessages.RoadInstruction;
        public static string ChangeLandUseInstruction { get; set; } = UIMessages.ChangeLandUseInstruction;
        public static string AddDividerInstruction { get; set; } = UIMessages.AddDividerInstruction;
        public static string AddBuildingInstruction { get; set; } = UIMessages.AddBuildingInstruction;
        public static string AddStudyInstruction { get; set; } = UIMessages.AddStudyInstruction;
        public static string SetExcelPathInstruction { get; set; } = UIMessages.SetExcelPathInstruction;
        public static string ExportExcelInstruction { get; set; } = UIMessages.ExportExcelInstruction;

        private bool _enableFloorCountSlider = true;
        public bool EnableFloorCountSlider
        {
            get => _enableFloorCountSlider;
            set
            {
                _enableFloorCountSlider = value;
                OnPropertyChanged(nameof(EnableFloorCountSlider));
            }
        }

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

        private bool _appEnabled = false;
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
                SetAppMode(AppMode.Road, RoadExpanderOpen);
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
                SetAppMode(AppMode.LandUse, LanduseExpanderOpen);
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
                SetAppMode(AppMode.Export, ExportsExpanderOpen);
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
                SetAppMode(AppMode.Building, BuildingsExpanderOpen);
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
                SetAppMode(AppMode.StudyArea, StudyAreaExpanderOpen);
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
                UpdateBuildingHeight(_activeBuildingFloorCount, ActiveBuildingFloorHeight);
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
                UpdateBuildingHeight(ActiveBuildingFloorCount, _activeBuildingFloorHeight);
            }
        }

        private double _activeBuildingHeight;
        public double ActiveBuildingHeight
        {
            get => _activeBuildingHeight;
            set
            {
                _activeBuildingHeight = Math.Round(value, 2);
                OnPropertyChanged(nameof(ActiveBuildingHeight));
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
            AppColours.LandUseColours[Landuse.Undefined].ToWindowsMediaColor());
        public SolidColorBrush SelectedLandUseColour
        {
            get => _selectedLandUseColour;
            set
            {
                _selectedLandUseColour = value;
                OnPropertyChanged(nameof(SelectedLandUseColour));
            }
        }

        public BuildingSelectorMode BuildingSelectorMode { get; set; } = BuildingSelectorMode.Footprint;


        private bool _buildingFootprintSelectorModeActive = true;
        public bool BuildingFootprintSelectorModeActive
        {
            get => _buildingFootprintSelectorModeActive;
            set
            {
                _buildingFootprintSelectorModeActive = value;
                OnPropertyChanged(nameof(BuildingFootprintSelectorModeActive));

                if (_buildingFootprintSelectorModeActive)
                    BuildingSelectorMode = BuildingSelectorMode.Footprint;

            }
        }

        private bool _buildingMassSelectorModeActive = false;
        public bool BuildingMassSelectorModeActive
        {
            get => _buildingMassSelectorModeActive;
            set
            {
                _buildingMassSelectorModeActive = value;
                OnPropertyChanged(nameof(BuildingMassSelectorModeActive));

                if (_buildingMassSelectorModeActive)
                    BuildingSelectorMode = BuildingSelectorMode.Mass;
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
                var study = CoreModel.Studies.Find(s => s.Name == value);
                if (study is not null)
                    ActiveStudy = study;

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

        private string _excelPath =  Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
            $"{RhinoDoc.ActiveDoc.Name}_ProjectReport.xlsx");

        public string ExcelPath
        {
            get => _excelPath;
            set
            {
                _excelPath = value;
                OnPropertyChanged(nameof(ExcelPath));
            }
        }
        private bool _builtOrLandArea = false;
        public bool BuiltOrLandArea
        {
            get => _builtOrLandArea;
            set
            {
                _builtOrLandArea = value;
                OnPropertyChanged(nameof(BuiltOrLandArea));
                UpdateDisplayedStudyAreaData(_activeStudy);
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
        public ICommand AddParcelDivider_Cmd { get; }
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
            this.AddParcelDivider_Cmd = new RelayCommand(AddParcelDivider_Exec);

            RhinoToCoreModel.ResetStudyAreas();
            ActiveStudy = CoreModel.Studies.FirstOrDefault();
            StudyAreasNames = CoreModel.Studies.Select(s => s.Name).ToList();
            ActiveStudyAreaName = ActiveStudy.Name;
        }

        private void AddParcelDivider_Exec(object obj)
        {
            try
            {
                RhinoDoc doc = RhinoDoc.ActiveDoc;
                int parcelDividerLayerIndex = LayerHelpers.CreateOrGetLayerIndex(
                    doc,
                    LayerConstants.ParcelDividerLayer,
                    LayerConstants.ParcelDividerColour);

                // Try to select a curve
                var rhinoObjs = doc.Objects.GetSelectedObjects(false, false).ToList();
                if (rhinoObjs.Count == 0)
                    throw new Exception(UIMessages.AddDividerInstruction);

                var rhinoCrvs = rhinoObjs
                    .Where(obj => obj.ObjectType == ObjectType.Curve
                        && (obj.Geometry as Curve)!.Degree < 2)
                    .ToList();

                int removed = rhinoObjs.Count() - rhinoCrvs.Count;
                
                List<DividerData> dividerData = new();

                foreach (var rhinoObj in rhinoCrvs)
                {
                    var bhomCrv = (rhinoObj.Geometry as Curve)!.ToBHoM2DegreeCrv();
                    if (bhomCrv is null || bhomCrv.IIsSelfIntersecting())
                    {
                        removed++;
                        continue;
                    }

                    dividerData.Add(new(bhomCrv, rhinoObj.Id));
                    LayerHelpers.ChangeLayer(rhinoObj, parcelDividerLayerIndex);
                }

                if (removed > 0)
                    MessageBox.Show($"Removed {removed} objects from selection that were either" +
                        $"\n 1. Not lines or polylines " +
                        $"\n 2. Self intersecting");

                if (!dividerData.Any()) return;

                ModelBuilderPipelines.AddOrUpdateParcelDividerPipeline(doc, dividerData);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ToggleEnableApp_Exec(object obj)
        {
            try
            {
                AppEnabled = !AppEnabled;

                bool expanderOpen = AppMode != AppMode.None;
                AppMode = AppMode.None;

                SetAppMode(AppMode, expanderOpen);

                if (!AppEnabled || AppMode == AppMode.None)
                {
                    // Hard lock to avoid complex refactor
                    CoreModelToRhino.ToggleLockModelLayers(RhinoDoc.ActiveDoc, true);
                    return;
                }

                ModelBuilderPipelines.ReloadAppPipeline(RhinoDoc.ActiveDoc);
                ActiveStudy = CoreModel.Studies.First();
                ActiveStudyAreaName = ActiveStudy.Name;
                StudyAreasNames = CoreModel.Studies.Select(s => s.Name).ToList();
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
                        string rhinoName = RhinoDoc.ActiveDoc.Name;
                        rhinoName = rhinoName is null || rhinoName == "" ? 
                            "Untitled" : rhinoName;

                        // Get the selected folder path
                        ExcelPath = Path.Combine(
                            folderBrowserDialog.SelectedPath,
                            $"{rhinoName}_ProjectReport.xlsx");
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
                var location = Path.GetDirectoryName(ExcelPath);
                if (!Path.Exists(location))
                    throw new Exception($"Excel path - {ExcelPath} is invalid. Please select a valid export location.");

                var pr = new ProjectReport(ExcelPath, CoreModel.ProjectWideStudy().Metrics, CoreModel.Studies);
                pr.WriteExcelFile();

                string? folder = Path.GetDirectoryName(ExcelPath);
                if (folder is not null)
                    Process.Start("explorer.exe", folder); 
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ChangeParcelUse_Exec(object o)
        {
            try
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

                if (srfObjs.Count == 0)
                    throw new Exception(UIMessages.ChangeLandUseInstruction);

                ModelBuilderPipelines.ChangeParcelUsePipeline(doc,
                    srfObjs.Select(s => s.Id).ToList(),
                    SelectedLandUse);

                RefreshActiveStudyAndMetrics();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
          
        }

        private void UpdateLandUseGraphData(StudyAreaMetrics metrics)
        {
            var groupedByUse = metrics.ParcelMetrics.GroupBy(p => p.Use);
            ParcelSeriesCollection = new SeriesCollection();
            foreach(var landUseGroup in groupedByUse)
            {
                var area = _builtOrLandArea ?
                    Math.Round(landUseGroup.Sum(p => p.BUA))
                    : Math.Round(landUseGroup.Sum(p => p.Area));
                var use = landUseGroup.First().Use;
                
                ParcelSeriesCollection.Add(new ColumnSeries()
                {
                    Title = landUseGroup.First().Use.ToString(),
                    Values = new ChartValues<double> { area },
                    Fill = new SolidColorBrush(AppColours.LandUseColours[use].ToWindowsMediaColor()),
                    Height = 40,
                    
                });
            }
            GraphHeight = groupedByUse.Count() * 12 + 50;
        }

        private void UpdateRoadLengthGraphData(StudyAreaMetrics metrics)
        {
            var groupedByWidth = metrics.TransitMetrics.GroupBy(p => Math.Round(p.RoadWidth,1));
            RoadLengthSeries = new SeriesCollection();
            var widthColors = CoreModel.SetRoadColorsByWidth();

            foreach (var transitGroup in groupedByWidth)
            {
                var length = Math.Round(transitGroup.Sum(p => p.RunningLength));
                var label = "Width (" + Math.Round(transitGroup.First().RoadWidth, 1).ToString() +"m)";

                var newRow = new PieSeries()
                {
                    Title = label,
                    Values = new ChartValues<double> { length },
                };

                widthColors.TryGetValue(transitGroup.Key, out Color color);
                if (color != default)
                    newRow.Fill = new SolidColorBrush(color.ToWindowsMediaColor());

                RoadLengthSeries.Add(newRow);
            }
        }

        
        private void UpdateRoadAreaGraphData(StudyAreaMetrics metrics)
        {
            var groupedByWidth = metrics.TransitMetrics.GroupBy(p => Math.Round(p.RoadWidth, 1));
            RoadAreaSeries = new SeriesCollection();

            var widthColors = CoreModel.SetRoadColorsByWidth();

            foreach (var transitGroup in groupedByWidth)
            {
                var area = Math.Round(transitGroup.Sum(p => p.Area));
                var label = "Width (" + Math.Round(transitGroup.First().RoadWidth, 1).ToString() + "m)";
                

                var newRow = new PieSeries()
                {
                    Title = label,
                    Values = new ChartValues<double> { area },
                };

                widthColors.TryGetValue(transitGroup.Key, out Color color);
                if (color != default)
                    newRow.Fill = new SolidColorBrush(color.ToWindowsMediaColor());

                RoadAreaSeries.Add(newRow);
            }
        }

        private void Test_Exec(object obj)
        {
            try
            {
                var doc = RhinoDoc.ActiveDoc;
                var selectedObjectCenter = doc.Objects.GetSelectedObjects(false, false)
                    .First()
                    .Geometry
                    .GetBoundingBox(false)
                    .Center;

                var viewTopLeft = doc.Views.ActiveView.ActiveViewport.ClientToScreen(new Point2d(0,0));
                var viewportPoint = doc.Views.ActiveView.ActiveViewport
                    .WorldToClient(selectedObjectCenter)
                    + new Point2d(viewTopLeft.X, viewTopLeft.Y);

                // Create the dialog window
                TestPopUp dialog = new();

                // Show the dialog
                dialog.ShowDialog();


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
                var rhinoObjs = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false).ToList();
                
                if (rhinoObjs.Count != 1 || rhinoObjs[0].ObjectType != ObjectType.Curve) 
                    throw new Exception(UIMessages.AddStudyInstruction);

                var rhinoObj = rhinoObjs[0];
                var boundary = rhinoObj.Geometry as Curve;

                if (!boundary.IsClosed)
                    throw new Exception("Error. Please select closed curve instead");
                
                if (ActiveStudyAreaName == _defaultStudyAreaNameTextBox
                    || ActiveStudyAreaName == string.Empty)
                    throw new Exception("Error. Please enter a study name");

                if (boundary is null || boundary.Degree > 1)
                    throw new Exception("Select closed polylines only.");
                
                var study = CoreModel.Studies.Find(s => s.Name == ActiveStudyAreaName);
                
                if(study!=null && study.Id != rhinoObj.Id)
                    throw new Exception($"Another study named \"{ActiveStudyAreaName}\" already exists.");
                
                var bhomCrv = boundary.ToBHoM2DegreeCrv();
                
                RhinoDoc doc2 = RhinoDoc.ActiveDoc;
                ModelBuilderPipelines.AddStudyAreaPipeline(doc: doc2,
                    studyData: new StudyData(bhomCrv!, rhinoObj.Id, ActiveStudyAreaName).ObjectToList());
                
                ActiveStudy = CoreModel.Studies.FirstOrDefault(s => rhinoObj.Id == s.Id);

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

                var allowedTypes = new List<ObjectType>
                {
                    ObjectType.Curve, ObjectType.Brep, ObjectType.Mesh, ObjectType.Mesh
                };

                var rhinoObjs = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false)
                    .Where(obj => allowedTypes.Contains(obj.ObjectType))
                    .ToList();

                var validation = new BuildingInputValidation(BuildingSelectorMode)
                    .ValidateInput(rhinoObjs);

                if (validation.Result != ValidationResult.All_Valid)
                    MessageBox.Show(validation.Message);

                if (validation.ValidObjects.Count == 0) return; 

                var rhinoBuildingData = validation.ValidObjects
                    .Select(o => RhinoBuildingData.FromRhinoObject(o, ActiveBuildingFloorCount, ActiveBuildingFloorHeight))
                    .ToList();

                RhinoEventHandler.UpdateSelectedBuildingsPosition(rhinoBuildingData);
                RefreshActiveStudyAndMetrics();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }

        private void SetAppMode(AppMode changedExpander, bool expanderOpen)
        {
            // Set current mode
            if (!expanderOpen)
            {
                // If expander is closed while the app mode is unchanged, it means wwe have closed all expanders
                if (AppMode == changedExpander) AppMode = AppMode.None;
                if (AppMode == AppMode.None)
                    CoreModelToRhino.ToggleLockModelLayers(RhinoDoc.ActiveDoc, true);

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
            if (AppColours.LandUseColours.TryGetValue(use, out Color color))
                SelectedLandUseColour = new SolidColorBrush(color.ToWindowsMediaColor());
            else
                SelectedLandUseColour = new SolidColorBrush(AppColours.LandUseColours[Landuse.Undefined]
                    .ToWindowsMediaColor());
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

                if (rhinoObjs.Count == 0)
                    throw new Exception(UIMessages.RoadInstruction);

                int roadIndex = RhinoObjAttributeConstants.RoadGeometryAttributes.LayerIndex;

                List<TransitData> transitData = new();

                foreach(var rhinoObj in rhinoObjs)
                {
                    var curve = rhinoObj.Geometry as Curve;
                    if (curve == null)
                        throw new Exception(UIMessages.RoadInstruction);

                    var bhomCrv = curve.ToBHoM2DegreeCrv();
                    if (bhomCrv == null || bhomCrv.IIsSelfIntersecting())
                        throw new Exception(UIMessages.RoadInstruction);

                    transitData.Add(new(bhomCrv, rhinoObj.Id, RoadWidth));
                    LayerHelpers.ChangeLayer(rhinoObj, roadLayerIndex);
                }

                if (!transitData.Any()) return;

                ModelBuilderPipelines.AddOrUpdateRoadPipeline(doc, transitData);
                RefreshActiveStudyAndMetrics();
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
            StudyAreasNames = CoreModel.Studies.Select(s => s.Name).ToList();

            if (_activeStudy != null
                && CoreModel.TryGetStudyArea(_activeStudy.Id, out StudyArea study))
                ActiveStudy = study;
            else if(CoreModel.Studies.Exists(s => s !=null))
                ActiveStudy = CoreModel.Studies[0];

            ActiveStudyAreaName = ActiveStudy.Name;
            UpdateDisplayedStudyAreaData(ActiveStudy);
        }

        private void UpdateBuildingHeight(double floorCount, double floorHeight)
        {
            ActiveBuildingHeight = floorCount * floorHeight;
        }
    }
}
