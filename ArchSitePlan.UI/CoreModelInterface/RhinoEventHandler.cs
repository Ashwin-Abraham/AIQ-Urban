using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.CoreModelInterface;
using ArchSitePlan.UI.Extensions;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.HTML_UI.HtmlCommsClasses;
using ArchSitePlan.UI.InputValidation.Results;
using ArchSitePlan.UI.InputValidation.Validators;
using ArchSitePlan.UI.Models;
using ArchSitePlan.UI.RhinoBHoMInterop;
using ArchSitePlan.UI.UsageLogs;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Runtime.InteropServices;
using System.Windows;
using static ArchSitePlan.UI.Constants.Enums;
using Plane = Rhino.Geometry.Plane;
using Vector = BH.oM.Geometry.Vector;

namespace ArchSitePlan.UI.Actions
{

    public static class RhinoEventHandler
    {
        public static UsageLog AppUsageLog = new();
        public static bool EventLock = false;
        public static bool ValidLicense = true;

        public static MainViewModel WPFViewModel;
        public static UIViewModel HTMLViewModel = new();

        private static bool _isReplacement = false;

        private static List<RhinoObject> _objectsForReplace = new();
        private static List<RhinoObject> _objectsForAddition = new();
        private static List<RhinoObject> _objectsForRemoval = new();
        private static List<RhinoObject> _objectsForUndelete = new();
        
        static RhinoEventHandler()
        {
            RhinoApp.Idle += ProcessEventPipepline;
            RhinoDoc.ReplaceRhinoObject += (sender, args) => QueueObjectForReplacement(args);
            RhinoDoc.AddRhinoObject += (sender, args) => QueueObjectForAddition(args); 
            RhinoDoc.DeleteRhinoObject += (sender, args) => QueueObjectForRemoval(args);
            RhinoDoc.UndeleteRhinoObject += (sender, args) => QueueObjectForUndelete(args);

            RhinoDoc.SelectObjects += (sender, args) => OnRhinoDocObjectSelection(args);
            RhinoDoc.DeselectObjects += (sender, args) => OnRhinoDocObjectDeSelection(args);
            RhinoDoc.DeselectAllObjects += (sender, args) => OnAllRhinoDocObjectDeSelection(args);
        }

        private static void QueueObjectForUndelete(RhinoObjectEventArgs args)
        {
            _objectsForUndelete.Add(args.TheObject);
        }

        private static void QueueObjectForRemoval(RhinoObjectEventArgs args)
        {
            _objectsForRemoval.Add(args.TheObject);
        }

        private static void QueueObjectForAddition(RhinoObjectEventArgs args)
        {
            _objectsForAddition.Add(args.TheObject);
        }

        private static void QueueObjectForReplacement(RhinoReplaceObjectEventArgs args)
        {
            _objectsForReplace.Add(args.NewRhinoObject);
        }

        private static void ProcessEventPipepline(object? sender, EventArgs e)
        {
            if (!WPFViewModel.AppEnabled || EventLock) return;

            // Pick the actions to execute.
            // Because Rhino sometimes sends both remove + add as well as replace objects events, we need to pick the one of these pathways only
            _objectsForAddition = _objectsForAddition.Where(id => !_objectsForReplace.Contains(id)).ToList();
            _objectsForRemoval = _objectsForRemoval.Where(id => !_objectsForReplace.Contains(id)).ToList();

            if(_objectsForReplace.Count > 0) 
                RunReplacementOperations(_objectsForReplace);
            
            if(_objectsForRemoval.Count > 0)
                 RunObjectRemovalOperations(_objectsForRemoval);
            
            if(_objectsForAddition.Count > 0)
                RunObjectAddOperations(_objectsForAddition);

            if (_objectsForUndelete.Count > 0)
                RunCoreModelUndoOperations(_objectsForUndelete);

            // Clean queue
            ResetEventQueueAndObjects();
        }

      

        public static void ResetEventQueueAndObjects()
        {
            _objectsForReplace.Clear();
            _objectsForAddition.Clear();
            _objectsForRemoval.Clear();
            _objectsForUndelete.Clear();
        }

        private static void OnAllRhinoDocObjectDeSelection(RhinoDeselectAllObjectsEventArgs args)
        {
            RhinoApp.InvokeOnUiThread(() => _popUp.Close());
        }

