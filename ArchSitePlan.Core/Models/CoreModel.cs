using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Metrics;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Core.Models
{
    public record StudyAreasAndMetrics(StudyArea Study, StudyAreaMetrics Metrics);
    public static class CoreModel
    {
        private static List<Road> _roads  = new List<Road>();
        public static IReadOnlyList<Road> Roads => _roads.AsReadOnly();
        public static List<PlanarSurface> ROWs = new List<PlanarSurface>();
        public static List<Parcel> Parcels = new List<Parcel>();
        public static List<Building> Buildings = new List<Building>();
        public static List<StudyAreasAndMetrics> StudiesAndMetrics = new ();
        static CoreModel() { }

        public static void AddStudyArea(StudyArea studyArea) => StudiesAndMetrics.Add(new StudyAreasAndMetrics(studyArea, null));
        public static void RemoveStudyArea(StudyArea studyArea) => StudiesAndMetrics.RemoveAll(sam => sam.Study == studyArea);

        public static void RefreshStudyAreaMetrics(bool RecalculatModelSplit = true)
        {
            if (RecalculatModelSplit) 
                SplitCoreModelByStudyAreas();

            for (int i = 0; i < StudiesAndMetrics.Count; i++) 
            {
                var parcelsInArea = Parcels
                    .Where(p => StudiesAndMetrics[i].Study.Boundary.IsContaining(p.Boundary.Centroid().ObjectToList()))
                    .ToList();

                StudiesAndMetrics[i] = new StudyAreasAndMetrics(
                    StudiesAndMetrics[i].Study,
                    new StudyAreaMetrics(parcelsInArea));
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
            var studyBoundaries = StudiesAndMetrics.Select(sa => sa.Study.Boundary).ToList();

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
            var splitters = StudiesAndMetrics.Select(sa => sa.Study.Boundary)
                .SelectMany(pline => pline.Segments())
                .ToList();

            var studyBoundaries = StudiesAndMetrics.Select(sa => sa.Study.Boundary).ToList();


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

        public static bool TryGetStudyArea(Guid id, out StudyArea area)
        {
            area = StudiesAndMetrics.Find(s => s.Study.Id == id).Study;
            return area != null;
        }

        public static bool TryAddStudyArea(Polyline boundary, Guid id, string name)
        {
            if (StudiesAndMetrics.Any(s => s.Study.Id == id)) return false;

            StudiesAndMetrics.Add( new StudyAreasAndMetrics( new StudyArea(boundary, id, name), null));
            return true;
        }
    }
}
