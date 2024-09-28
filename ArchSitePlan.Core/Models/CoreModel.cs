using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Metrics;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Core.Models
{
    public static class CoreModel
    {
        private static List<Road> _roads  = new List<Road>();
        public static IReadOnlyList<Road> Roads => _roads.AsReadOnly();
        public static List<PlanarSurface> ROWs = new List<PlanarSurface>();
        public static List<Parcel> Parcels = new List<Parcel>();
        public static List<Building> Buildings = new List<Building>();
        public static List<StudyArea> Studies = new ();
        static CoreModel() { }

        public static void AddStudyArea(StudyArea studyArea) => Studies.Add(studyArea);
        public static void RemoveStudyArea(StudyArea studyArea) => Studies.Remove(studyArea);

        public static void RecalculateBuildingHostParcelIDs()
        {
            foreach(var building in Buildings)
            {
                var parcel = Parcels.FirstOrDefault(p => p.Boundary.ExternalBoundary.IIsContaining(building.Origin.ObjectToList()));
                building.HostParcelId = parcel?.OriginalObjectID?? Guid.Empty;
            }
        }
        public static void RefreshStudyAreaMetrics(bool RecalculatModelSplit = true)
        {
            if (RecalculatModelSplit)
            {
                SplitCoreModelByStudyAreas();
            }

            for (int i = 0; i < Studies.Count; i++) 
            {
                if (Studies[i].Boundary == null)
                {
                    Studies[i] = new StudyArea(Studies[i], Parcels, Buildings, Roads.ToList());
                }
                else
                {
                    var parcelsInArea = Parcels
                   .Where(p => Studies[i].Boundary.IsContaining(p.Boundary.Centroid().ObjectToList()))
                   .ToList();

                    var buildingsInArea = Buildings
                        .Where(bldg =>
                        {
                            var boundaryPt = Studies[i].Boundary.StartPoint();
                            var bldgPt = bldg.Origin.Project(Create.Plane(boundaryPt, Vector.ZAxis));
                            return Studies[i].Boundary.IsContaining(bldgPt.ObjectToList());
                        })
                        .ToList();

                    Studies[i] = new StudyArea(Studies[i], parcelsInArea, buildingsInArea, Roads.ToList());
                }
            }
        }
        private static void SplitCoreModelByStudyAreas()
        {
            SplitParcelsByStudyAreas();
            //SplitRoadsByStudyAreas();
            //SplitRoadRoWsByStudyAreas();
        }

        private static void SplitRoadRoWsByStudyAreas()
        {
            //var studyBoundaries = StudyAreas.Select(sa => sa.Boundary).ToList();

            //foreach (var boundary in studyBoundaries)
            //{
            //    // For each road recalculate the split roads
            //    var splitRoWs = new List<PlanarSurface>();

            //    foreach (var row in ROWs)
            //    {
            //        Compute.BooleanDifference()
            //        var insideStudy = Compute.BooleanIntersection(splitRoWs.Select(r => r.ExternalBoundary), boundary);

            //        if (insideStudy.Any())
            //        {
            //            var outsideStudy = Compute.BooleanDifference(parcel.Boundary.ExternalBoundary, boundary.ObjectToList());

            //            List<PolyCurve> splitBoundaries = insideStudy.Concat(outsideStudy).ToList();
            //            var parcelFragments = insideStudy
            //                .Concat(outsideStudy)
            //                .Select(b => new Parcel(parcel, new PlanarSurface((ICurve)b, new List<ICurve>())));

            //            splitParcels.AddRange(parcelFragments);
            //        }
            //        else
            //        {
            //            splitParcels.Add(parcel);
            //        }
            //        _roads = splitRoads;
            //}
        }

        private static void SplitRoadsByStudyAreas()
        {
            var studyBoundaries = Studies.Select(sa => sa.Boundary).ToList();

            foreach (var boundary in studyBoundaries)
            {
                // For each road recalculate the split roads
                var splitRoads = new List<Road>();

                foreach (var road in Roads)
                {
                    var intersectionPts = road.Centerline.ICurveIntersections(boundary);

                    if (intersectionPts.Any())
                    {
                        var centerLineFragments = road.Centerline.SplitAtPoints(intersectionPts);

                        var roadFragments = centerLineFragments
                            .Select(cl => new Road((PolyCurve)cl, road));

                        splitRoads.AddRange(roadFragments);
                    }
                    else
                    {
                        splitRoads.Add(road);
                    }
                }
                _roads = splitRoads;
            }
        }
        private static void SplitParcelsByStudyAreas()
        {
            var splitters = Studies.Select(sa => sa.Boundary)
                .Where(crv => crv != null)
                .SelectMany(pline => pline.Segments())
                .ToList();

            var studyBoundaries = Studies.Select(sa => sa.Boundary)
                .Where(crv=> crv!=null)
                .ToList();


            foreach(var boundary in studyBoundaries)
            {
                // For each study boundary recalculate the split parcels
                var splitParcels = new List<Parcel>();

                foreach (var parcel in Parcels)
                {
                    var insideStudy = Compute.BooleanIntersection(parcel.Boundary.ExternalBoundary, boundary);

                    if (insideStudy.Any())
                    {
                        var outsideStudy = Compute.BooleanDifference(parcel.Boundary.ExternalBoundary, boundary.ObjectToList());

                        List<PolyCurve> splitBoundaries = insideStudy.Concat(outsideStudy).ToList();
                        var parcelFragments = insideStudy
                            .Concat(outsideStudy)
                            .Select(b => new Parcel(parcel, new PlanarSurface((ICurve)b, new List<ICurve>())));

                        splitParcels.AddRange(parcelFragments);
                    }
                    else
                    {
                        splitParcels.Add(parcel);
                    }
                }

                Parcels = splitParcels;
            }
        }

        public static bool TryUpdateSomeRoads(List<(Polyline,Guid)> newGuidAndCL)
        {
            var guids = newGuidAndCL.Select(x => x.Item2).ToList();

            var roadsToUpdate = Roads
                .Where(rd => guids.Contains(rd.CenterLineId))
                .ToList();

            foreach (var road in roadsToUpdate )
            {
                var newCL = newGuidAndCL
                    .Find(x => x.Item2 == road.CenterLineId)
                    .Item1;

                AddOrUpdateRoad(newCL, road.CenterLineId, road.Width );
                _roads.Remove(road);
            }

            return roadsToUpdate.Any();
        }

        public static bool TryRemoveRoad(Guid roadCLId)
        {
            int count = _roads.RemoveAll(r => r.CenterLineId == roadCLId);
            UpdateParcelsAndRoWs();
            return count > 0;
        }

        private static void UpdateParcelsAndRoWs()
        {
            var externalBoundaries = _roads.Select(r => r.RoW.ExternalBoundary);
            var union = Compute.BooleanUnion(externalBoundaries)
                //.Select(pCrv => Create.PlanarSurface(pCrv))
                .ToList();

            var roadPoints = Roads
                .Select(r => r.Centerline.PointAtParameter(0.5))
                .ToList();

            var roads = union
                .Where(pCrv => pCrv.IsContainingAny(roadPoints))
                .ToList();

            /// TODO: More detailed road ROWs
            // var roadNetwork = new RoadNetwork(Roads);

            var parcels = union.Where(pCrv => !roads.Contains(pCrv))
                //.Select(p => p.ExternalBoundary.Fillet())
                .Cast<ICurve>()
                .ToList();

            var roadSrfs = roads.Select(r => Create.PlanarSurface(r, parcels)).ToList();

            List<Parcel> newParcels = new();
            foreach(var pCrv in union)
            {
                if (roads.Contains(pCrv)) continue;

                Landuse nearestUse = Parcels.Any()?
                    Parcels.MinBy(p => p.Boundary.Centroid().Distance(pCrv.Centroid())).LandUse
                    :Landuse.Undefined;

                newParcels.Add(new Parcel(Create.PlanarSurface(pCrv), nearestUse));
            }

            Parcels = newParcels;
            ROWs = roadSrfs;
        }

        public static void AddOrUpdateRoad(Polyline centerLine, Guid id, double width)
        {
            TryRemoveRoad(id);
            var pCurve = Create.PolyCurve(centerLine.ObjectToList());

            if (Road.TryCreateRoad(pCurve, id, width, out Road road))
            {
                //TryRemove(road.CenterLineId);
                _roads.Add(road);
            }
        }

        public static void AddOrUpdateRoads(List<(Polyline, Guid, double)> centerLines)
        {
            foreach(var cl in centerLines)
            {
                AddOrUpdateRoad(cl.Item1, cl.Item2, cl.Item3);
            }

            UpdateParcelsAndRoWs();
            RefreshStudyAreaMetrics();
        }

        public static bool TryRemove(Guid roadId)
        {
            int removalIndex = _roads.FindIndex(r => r.CenterLineId.Equals(roadId));

            if (removalIndex != -1)
            {
                _roads.RemoveAt(removalIndex);
                return true;
            }

            return false;
        }

        public static void UpdateParcelUse(List<Guid> guids, Landuse use)
        {
            Parcels.FindAll(p => guids.Contains(p.OriginalObjectID))
            .ForEach(p => p.LandUse = use);

            RefreshStudyAreaMetrics(false);
        }

        public static bool TryAddBuilding(Mesh envelope,
            List<PlanarSurface> floorPlates,
            Guid envelopeID,
            Guid footPrintId,
            double floorToFloorHt)
        {

            if (!TryGetBuilding(footPrintId, envelopeID, out Building building))
            {
                var newBldg = new Building(
                    envelope,
                    floorPlates,
                    floorToFloorHt,
                    envelopeID,
                    footPrintId);
                
                Buildings.Add(newBldg);

                return true;
            }
            return false;
        }

        public static bool TryGetStudyArea(Guid id, out StudyArea study)
        {
            study = Studies.Find(s => s.Id == id);
            return study != null;
        }

        public static bool TryAddOrUpdateStudyArea(Polyline boundary, Guid id, string name)
        {
            if (Studies.Any(s => s.Id == id))
            {
                int index = Studies.FindIndex(s => s.Id == id);
                Studies[index] = new StudyArea(boundary, id, name, Parcels, Buildings, Roads.ToList());
            }
            else
                Studies.Add(new StudyArea(boundary, id, name, Parcels, Buildings, Roads.ToList()));
            return true;
        }

        public static bool TryFindRoad(Guid id, out Road road)
        {
            road = Roads.FirstOrDefault(r => r.CenterLineId == id);
            return road != null;
        }

        public static bool TryRemoveBuilding(Guid id)
        {
            int count = Buildings.RemoveAll(b => b.FootprintId == id || b.EnvelopeId == id);
            
            return count > 0;
        }

        public static bool TryGetBuilding(Guid footPrintId, Guid massId, out Building building)
        {
            building = Buildings.FirstOrDefault(b =>
            (footPrintId != Guid.Empty && b.FootprintId == footPrintId)
            || (massId != Guid.Empty && b.EnvelopeId == massId));
            return building != null;
        }

        public static List<Building> FindBuildingsInParcels(List<Parcel> parcels)
        {
            var parcelGuids = parcels.Select(p => p.OriginalObjectID).ToList();
            var buildings = Buildings.Where(b => parcelGuids.Contains(b.HostParcelId)).ToList();

            return buildings;
        }

        public static void ClearModel()
        {
            _roads = new();
            ROWs = new();
            Buildings = new();
            Parcels= new();
            ResetStudyAreas();
        }

        public static void ResetStudyAreas()
        {
            Studies = new();
            AddStudyArea(new StudyArea(
                null,
                Guid.NewGuid(),
                CoreGeneralConstants.FullProjectStudyName,
                new List<Parcel>(),
                new List<Building>(),
                new List<Road>()));
        }

        public static void UpdateParcelUses(List<(Point point, Landuse use)> landuseData)
        {
            foreach(var p in Parcels)
            {
                var closestRhinoData = landuseData.MinBy(data => data.point.Distance(p.Boundary.Centroid()));
                p.LandUse = closestRhinoData.use;
            }
        }
    }
}
