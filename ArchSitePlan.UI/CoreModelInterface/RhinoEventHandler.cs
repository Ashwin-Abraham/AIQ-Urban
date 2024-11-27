using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.CoreModelInterface;
using ArchSitePlan.UI.Extensions;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.RhinoBHoMInterop;
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
        public static bool EventLock = false;

        private static Guid _lastReplacedId = Guid.Empty;
        private static List<Guid> _selectedParcels = new List<Guid>();
        public static MainViewModel ViewModel;

        static RhinoEventHandler()
        {
            EventHandler<RhinoReplaceObjectEventArgs> replaceRhinoObjectEvent = (sender, args) => OnRhinoDocObjectReplace(args);
            RhinoDoc.ReplaceRhinoObject += replaceRhinoObjectEvent;

            EventHandler<RhinoObjectEventArgs> addRhinoObjectEvent = (sender, args) => OnRhinoDocObjectAddition(args);
            RhinoDoc.AddRhinoObject += addRhinoObjectEvent;

            EventHandler<RhinoObjectEventArgs> removedRhinoObjectEvent = (sender, args) => OnRhinoDocObjectRemove(args);
            RhinoDoc.DeleteRhinoObject += removedRhinoObjectEvent;

            EventHandler<RhinoObjectSelectionEventArgs> selectedRhinoObjectsEvent = (sender, args) 
                => OnRhinoDocObjectSelection(args);
            RhinoDoc.SelectObjects += selectedRhinoObjectsEvent;

            EventHandler<RhinoObjectSelectionEventArgs> deselectedRhinoObjectsEvent = (sender, args) => OnRhinoDocObjectDeSelection(args);
            RhinoDoc.DeselectObjects += deselectedRhinoObjectsEvent;

            EventHandler<RhinoDeselectAllObjectsEventArgs> deselectedAllRhinoObjectsEvent = (sender, args) => OnAllRhinoDocObjectDeSelection(args);
            RhinoDoc.DeselectAllObjects += deselectedAllRhinoObjectsEvent;
        }

        private static void OnAllRhinoDocObjectDeSelection(RhinoDeselectAllObjectsEventArgs args)
        {
            _popUp.Close();
        }

        private static void OnRhinoDocObjectDeSelection(RhinoObjectSelectionEventArgs args)
        {
            _popUp.Close();
        }

        private static void OnRhinoDocObjectReplace(RhinoReplaceObjectEventArgs args) 
        {
            int d = 34;
            int a = d + 34;
        }
        private static void OnRhinoDocObjectAddition(RhinoObjectEventArgs args)
        {
            try
             {
                if (EventLock) return;

                EventLock = true;
                 RhinoDoc doc = RhinoDoc.ActiveDoc;
                RhinoObject obj = args.TheObject;

                if (obj == null)
                {
                    EventLock = false;
                    return;
                }

                if (ViewModel.AppMode == AppMode.Building)
                {
                    if (!(obj.ObjectType == ObjectType.Curve
                        || obj.ObjectType == ObjectType.Brep
                        || obj.ObjectType == ObjectType.Extrusion
                        || obj.ObjectType == ObjectType.Mesh)) return;

                    if (!CoreModel.TryGetBuilding(obj.Id, obj.Id, out Building building))
                    {
                        EventLock = false;
                        return;
                    }

                    UpdateSelectedBuildings(obj.ObjectToList(),
                            building.FloorCount,
                            building.FloorToFloorHeight);

                    ViewModel.RefreshActiveStudyAndMetrics();
                }
                else if (ViewModel.AppMode == AppMode.Road)
                {
                    var roadCLLayers = doc.Layers
                        .Where(lay => !lay.IsDeleted && lay.FullPath.Contains(LayerConstants.RoadCLLayer))
                        .Select(lay => lay.Index)
                        .ToList();

                    var validationResult = InputValidation.ValidateRoadInput(obj.ObjectToList());
                   
                    if (validationResult.ValidCenterlines.Count == 0)
                    {
                        EventLock = false;
                        return;
                    }

                    var bhomCrv = (validationResult.ValidCenterlines[0].Geometry as Curve)!.ToBHoM2DegreeCrv();
                    ModelBuilderPipelines.AddOrUpdateRoadPipeline(
                        doc,
                        (bhomCrv, obj.Id).ObjectToList(), 
                        ViewModel.RoadWidth);
                    
                    ViewModel.RefreshActiveStudyAndMetrics();
                }
                else if (ViewModel.AppMode == AppMode.LandUse)
                {
                    if (obj.ObjectType != ObjectType.Curve)
                    {
                        EventLock = false;
                        return;
                    }

                    var bhomCrv = (obj.Geometry as Curve)!.ToBHoM2DegreeCrv();
                    
                    if (!CoreModel.TryGetParcelDivider(obj.Id, out _)
                        || bhomCrv is null)
                    {
                        EventLock = false;
                        return;
                    }

                    var plineObjs = (bhomCrv, obj.Id).ObjectToList();
                    ModelBuilderPipelines.AddOrUpdateParcelDividerPipeline(RhinoDoc.ActiveDoc, plineObjs);
                    ViewModel.RefreshActiveStudyAndMetrics();
                }
                else if (ViewModel.AppMode == AppMode.StudyArea)
                {
                    var curve = obj.Geometry as Curve;
                    if (!CoreModel.TryGetStudyArea(obj.Id, out _)
                        || curve is null)
                    {
                        EventLock = false;
                        return;
                    }

                    var bhomCurve = curve.ToBHoM2DegreeCrv();
                    if (bhomCurve is null)
                    {
                        EventLock = false;
                        return;
                    }

                    var crvObjs = (bhomCurve, obj.Id).ObjectToList();
                    ModelBuilderPipelines.UpdateStudyAreas(RhinoDoc.ActiveDoc, crvObjs);
                    ViewModel.RefreshActiveStudyAndMetrics();
                }

                EventLock = false;
            }
            catch(Exception ex)
            {
                EventLock = false;
                MessageBox.Show(ex.Message);
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
                
                List<Guid> coreModelIds = FindCoreModelObjs(ids, ViewModel.AppMode);

                if (!ViewModel.AppEnabled || ids.Count == 0) return;

                // Find screen point
                var selectedObjects = doc.Objects.Where(obj => coreModelIds.Contains(obj.Id));
                var center = CalculateObjectsCenter(selectedObjects);
                var pointOnScreen = ToScreenPoint(center, doc.Views.ActiveView.ActiveViewport);

                // Position window
                _popUp = new();

                switch (ViewModel.AppMode)
                {
                    case AppMode.Road:
                        var roads = CoreModel.Roads.Where(rd => coreModelIds.Contains(rd.CenterLineId)).ToList();
                        if (roads.Count == 0) break;
                        PopUpViewModel.ConfigureRoadsInPopUp(_popUp, roads);
                        _popUp.Show();
                        break;

                    case AppMode.LandUse:
                        var parcels = CoreModel.Parcels.Where(p => coreModelIds.Contains(p.Id)).ToList();
                        if (parcels.Count == 0) break;
                        PopUpViewModel.ConfigureLandUseInPopUp(_popUp, parcels);
                        _popUp.Show();
                        break;

                    case AppMode.Building:
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

        private static List<Guid> FindCoreModelObjs(List<Guid> selectedIds, AppMode mode)
        {
            switch(mode)
            {
                case AppMode.Road:
                    return CoreModel.Roads
                        .Where(rd => selectedIds.Contains(rd.CenterLineId))
                        .Select(rd => rd.CenterLineId)
                        .ToList();

                case AppMode.LandUse: return CoreModel.Parcels
                        .Where(p => selectedIds.Contains(p.Id))
                        .Select(p => p.Id)
                        .ToList();

                case AppMode.Building: return CoreModel.Buildings
                        .Where(b => selectedIds.Contains(b.EnvelopeId))
                        .Select(b => b.EnvelopeId)
                        .ToList();

                case AppMode.StudyArea:
                    return CoreModel.Studies
                        .Where(st => selectedIds.Contains(st.Id))
                        .Select(b => b.Id)
                        .ToList();

                default: return new();
            }
        }

        public static void OnRhinoDocObjectRemove(RhinoObjectEventArgs args)
        {
            try
            {
                if (EventLock || !args.TheObject.IsDeleted) return;
                RhinoDoc doc = RhinoDoc.ActiveDoc;

                EventLock = true;
                switch (ViewModel.AppMode)
                {
                    case AppMode.Road:
                        ModelBuilderPipelines.RemoveRoadPipeline(
                            args.TheObject.Id.ObjectToList(),
                            doc);

                        ViewModel.RefreshActiveStudyAndMetrics();
                        break;

                    case AppMode.Building:
                        ModelBuilderPipelines.DeleteBuildingPipeline(doc, args.ObjectId.ObjectToList());
                        ViewModel.RefreshActiveStudyAndMetrics();
                        break;

                    case AppMode.StudyArea:
                        ModelBuilderPipelines.DeleteStudyAreaPipeline(doc, args.ObjectId.ObjectToList());
                        ViewModel.RefreshActiveStudyAndMetrics();
                        break;

                    case AppMode.LandUse:
                        ModelBuilderPipelines.DeleteParcelDividers(doc, args.ObjectId.ObjectToList());
                        ViewModel.RefreshActiveStudyAndMetrics();
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

        internal static void UpdateSelectedBuildings(List<RhinoObject> rhinoObjs, 
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
                else if(obj.ObjectType == ObjectType.Extrusion)
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

                var bhomMesh  = rhinoMesh.ToBHoM();

                buildingData.Add((bhomMesh,
                    envelopeID,
                    floorPlates));

            }

            EventLock = true;
            rhinoObjs.ForEach(o => doc.Objects.Delete(o, true, true));
            
            ModelBuilderPipelines.DeleteBuildingPipeline(doc, buildingEnvelopeGuids);

            ModelBuilderPipelines.AddOrUpdateBuldingPipeline(doc,
                  buildingData,
                  floorToFloorHt,
                  floorCount);
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
    