        private static void OnRhinoDocObjectDeSelection(RhinoObjectSelectionEventArgs args)
        {
            RhinoApp.InvokeOnUiThread(() => _popUp.Close());
        }
        private static void RunCoreModelUndoOperations(List<RhinoObject> objectsForUndelete)
        {
            try
            {
                if (EventLock) return;

                EventLock = true;
                RhinoDoc doc = RhinoDoc.ActiveDoc;

                // TODO if the last road is deleted and ctrl-z-ed 
                // an error is thrown because the app layer for roads doesnt exist
                var appObjects = objectsForUndelete
                    .Where(o => doc.Layers.FindIndex(o.Attributes.LayerIndex) != null
                    && !doc.Layers.FindIndex(o.Attributes.LayerIndex).IsDeleted
                    && doc.Layers[o.Attributes.LayerIndex].FullPath.Contains(LayerConstants.MainModellingLayer))
                    .ToList();

                var ids = appObjects.Select(o => o.Id).ToList();

                if (objectsForUndelete.Count == 0)
                {
                    EventLock = false;
                    return;
                }

                doc.Objects.Delete(ids, true);
                doc.Views.Redraw();
                EventLock = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                EventLock = false;
            }
        }

        private static void RunReplacementOperations(List<RhinoObject> replacedObjects) 
        {
            try
            {
                if (EventLock) return;

                EventLock = true;
                RhinoDoc doc = RhinoDoc.ActiveDoc;

                if (replacedObjects.Count == 0 || WPFViewModel.AppMode == ModellingTool.None)
                {
                    EventLock = false;
                    return;
                }

                if (WPFViewModel.AppMode == ModellingTool.Building)
                {
                    List<RhinoBuildingData> buildingData = RhinoBuildingData.FromExisting(replacedObjects).ToList();

                    if (buildingData.Count == 0)
                    {
                        EventLock = false;
                        return;
                    }

                    UpdateSelectedBuildingsPosition(buildingData);
                    WPFViewModel.RefreshActiveStudyAndMetrics();
                }
                else if (WPFViewModel.AppMode == ModellingTool.Transit)
                {
                    var roadData = RhinoRoadData.FromExisting(replacedObjects).ToList();
                    var coreRoadData = RhinoRoadData.ToCoreModelCreationData(roadData).ToList();

                    if (coreRoadData.Count == 0)
                    {
                        EventLock = false;
                        return;
                    }

                    ModelBuilderPipelines.AddOrUpdateRoadPipeline(
                        doc,
                        coreRoadData,
                        WPFViewModel.AppMode);

                    AppUsageLog.ModelLog.Transit++;
                    WPFViewModel.RefreshActiveStudyAndMetrics();
                }

                // This is actually for dividers
                else if (WPFViewModel.AppMode == ModellingTool.LandUse)
                {
                    var validation = new DividerInputValidator(true).ValidateInput(replacedObjects);
                    var rhinoDividerData = RhinoDividerData.Create(validation.ValidObjects);
                    var coreDividerData = RhinoDividerData.ToCoreModelData(rhinoDividerData).ToList();

                    if (coreDividerData.Count == 0)
                    {
                        EventLock = false;
                        return;
                    }

                    ModelBuilderPipelines.AddOrUpdateParcelDividerPipeline(
                        RhinoDoc.ActiveDoc,
                        coreDividerData,
                        WPFViewModel.AppMode);

                    WPFViewModel.RefreshActiveStudyAndMetrics();
                }
                else if (WPFViewModel.AppMode == ModellingTool.Zones)
                {
                    var rhinoStudyData = RhinoStudyData.FromExisting(replacedObjects);
                    var coreStudyData = RhinoStudyData.ToCoreModelData(rhinoStudyData).ToList();

                    if (coreStudyData.Count == 0)
                    {
                        EventLock = false;
                        return;
                    }

                    ModelBuilderPipelines.UpdateStudyAreas(RhinoDoc.ActiveDoc, coreStudyData, WPFViewModel.AppMode);
                    WPFViewModel.RefreshActiveStudyAndMetrics();
                }

                EventLock = false;
            }
            catch (Exception ex)
            {
                EventLock = false;
                MessageBox.Show(ex.Message);
            }
        }

        public static void UpdateSelectedBuildingsPosition(List<RhinoBuildingData> rhinoCreationData)
        {
            var buildingData = rhinoCreationData.Select(x => x.ToCoreModelData()).ToList();
            List<Guid> ids = rhinoCreationData.Select(x => x.ID).ToList();

            EventLock = true;
            RhinoDoc doc = RhinoDoc.ActiveDoc;
            ModelBuilderPipelines.DeleteBuildingPipeline(doc, buildingData, WPFViewModel.AppMode);
            ModelBuilderPipelines.AddOrUpdateBuldingPipeline(doc, buildingData, WPFViewModel.AppMode);
            EventLock = false;
        }

