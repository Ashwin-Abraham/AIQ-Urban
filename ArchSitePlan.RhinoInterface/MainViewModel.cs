using System.Diagnostics;
using System.Windows.Input;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using ArchSitePlan.RhinoInterface.Actions;
using ArchSitePlan.RhinoInterface.Constants;
using ArchSitePlan.RhinoInterface.CoreModelInterface;
using ArchSitePlan.RhinoInterface.Helpers;
using ArchSitePlan.RhinoInterface.HTML_UI;
using ArchSitePlan.RhinoInterface.InputValidation.Results;
using ArchSitePlan.RhinoInterface.InputValidation.Validators;
using ArchSitePlan.RhinoInterface.Models;
using ArchSitePlan.RhinoInterface.RhinoBHoMInterop;
using BH.Engine.Geometry;
using Eto.Forms;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using static ArchSitePlan.RhinoInterface.Constants.Enums;

namespace ArchSitePlan.RhinoInterface;
public record VMActiveParcelsDetails(
    string Use,
    string TotalArea,
    string BUA,
    string CoveragePctg,
    string FAR);

public class MainViewModel // ViewModelBase, INotifyPropertyChanged
{
    #region VARIABLES
    private string _licenseKey = "licenseKey";

    public string LicenseKey
    {
        get
        {
            return _licenseKey;
        }
        set
        {
            _licenseKey = value;
            OnPropertyChanged(nameof(LicenseKey));
        }
    }
    private string _customerID = "customerID";
    public string CustomerID
    {
        get => _customerID;
        set
        {
            _customerID = value;
            OnPropertyChanged(nameof(CustomerID));
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
    public ModellingTool AppMode = ModellingTool.None;

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
            SetAppMode(ModellingTool.Transit, RoadExpanderOpen);
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
            SetAppMode(ModellingTool.LandUse, LanduseExpanderOpen);
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
            SetAppMode(ModellingTool.Export, ExportsExpanderOpen);
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
            SetAppMode(ModellingTool.Building, BuildingsExpanderOpen);
        }
    }

    private void OnPropertyChanged(string v)
    {
        throw new NotImplementedException();
    }

