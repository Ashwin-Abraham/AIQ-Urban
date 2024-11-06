using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Metrics;
using ArchSitePlan.Core.Models;
using ArchSitePlan.UI.Actions;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.CoreModelInterface;
using ArchSitePlan.UI.Extensions;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.Models;
using ArchSitePlan.UI.RhinoBHoMInterop;
using LiveCharts;
using LiveCharts.Wpf;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.UI;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
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

                EnableFloorCountSlider = !_buildingMassSelectorModeActive;
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
                var rhinoObjs = doc.Objects.GetSelectedObjects(false, false)
                    .Where(obj => obj.ObjectType == ObjectType.Curve)
                    .ToList();

                List<(BH.oM.Geometry.Polyline?, Guid)> plineObjs = new();

                foreach (var rhinoObj in rhinoObjs)
                {
                    Curve? crv = rhinoObj.Geometry as Curve;
                    Polyline? pline = null;

                    if (crv is not null && 
                        !crv.TryGetPolyline(out pline)) continue;

                    plineObjs.Add((pline?.ToBHoMPolyline(), rhinoObj.Id));
                    LayerHelpers.ChangeLayer(rhinoObj, parcelDividerLayerIndex);
                }

                if (!plineObjs.Any()) return;

                ModelBuilderPipelines.AddOrUpdateParcelDividerPipeline(doc, plineObjs);
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

                SetSelectionMode(AppMode, expanderOpen);

                if (!AppEnabled)
                {
                    // Hard lock to avoid complex refactor
                    CoreModelToRhino.ToggleLockModelLayers(RhinoDoc.ActiveDoc, AppEnabled);
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
                            $"{rhinoName}ProjectReport.xlsx");
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

            ModelBuilderPipelines.ChangeParcelUsePipeline(doc, 
                srfObjs.Select(s => s.Id).ToList(),
                SelectedLandUse);
            
            RefreshActiveStudyAndMetrics();
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
                    Fill = new SolidColorBrush(Defaults.LandUseColours[use].ToWindowsColor()),
                    Height = 40,
                    
                });
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
                var doc = RhinoDoc.ActiveDoc;
                var selectedObjectCenter = doc.Objects.GetSelectedObjects(false, false)
                    .First()
                    .Geometry
                    .GetBoundingBox(false)
                    .Center;

                var viewTopLeft = doc.Views.ActiveView.ActiveViewport.ClientToScreen(new Point2d(0,0));
                //var viewTopLeft = new Point2d(viewportRect.Left, viewportRect.Top);
                var viewportPoint = doc.Views.ActiveView.ActiveViewport
                    .WorldToClient(selectedObjectCenter)
                    + new Point2d(viewTopLeft.X, viewTopLeft.Y);

                // Create the dialog window
                TestPopUp dialog = new();
                //{
                //    Left = 50,
                //    Top = 50,
                //    Width = 300,
                //    Height = 200
                //};

                // Show the dialog
                dialog.ShowDialog();

                //using (Form dialog = new Form())
                //{
                //    dialog.Text = "Dialog at Center Point";
                //    dialog.Size = new System.Drawing.Size(200, 100);
                //    dialog.StartPosition = FormStartPosition.Manual;
                //    dialog.Location = new System.Drawing.Point(
                //        (int)viewportPoint.X,
                //        (int)viewportPoint.Y);
                //    //dialog.Location = new System.Drawing.Point(50, 50);
                //    dialog.FormBorderStyle = FormBorderStyle.None;
                //    dialog.AllowTransparency = true;
                //    dialog.Opacity = 0.75;

                //    Label label = new Label
                //    {
                //        Text = "Hello Harshitha!",
                //        Dock = DockStyle.Fill,
                //        TextAlign = System.Drawing.ContentAlignment.MiddleCenter
                //    };
                //    dialog.Controls.Add(label);

                //    // Show the dialog
                //    dialog.ShowDialog();
                //}
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

                var study = CoreModel.Studies.Find(s => s.Name == ActiveStudyAreaName);
                if(study!=null && study.Id != rhinoObj.Id)
                    throw new Exception($"Another study named \"{ActiveStudyAreaName}\" already exists.");

                RhinoDoc doc2 = RhinoDoc.ActiveDoc;
                ModelBuilderPipelines.AddStudyAreaPipeline(doc: doc2,
                    studyData: (rhinoPline.ToBHoMPolyline(),
                        rhinoObj.Id,
                        ActiveStudyAreaName).ObjectToList());
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

                RhinoEventHandler.UpdateSelectedBuildings(rhinoObjs,
                    ActiveBuildingFloorCount,
                    ActiveBuildingFloorHeight);

                RefreshActiveStudyAndMetrics();
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

                int roadIndex = RhinoObjAttributeConstants.RoadGeometryAttributes.LayerIndex;

                List<(BH.oM.Geometry.ICurve?, Guid)> CLAndID = new();

                foreach(var rhinoObj in rhinoObjs)
                {
                    var curve = rhinoObj.Geometry as Curve;
                    if (curve == null) continue;

                    var bhomCrv = curve.ToBHoM2DegreeCrv();
                    if (bhomCrv == null) continue;

                    CLAndID.Add((bhomCrv, rhinoObj.Id));
                    LayerHelpers.ChangeLayer(rhinoObj, roadLayerIndex);
                }

                if (!CLAndID.Any()) return;

                ModelBuilderPipelines.AddOrUpdateRoadPipeline(doc, CLAndID, RoadWidth);
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
    }
}
