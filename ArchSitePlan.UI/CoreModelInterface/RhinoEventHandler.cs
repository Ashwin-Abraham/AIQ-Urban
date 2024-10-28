using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.UI.CoreModelInterface;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.RhinoBHoMInterop;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Windows;
using static ArchSitePlan.UI.Constants.Enums;
using Plane = Rhino.Geometry.Plane;
using Vector = BH.oM.Geometry.Vector;

namespace ArchSitePlan.UI.Actions
{

    public static class RhinoEventHandler
    {
        public static bool RemovalEventLock = false;

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

            EventHandler<RhinoObjectSelectionEventArgs> selectedRhinoObjectsEvent = (sender, args) => OnRhinoDocObjectSelection(args);
            RhinoDoc.SelectObjects += selectedRhinoObjectsEvent;
        }
        private static void OnRhinoDocObjectReplace(RhinoReplaceObjectEventArgs args)
        {
            try
            {
                _lastReplacedId = args.ObjectId;
                RhinoDoc doc = RhinoDoc.ActiveDoc;
                RhinoObject obj = args.NewRhinoObject;

                if (obj == null) return;

                if (ViewModel.AppMode == AppMode.Building)
                {

                    if (!(obj.ObjectType == ObjectType.Curve
                        || obj.ObjectType == ObjectType.Brep
                        || obj.ObjectType == ObjectType.Extrusion
                        || obj.ObjectType == ObjectType.Mesh)) return;

                    if (!CoreModel.TryGetBuilding(obj.Id, obj.Id, out Building building)) return;

                    UpdateSelectedBuildings(obj.ObjectToList(),
                            building.FloorCount,
                            building.FloorToFloorHeight);

                    //var buildingData 
                    //    = (building.Envelope, 
                    //    building.EnvelopeId, 
                    //    building.FloorPlates.Select(f => f.Profile).ToList());

                    //_removalEventLock = true;
                    //ModelBuilderPipelines.DeleteBuildingPipeline(doc, building.EnvelopeId.ObjectToList());
                    //ModelBuilderPipelines.AddOrUpdateBuldingPipeline(doc,
                    //    buildingData.ObjectToList(),
                    //    ViewModel.ActiveBuildingFloorHeight,
                    //    ViewModel.ActiveBuildingFloorCount);
                    //_removalEventLock = false;

                    ViewModel.RefreshActiveStudyAndMetrics();
                }
                else if (ViewModel.AppMode == AppMode.Road)
                {
                    if (!CoreModel.TryFindRoad(args.ObjectId, out _)
                        || !RhinoToCoreModel.TryExtractBHoMPolyline(obj, out BH.oM.Geometry.Polyline? pline))
                        return ;

                    ModelBuilderPipelines.AddOrUpdateRoadPipeline(
                        doc,
                        (pline, obj.Id).ObjectToList(), 
                        ViewModel.RoadWidth);
                    
                    ViewModel.RefreshActiveStudyAndMetrics();
                }
                else if (ViewModel.AppMode == AppMode.LandUse)
                {
                    var pline = DocObjectHelpers.TryExtractPolyline(obj);
                    if (!CoreModel.TryGetParcelDivider(obj.Id, out _)
                        || pline is null) return;

                    var plineObjs = (pline.ToBHoM(), obj.Id).ObjectToList();
                    ModelBuilderPipelines.AddOrUpdateParcelDividerPipeline(RhinoDoc.ActiveDoc, plineObjs);
                    ViewModel.RefreshActiveStudyAndMetrics();
                }
                else if (ViewModel.AppMode == AppMode.StudyArea)
                {
                    var pline = DocObjectHelpers.TryExtractPolyline(obj);
                    if (!CoreModel.TryGetStudyArea(obj.Id, out _)
                        || pline is null) return;

                    var plineObjs = (pline.ToBHoM(), obj.Id).ObjectToList();
                    ModelBuilderPipelines.UpdateStudyAreas(RhinoDoc.ActiveDoc, plineObjs);
                    ViewModel.RefreshActiveStudyAndMetrics();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

      
        public static void ActivateRoadSelectionFilter(bool activate)
        {
            Rhino.ApplicationSettings.SelectionFilterSettings.OneShotGeometryFilter = ObjectType.Curve;
        }
        private static void OnRhinoDocObjectSelection(RhinoObjectSelectionEventArgs args)
        {
            try
            {
                if (ViewModel.AppMode == AppMode.Road)
                {
                    var crvObjs = args.RhinoObjects
                        .Where(obj => obj.ObjectType == ObjectType.Curve)
                        .ToList();

                    // Set active width as first roads width
                    foreach (var crvObj in crvObjs)
                    {
                        bool roadExists = RhinoToCoreModel.TryFindRoad(crvObj.Id, out Road road);
                        if(roadExists)
                        {
                            ViewModel.RoadWidth = road.Width;
                            break;
                        }
                    }
                }

                else if(ViewModel.AppMode == AppMode.LandUse)
                {
                    var srfObjs = args.RhinoObjects
                        .Where(obj => obj.ObjectType == ObjectType.Brep)
                        .ToList();

                    if(srfObjs.Any())
                        UpdateSelectedParcelsMetrics(srfObjs);
                }

                else if(ViewModel.AppMode == AppMode.Building)
                {
                    // Update view model with current building data
                    var crvRhinoObjs = args.RhinoObjects
                                            .Where(obj => obj.ObjectType == ObjectType.Curve)
                                            .Where(obj => (obj.Geometry as Curve).IsClosed)
                                            .ToList();

                    var envelopeRhinoObjs = args.RhinoObjects
                        .Where(obj => obj.ObjectType == ObjectType.Brep 
                            || obj.ObjectType == ObjectType.Extrusion
                            || obj.ObjectType == ObjectType.Mesh)
                        .ToList();

                    var rhinoObjs = ViewModel.BuildingSelectorMode == BuildingSelectorMode.Footprint? crvRhinoObjs: envelopeRhinoObjs;
                    var firstObj = rhinoObjs.FirstOrDefault(b => b.Id != Guid.Empty);

                    Building building = null;
                    if (firstObj == null || !CoreModel.TryGetBuilding(firstObj.Id, firstObj.Id, out building))
                        return;

                    ViewModel.ActiveBuildingFloorCount = building.FloorCount;
                    ViewModel.ActiveBuildingFloorHeight = building.FloorToFloorHeight;
                }

                else if(ViewModel.AppMode == AppMode.StudyArea)
                {
                    var selectedIds = args.RhinoObjects.Select(o => o.Id).ToList();
                    var firstValidStudy = CoreModel.Studies.FirstOrDefault(s => selectedIds.Contains(s.Id));

                    if (firstValidStudy == null) return;

                    ViewModel.ActiveStudy = firstValidStudy;
                    ViewModel.ActiveStudyAreaName = firstValidStudy.Name;
                }
                else if(ViewModel.AppMode != AppMode.None)
                {
                    //var activeStudyMetric = CoreModel.StudiesAndMetrics.FirstOrDefault(s => s.Study.Name == ViewModel.ActiveStudyAreaName);
                    //ViewModel.UpdateDisplayedStudyAreaData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }

        private static void UpdateSelectedParcelsMetrics(List<RhinoObject> selectedBreps)
        {
            if(selectedBreps == null || selectedBreps.Count == 0) return;

            RhinoToCoreModel.RecalculateBuildingHostParcelIDs();

            // Check that they are parcels
            var guids = selectedBreps.Select(obj => obj.Id).ToList();
            var parcels = RhinoToCoreModel.GetParcel(guids).ToList();
            var containedBuildings = RhinoToCoreModel.FindBuildingsInParcels(parcels);

            // Aggregate metrics
            double parcelsArea = parcels.Sum(p => p.Area());
            double parcelsBuiltArea = containedBuildings.Sum(b => b.BUA());
            double parcelsCoverage = containedBuildings.Sum(b => b.FootprintArea()) / parcelsArea * 100;
            double parcelsFAR = parcelsBuiltArea / parcelsArea;

            string use;
            if (parcels.GroupBy(p => p.LandUse).Count() > 1)
                use = "Mixed";
            else
                use = parcels.First().LandUse.ToString();

            // Change viewmodel variables
            ViewModel.ActiveParcelMetrics = new VMActiveParcelsDetails(
                Use: use,
                TotalArea: Math.Round(parcelsArea).ToString(),
                BUA: Math.Round(parcelsBuiltArea).ToString(),
                CoveragePctg: Math.Round(parcelsCoverage, 1).ToString(),
                FAR: Math.Round(parcelsFAR, 2).ToString());
        }

        public static void OnRhinoDocObjectAddition(RhinoObjectEventArgs args)
        {
        }

        public static void OnRhinoDocObjectRemove(RhinoObjectEventArgs args)
        {
            try
            {
                if (RemovalEventLock) return;

                RhinoDoc doc = RhinoDoc.ActiveDoc;

                if (!args.TheObject.IsDeleted
                    || _lastReplacedId == args.ObjectId)
                {
                    _lastReplacedId = Guid.Empty;
                    return;
                }
                switch (ViewModel.AppMode)
                {
                    case AppMode.Road:
                        
                        if (!CoreModelEditStepLibrary
                            .TryRemoveRoad(args.TheObject.Id.ObjectToList()))
                            break;
                        
                        _lastReplacedId = args.TheObject.Id;
                        ModelBuilderPipelines.RefreshStudyAreaMetricsPipeline();
                        ViewModel.RefreshActiveStudyAndMetrics();
                        // Output again because the parcels also may have been edited
                        ModelBuilderPipelines.ReplaceRhinoModelPipeline(doc);
                        break;

                    case AppMode.Building:
                        ModelBuilderPipelines.DeleteBuildingPipeline(doc, args.ObjectId.ObjectToList());
;                        ViewModel.RefreshActiveStudyAndMetrics();
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
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
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

            RemovalEventLock = true;
            rhinoObjs.ForEach(o => doc.Objects.Delete(o, true, true));
            
            ModelBuilderPipelines.DeleteBuildingPipeline(doc, buildingEnvelopeGuids);

            ModelBuilderPipelines.AddOrUpdateBuldingPipeline(doc,
                  buildingData,
                  floorToFloorHt,
                  floorCount);
            RemovalEventLock = false;

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
                bhom = polyline.ToBHoM();
            else
                return null;

            bhom = bhom.IsClockwise(Vector.ZAxis) ? bhom : bhom.Flip();

            bool clockWise = bhom.IsClockwise(Vector.ZAxis);

            PlanarSurface footPrint = Create.PlanarSurface(bhom);
            return footPrint;
        }
    }
}