    private bool _studyAreaExpanderOpen = false;
    public bool StudyAreaExpanderOpen
    {
        get => _studyAreaExpanderOpen;
        set
        {
            _studyAreaExpanderOpen = value;
            OnPropertyChanged(nameof(StudyAreaExpanderOpen));
            SetAppMode(ModellingTool.Zones, StudyAreaExpanderOpen);
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

    private List<string> _landuses = AppLanduses.Uses.Select(x => x.Name).ToList();
    public List<string> Landuses
    {
        get => _landuses;
        set => _landuses = value;
    }

    private string _selectedLanduse = AppLanduses.Undefined.Name;
    public string SelectedLandUse
    {
        get => _selectedLanduse;
        set
        {
            _selectedLanduse = value;
            OnPropertyChanged(nameof(SelectedLandUse));
            UpdateSelectedLanduseColour(_selectedLanduse);
        }
    }

    //private SolidColorBrush _selectedLandUseColour = new SolidColorBrush(
    //    AppLanduses.Undefined.Color.ToWindowsMediaColor());

    //public SolidColorBrush SelectedLandUseColour
    //{
    //    get => _selectedLandUseColour;
    //    set
    //    {
    //        _selectedLandUseColour = value;
    //        OnPropertyChanged(nameof(SelectedLandUseColour));
    //    }
    //}

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
    //private SeriesCollection _roadLengthSeries;
    //public SeriesCollection RoadLengthSeries
    //{
    //    get => _roadLengthSeries;
    //    set
    //    {
    //        _roadLengthSeries = value;
    //        OnPropertyChanged(nameof(RoadLengthSeries));
    //    }
    //}

    //private SeriesCollection _roadAreaSeries;
    //public SeriesCollection RoadAreaSeries
    //{
    //    get => _roadAreaSeries;
    //    set
    //    {
    //        _roadAreaSeries = value;
    //        OnPropertyChanged(nameof(RoadAreaSeries));
    //    }
    //}

    //private SeriesCollection _parcelSeriesCollection;
    //public SeriesCollection ParcelSeriesCollection
    //{
    //    get => _parcelSeriesCollection;
    //    set
    //    {
    //        _parcelSeriesCollection = value;
    //        OnPropertyChanged(nameof(ParcelSeriesCollection));
    //    }
    //}

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
    public ICommand ContactMe_Cmd { get; }
    #endregion

    public MainViewModel()
    {
    //    this.AddOrUpdateRoads_Cmd = new RelayCommand(AddOrUpdateRoads_Exec);
    //    this.UpdateBuilding_Cmd = new RelayCommand(UpdateBuilding_Exec);
    //    this.AddStudyArea_Cmd = new RelayCommand(AddStudyArea_Exec);
    //    this.Test_Cmd = new RelayCommand(Test_Exec);
    //    this.ChangeParcelUse_Cmd = new RelayCommand(ChangeParcelUse_Exec);
    //    this.ExportExcel_Cmd = new RelayCommand(ExportExcel_Exec);
    //    this.ToggleEnableApp_Cmd = new RelayCommand(ToggleEnableApp_Exec);
    //    this.SetExcelPath_Cmd = new RelayCommand(SetExcelPath_Exec);
    //    this.AddParcelDivider_Cmd = new RelayCommand(AddParcelDivider_Exec);
    //    this.ContactMe_Cmd = new RelayCommand(ContactMe_Exec);

        CoreModel.ResetStudyAreas();
        ActiveStudy = CoreModel.Studies.FirstOrDefault();
        StudyAreasNames = CoreModel.Studies.Select(s => s.Name).ToList();
        ActiveStudyAreaName = ActiveStudy.Name;
    }

    private void ContactMe_Exec(object obj)
    {
        string linkedInLink = "https://www.linkedin.com/in/ashwin-abraham-gd/";
        // Open email client with my email ID
        Process.Start(new ProcessStartInfo(linkedInLink) { UseShellExecute = true });
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

            ModelBuilderPipelines.AddOrUpdateParcelDividerPipeline(doc, dividerData, AppMode);
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
            ServerComms.StartServer();
            VueUI.OpenWindow();

            RhinoDoc doc = RhinoDoc.ActiveDoc;
            AppEnabled = !AppEnabled;

            bool expanderOpen = AppMode != ModellingTool.None;
            AppMode = ModellingTool.None;

            SetAppMode(AppMode, expanderOpen);

            if (!AppEnabled || AppMode == ModellingTool.None)
            {
                // Hard lock to avoid complex refactor
                LayerManager.ToggleLockModelLayers(doc, true);
                return;
            }

            ModelBuilderPipelines.ReloadAppPipeline(doc, AppMode);
            ActiveStudy = CoreModel.Studies.First();
            ActiveStudyAreaName = ActiveStudy.Name;
            StudyAreasNames = CoreModel.Studies.Select(s => s.Name).ToList();
            LayerManager.ToggleLayerStateBasedOnAppMode(doc, AppMode);
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
            MessageBox.Show("Excel dialog box not implemented");

            //using (SelectFolderDialog folderBrowserDialog = new SelectFolderDialog())
            //{
            //    folderBrowserDialog.Title = "Set export location";

            //    if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            //    {
            //        string rhinoName = RhinoDoc.ActiveDoc.Name;
            //        rhinoName = rhinoName is null || rhinoName == "" ? 
            //            "Untitled" : rhinoName;

            //        // Get the selected folder path
            //        ExcelPath = Path.Combine(
            //            folderBrowserDialog.SelectedPath,
            //            $"{rhinoName}_ProjectReport.xlsx");
            //    }
            //}
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
                AppLanduses.GetUse(SelectedLandUse),
                AppMode);

            RefreshActiveStudyAndMetrics();
        }
        catch(Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
          
    }

    //private void UpdateLandUseGraphData(StudyAreaMetrics metrics)
    //{
    //    var groupedByUse = metrics.ParcelMetrics.GroupBy(p => p.Use);
    //    ParcelSeriesCollection = new SeriesCollection();
    //    foreach(var landUseGroup in groupedByUse)
    //    {
    //        var area = _builtOrLandArea ?
    //            Math.Round(landUseGroup.Sum(p => p.BUA))
    //            : Math.Round(landUseGroup.Sum(p => p.Area));
    //        var use = landUseGroup.First().Use;
                
    //        ParcelSeriesCollection.Add(new ColumnSeries()
    //        {
    //            Title = landUseGroup.First().Use.ToString(),
    //            Values = new ChartValues<double> { area },
    //            Fill = new SolidColorBrush(use.Color.ToWindowsMediaColor()),
    //            Height = 40,
                    
    //        });
    //    }
    //    GraphHeight = groupedByUse.Count() * 12 + 50;
    //}

    //private void UpdateRoadLengthGraphData(StudyAreaMetrics metrics)
    //{
    //    var groupedByWidth = metrics.TransitMetrics.GroupBy(p => Math.Round(p.RoadWidth,1));
    //    RoadLengthSeries = new SeriesCollection();
    //    var widthColors = CoreModel.SetRoadColorsByWidth();

    //    foreach (var transitGroup in groupedByWidth)
    //    {
    //        var length = Math.Round(transitGroup.Sum(p => p.RunningLength));
    //        var label = "Width (" + Math.Round(transitGroup.First().RoadWidth, 1).ToString() +"m)";

    //        var newRow = new PieSeries()
    //        {
    //            Title = label,
    //            Values = new ChartValues<double> { length },
    //        };

    //        widthColors.TryGetValue(transitGroup.Key, out Color color);
    //        if (color != default)
    //            newRow.Fill = new SolidColorBrush(color.ToWindowsMediaColor());

    //        RoadLengthSeries.Add(newRow);
    //    }
    //}

        
    //private void UpdateRoadAreaGraphData(StudyAreaMetrics metrics)
    //{
    //    var groupedByWidth = metrics.TransitMetrics.GroupBy(p => Math.Round(p.RoadWidth, 1));
    //    RoadAreaSeries = new SeriesCollection();

    //    var widthColors = CoreModel.SetRoadColorsByWidth();

    //    foreach (var transitGroup in groupedByWidth)
    //    {
    //        var area = Math.Round(transitGroup.Sum(p => p.Area));
    //        var label = "Width (" + Math.Round(transitGroup.First().RoadWidth, 1).ToString() + "m)";
                

    //        var newRow = new PieSeries()
    //        {
    //            Title = label,
    //            Values = new ChartValues<double> { area },
    //        };

    //        widthColors.TryGetValue(transitGroup.Key, out Color color);
    //        if (color != default)
    //            newRow.Fill = new SolidColorBrush(color.ToWindowsMediaColor());

    //        RoadAreaSeries.Add(newRow);
    //    }
    //}

    private void Test_Exec(object obj)
    {
        try
        {
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
                studyData: [new StudyData(bhomCrv!, rhinoObj.Id, ActiveStudyAreaName)],
                AppMode);
                
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
                ObjectType.Curve, ObjectType.Brep, ObjectType.Mesh, ObjectType.Extrusion
            };

            var rhinoObjs = doc.Objects.GetSelectedObjects(false, false)
                .Where(obj => allowedTypes.Contains(obj.ObjectType))
                .ToList();

            var validation = new BuildingInputValidator(BuildingSelectorMode)
                .ValidateInput(rhinoObjs);

            if (validation.Result != ValidationResult.All_Valid)
                MessageBox.Show(validation.Message);

            if (validation.ValidObjects.Count == 0) return; 

            var rhinoBuildingData = validation.ValidObjects
                .Select(o => RhinoBuildingData.FromRhinoObject(o, ActiveBuildingFloorCount, ActiveBuildingFloorHeight))
                .ToList();

            DocObjectHelpers.DeleteRhinoObjectsIfPresent(doc, rhinoObjs.Select(r => r.Id));
            RhinoEventHandler.UpdateSelectedBuildingsPosition(rhinoBuildingData);
            RefreshActiveStudyAndMetrics();
        }
        catch(Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
            
    }