        private static void RunObjectAddOperations(List<RhinoObject> newObjects)
        {
            try
             {
                if (EventLock) return;

                EventLock = true;
                 RhinoDoc doc = RhinoDoc.ActiveDoc;

                if (newObjects.Count == 0)
                {
                    EventLock = false;
                    return;
                }

                // Road needs the add event because the join command
                // on curves in rhino will not trigger a replace event
                else if (WPFViewModel.AppMode == ModellingTool.Transit)
                {
                    var result = new TransitInputValidator().ValidateInput(newObjects);
                    if (result.ValidObjects.Count == 0)
                    {
                        EventLock = false;
                        return;
                    }

                    var roadData = RhinoRoadData.FromExisting(result.ValidObjects).ToList();
                    var coreRoadData = RhinoRoadData.ToCoreModelCreationData(roadData).ToList();

                    if (coreRoadData.Count == 0)
                    {
                        EventLock = false;
                        return;
                    }

                    ModelBuilderPipelines.AddOrUpdateRoadPipeline(doc, coreRoadData, WPFViewModel.AppMode);
                    WPFViewModel.RefreshActiveStudyAndMetrics();
                }
                // Landuse (ie Dividers) needs the add event because the join command
                // on curves in rhino will not trigger a replace event
                else if (WPFViewModel.AppMode == ModellingTool.LandUse)
                {
                    var result = new DividerInputValidator(true).ValidateInput(newObjects);
                    if(result.ValidObjects.Count == 0)
                    {
                        EventLock = false;
                        return;
                    }

                    var rhinoDividerData = RhinoDividerData.Create(result.ValidObjects);
                    var coreDividerData = RhinoDividerData.ToCoreModelData(rhinoDividerData).ToList();

                    if (coreDividerData.Count == 0)
                    {
                        EventLock = false;
                        return;
                    }

                    ModelBuilderPipelines.AddOrUpdateParcelDividerPipeline(
                        RhinoDoc.ActiveDoc, 
                        coreDividerData,
                        WPFViewModel.AppMode);

                    WPFViewModel.RefreshActiveStudyAndMetrics();
                }

                EventLock = false;
            }
            catch(Exception ex)
            {
                EventLock = false;
                MessageBox.Show(ex.Message);
            }
        }
        public static void RunObjectRemovalOperations(List<RhinoObject> removedObjects)
        {
            try
            {
                List<Guid> removedIDs = removedObjects.Select(obj => obj.Id).ToList();

                if (EventLock || removedObjects.Count == 0) return;
                RhinoDoc doc = RhinoDoc.ActiveDoc;

                EventLock = true;
                switch (WPFViewModel.AppMode)
                {
                    case ModellingTool.Transit:
                        var transitResult = new TransitInputValidator().ValidateInput(removedObjects);
                        var transitData = RhinoRoadData.FromExisting(transitResult.ValidObjects)
                            .Select(x => x.ToCoreModelCreationData())
                            .ToList();

                        if (transitResult.Result == ValidationResult.All_Invalid
                            || transitResult.Result == ValidationResult.EmptyInput)
                            break;

                        ModelBuilderPipelines.RemoveRoadPipeline(transitData,doc, WPFViewModel.AppMode);

                        WPFViewModel.RefreshActiveStudyAndMetrics();
                        break;

                    case ModellingTool.Building:
                        var buildingResult = new BuildingInputValidator(BuildingSelectorMode.Mass)
                            .ValidateInput(removedObjects);

                        if (buildingResult.Result == ValidationResult.All_Invalid
                            || buildingResult.Result == ValidationResult.EmptyInput)
                            break;

                        var buildingData = RhinoBuildingData
                            .FromExisting(buildingResult.ValidObjects)
                            .Select(x => x.ToCoreModelData())
                            .Where(x => x != null)
                            .Cast<BuildingData>()
                            .ToList();

                        ModelBuilderPipelines.DeleteBuildingPipeline(doc, buildingData, WPFViewModel.AppMode);
                        WPFViewModel.RefreshActiveStudyAndMetrics();
                        break;

                    case ModellingTool.Zones:
                        var result = new StudyInputValidator().ValidateInput(removedObjects);
                        if (result.ValidObjects.Count == 0)
                        {
                            EventLock = false;
                            return;
                        }
                        ModelBuilderPipelines.DeleteStudyAreaPipeline(doc, removedIDs, WPFViewModel.AppMode);
                        WPFViewModel.RefreshActiveStudyAndMetrics();
                        break;

                    case ModellingTool.LandUse:

                        var validation = new DividerInputValidator(true).ValidateInput(removedObjects);

                        if (validation.ValidObjects.Count == 0) break;

                        var divData = RhinoDividerData.FromExisting(validation.ValidObjects)
                            .Select(x => x.ToCoreModelData())
                            .ToList();

                        ModelBuilderPipelines.DeleteParcelDividers(doc, divData, WPFViewModel.AppMode);
                        WPFViewModel.RefreshActiveStudyAndMetrics();
                        break;
                }

                EventLock = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                EventLock = false;
            }
        }

