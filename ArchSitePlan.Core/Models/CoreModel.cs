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

        public static void RefreshStudyAreaMetrics(bool RecalculatModelSplit = true)
        {
            if (RecalculatModelSplit) 
                SplitCoreModelByStudyAreas();

            for (int i = 0; i < Studies.Count; i++) 
            {
                var parcelsInArea = Parcels
                    .Where(p => Studies[i].Boundary.IsContaining(p.Boundary.Centroid().ObjectToList()))
                    .ToList();

                Studies[i] = new StudyArea(Studies[i], parcelsInArea);
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
                .SelectMany(pline => pline.Segments())
                .ToList();

            var studyBoundaries = Studies.Select(sa => sa.Boundary).ToList();


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

                AddRoad(newCL, road.CenterLineId, road.Width );
                _roads.Remove(road);
            }

            return roadsToUpdate.Any();
        }

        public static bool TryRemoveRoad(Guid roadCLId)
        {
            int count = _roads.RemoveAll(r => r.CenterLineId == roadCLId);
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

            var parcels = union.Where(pCrv => !roads.Contains(pCrv))
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

        public static void AddRoad(Polyline centerLine, Guid id, double width)
        {
            var pCurve = Create.PolyCurve(centerLine.ObjectToList());
            if(Road.TryCreateRoad(pCurve, id, width, out Road road))
            {
                TryRemove(road.CenterLineId);
                _roads.Add(road);
                UpdateParcelsAndRoWs();
                RefreshStudyAreaMetrics();
            }
        }

        public static void AddOrUpdateRoads(List<(Polyline, Guid, double)> centerLines)
        {
            foreach(var cl in centerLines)
            {
                var pCurve = Create.PolyCurve(cl.Item1.ObjectToList());
                if (Road.TryCreateRoad(pCurve, cl.Item2, cl.Item3, out Road road))
                {
                    TryRemove(road.CenterLineId);
                    _roads.Add(road);
                    
                }
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

        public static IEnumerable<object> GetRoads()
        {
            throw new NotImplementedException();
        }

        public static bool TryAddBuilding(Guid id, 
            PlanarSurface footPrint, 
            int floorCount, 
            double floorToFloorHt)
        {
            if(!Buildings.Any(b => b.Id == id))
            {
                var newBldg = new Building(footPrint, floorCount, floorToFloorHt, id);
                Buildings.Add(newBldg);

                return true;
            }
            return false;
        }

        public static void UpdateBuilding(Guid id, PlanarSurface footPrint, int floorCount, double floorToFloorHt)
        {
            int i = Buildings.FindIndex(b => b.Id == id);

            if (i == -1) return;

            Buildings[i].UpdateProperties(footPrint, floorCount, floorToFloorHt);
        }

        public static bool TryGetStudyAreaAndMetric(Guid id, out StudyArea area)
        {
            area = null;
            var studyAndMetric = Studies.Find(s => s.Id == id);

            if (studyAndMetric == null) return false;

            area = studyAndMetric;
            return true;
        }

        public static bool TryAddOrUpdateStudyArea(Polyline boundary, Guid id, string name)
        {
            if (Studies.Any(s => s.Id == id))
            {
                int index = Studies.FindIndex(s => s.Id == id);
                Studies[index] = new StudyArea(boundary, id, name);
            }
            else
                Studies.Add( new StudyArea(boundary, id, name));
            return true;
        }

        public static bool TryFindRoad(Guid id, out Road road)
        {
            road = Roads.FirstOrDefault(r => r.CenterLineId == id);
            return road != null;
        }
    }
}