    private void SetAppMode(ModellingTool changedExpander, bool expanderOpen)
    {
        // Set current mode
        if (!expanderOpen)
        {
            // If expander is closed while the app mode is unchanged, it means wwe have closed all expanders
            if (AppMode == changedExpander) AppMode = ModellingTool.None;
            if (AppMode == ModellingTool.None)
                LayerManager.ToggleLockModelLayers(RhinoDoc.ActiveDoc, true);

            // Early return to avoid cyclic SetSelectionMode calls for expander closure
            return;
        }
        if (expanderOpen)
            AppMode = changedExpander;

        // Close other expanders
        if (AppMode != ModellingTool.Transit && RoadExpanderOpen)
            RoadExpanderOpen = false;
        if (AppMode != ModellingTool.LandUse && LanduseExpanderOpen)
            LanduseExpanderOpen = false;
        if (AppMode != ModellingTool.Building && BuildingsExpanderOpen)
            BuildingsExpanderOpen = false;
        if (AppMode != ModellingTool.Zones && StudyAreaExpanderOpen)
            StudyAreaExpanderOpen = false;
        if (AppMode != ModellingTool.Export && ExportsExpanderOpen)
            ExportsExpanderOpen = false;

        if(expanderOpen)
            LayerManager.ToggleLayerStateBasedOnAppMode(RhinoDoc.ActiveDoc, changedExpander);

        RhinoDoc.ActiveDoc.Views.Redraw();
    }

    private void UpdateSelectedLanduseColour(string use)
    {
        //var landuse = AppLanduses.GetUse(use);
        //SelectedLandUseColour = new SolidColorBrush(landuse.Color.ToWindowsMediaColor());
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

            ModelBuilderPipelines.AddOrUpdateRoadPipeline(doc, transitData, AppMode);
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

            //UpdateLandUseGraphData(study.Metrics);
            //UpdateRoadLengthGraphData(study.Metrics);
            //UpdateRoadAreaGraphData(study.Metrics);
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