        private static TestPopUp _popUp = new TestPopUp();

        private static Point3d CalculateObjectsCenter(IEnumerable<RhinoObject> objs)
        {
            var centers = objs.Select(obj => obj.Geometry.GetBoundingBox(false).Center)
                .ToList();

            return centers.AveragePoint();
        }
        private static System.Drawing.Point ToScreenPoint(Point3d modelPoint, RhinoViewport viewport)
        {
            var viewportPoint = viewport.WorldToClient(modelPoint);
            var screenPoint1 = viewport.ClientToScreen(viewportPoint);
            var point = viewport.ParentView.ClientToScreen(viewportPoint);

            var screenPoint2 = new System.Drawing.Point((int)point.X, (int)point.Y);

            return screenPoint2;
        }
        private static void OnRhinoDocObjectSelection(RhinoObjectSelectionEventArgs args)
        {
            try
            {
                _popUp.Close();

                RhinoDoc doc = RhinoDoc.ActiveDoc;
                var ids = doc.Objects
                    .GetSelectedObjects(false, false)
                    .Select(obj => obj.Id).ToList();
                
                List<Guid> coreModelIds = FindCoreModelObjs(ids, WPFViewModel.AppMode);

                if (!WPFViewModel.AppEnabled || ids.Count == 0) return;

                // Find screen point
                var selectedObjects = doc.Objects.Where(obj => coreModelIds.Contains(obj.Id));
                var center = CalculateObjectsCenter(selectedObjects);
                var pointOnScreen = ToScreenPoint(center, doc.Views.ActiveView.ActiveViewport);

                RhinoApp.InvokeOnUiThread(() =>
                {
                    // Position window
                    _popUp = new();

                    switch (WPFViewModel.AppMode)
                    {
                        case ModellingTool.Transit:
                            var roads = CoreModel.Roads.Where(rd => coreModelIds.Contains(rd.CenterLineId)).ToList();
                            if (roads.Count == 0) break;
                            PopUpViewModel.ConfigureRoadsInPopUp(_popUp, roads);
                            _popUp.Show();
                            break;

                        case ModellingTool.LandUse:
                            var parcels = CoreModel.Parcels.Where(p => coreModelIds.Contains(p.Id)).ToList();
                            if (parcels.Count == 0) break;
                            PopUpViewModel.ConfigureLandUseInPopUp(_popUp, parcels);
                            _popUp.Show();
                            break;

                        case ModellingTool.Building:
                            var buildings = CoreModel.Buildings
                                .Where(bldg => coreModelIds.Contains(bldg.EnvelopeId))
                                .ToList();
                            if (buildings.Count == 0) break;
                            PopUpViewModel.ConfigureBuildingsInPopUp(_popUp, buildings);
                            _popUp.Show();
                            break;
                    }
                    var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(_popUp);
                    _popUp.Left = pointOnScreen.X / dpi.DpiScaleX;
                    _popUp.Top = pointOnScreen.Y / dpi.DpiScaleY;
                    FocusRhinoWindow();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        // Import the SetForegroundWindow function from user32.dll
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        private static void FocusRhinoWindow()
        {
            // Get Rhino's main window handle
            IntPtr rhinoWindowHandle = RhinoApp.MainWindowHandle();

            // Set Rhino's window as the foreground window
            SetForegroundWindow(rhinoWindowHandle);
        }

        private static List<Guid> FindCoreModelObjs(List<Guid> selectedIds, ModellingTool mode)
        {
            switch(mode)
            {
                case ModellingTool.Transit:
                    return CoreModel.Roads
                        .Where(rd => selectedIds.Contains(rd.CenterLineId))
                        .Select(rd => rd.CenterLineId)
                        .ToList();

                case ModellingTool.LandUse: return CoreModel.Parcels
                        .Where(p => selectedIds.Contains(p.Id))
                        .Select(p => p.Id)
                        .ToList();

                case ModellingTool.Building: return CoreModel.Buildings
                        .Where(b => selectedIds.Contains(b.EnvelopeId))
                        .Select(b => b.EnvelopeId)
                        .ToList();

                case ModellingTool.Zones:
                    return CoreModel.Studies
                        .Where(st => selectedIds.Contains(st.Id))
                        .Select(b => b.Id)
                        .ToList();

                default: return new();
            }
        }

        internal static void UpdateSelectedBuildings(
            List<RhinoObject> rhinoObjs,
            int floorCount,
            double floorToFloorHt)
        {
            RhinoDoc doc = RhinoDoc.ActiveDoc;

            var buildingEnvelopeGuids = new List<Guid>();
            List<(BH.oM.Geometry.Mesh? Envelope, Guid ID, List<PlanarSurface> FloorPlates)> buildingData = new();


            foreach (var obj in rhinoObjs)
            {
                bool buildingExists = CoreModel.TryGetBuilding(obj.Id, obj.Id, out Building building);
                Guid footPrintID = Guid.Empty;
                Guid envelopeID = Guid.Empty;

                // Build Rhino Mesh 
                Rhino.Geometry.Mesh rhinoMesh = null;
                envelopeID = obj.Id;
                if (obj.ObjectType == ObjectType.Mesh)
                {
                    rhinoMesh = obj.Geometry as Rhino.Geometry.Mesh;
                }
                else if (obj.ObjectType == ObjectType.Brep)
                {
                    rhinoMesh = GeometryHelpers.ConvertToMesh(obj.Geometry as Rhino.Geometry.Brep);
                }
                else if (obj.ObjectType == ObjectType.Extrusion)
                {
                    rhinoMesh = GeometryHelpers.ConvertToMesh(obj.Geometry as Rhino.Geometry.Extrusion);
                }
                else if (obj.ObjectType == ObjectType.Curve)
                {
                    footPrintID = obj.Id;
                    envelopeID = Guid.Empty;

                    var extrusion = Rhino.Geometry.Extrusion.Create(
                        obj.Geometry as Curve,
                        Rhino.Geometry.Plane.WorldXY,
                        floorCount * floorToFloorHt,
                        true);

                    rhinoMesh = GeometryHelpers.ConvertToMesh(extrusion);
                }
                else
                {
                    continue;
                }

                // Rebuild the mesh if this is an editing operation, to match the floor params

                if (buildingExists)
                {
                    buildingEnvelopeGuids.Add(envelopeID);
                    rhinoMesh = RescaleRhinoMeshForNewParameters(rhinoMesh, floorCount, floorToFloorHt);
                }

                var floorPlates = GeometryHelpers
                    .GetContours(rhinoMesh, floorToFloorHt)
                    .Select(c => c.ToBHoM())
                    .Select(c => new PlanarSurface(c, new List<ICurve>()))
                    .ToList();

                var bhomMesh = rhinoMesh.ToBHoM();

                buildingData.Add((bhomMesh,
                    envelopeID,
                    floorPlates));

            }

            EventLock = true;
            rhinoObjs.ForEach(o => doc.Objects.Delete(o, true, true));

            //ModelBuilderPipelines.DeleteBuildingPipeline(doc, buildingData);

            //ModelBuilderPipelines.AddOrUpdateBuldingPipeline(doc, buildingData)
            EventLock = false;

        }

        private static Rhino.Geometry.Mesh? RescaleRhinoMeshForNewParameters(Rhino.Geometry.Mesh? rhinoMesh, int floorCount, double floorToFloorHt)
        {
            floorCount = floorCount <= 1 ? 1 : floorCount - 1;

            var bbox = rhinoMesh.GetBoundingBox(false);
            var zHt = bbox.Max.Z - bbox.Min.Z;
            var newZHt = floorToFloorHt * floorCount;
            var basePlane = Plane.CreateFromNormal(bbox.Min, Vector3d.ZAxis);
            var scaler = Transform.Scale(basePlane, 1, 1, newZHt / zHt);
            rhinoMesh.Transform(scaler);
            return rhinoMesh;
        }

        private static PlanarSurface GetFootPrint(RhinoObject crv)
        {
            var curve = crv.Geometry as Curve;
            BH.oM.Geometry.Polyline bhom = null;

            if (curve != null && curve.TryGetPolyline(out Rhino.Geometry.Polyline polyline)
                && polyline.IsClosed)
                bhom = polyline.ToBHoMPolyline();
            else
                return null;

            bhom = bhom.IsClockwise(Vector.ZAxis) ? bhom : bhom.Flip();

            bool clockWise = bhom.IsClockwise(Vector.ZAxis);

            PlanarSurface footPrint = Create.PlanarSurface(bhom);
            return footPrint;
        }

        
    }
}
    