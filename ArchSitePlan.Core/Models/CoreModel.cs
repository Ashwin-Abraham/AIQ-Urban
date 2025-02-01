using System.Drawing;
using ArchSitePlan.Core.Comparer;
using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Models
{
    public static class CoreModel
    {
        public static List<Road> Roads = new List<Road>();
        public static List<PlanarSurface> ROWs = new List<PlanarSurface>();
        public static List<Parcel> Parcels = new List<Parcel>();
        public static List<Building> Buildings = new List<Building>();
        public static List<StudyArea> Studies = new ();
        public static List<ParcelDivider> Dividers = new ();
        static CoreModel() { }

        public static void AddStudyArea(StudyArea studyArea) => Studies.Add(studyArea);
        public static void RemoveStudyArea(StudyArea studyArea) => Studies.Remove(studyArea);

        public static void RecalculateBuildingHostParcelIDs()
        {
            foreach(var building in Buildings)
            {
                var parcel = Parcels.FirstOrDefault(p => p.Boundary.ExternalBoundary.IIsContaining(building.Origin.ObjectToList()));
                building.HostParcelId = parcel?.Id?? Guid.Empty;
            }
        }
        internal static void RefreshStudyAreaMetrics()
        {
            for (int i = 0; i < Studies.Count; i++) 
            {
                if (Studies[i].Boundary == null)
                {
                    Studies[i] = new StudyArea(Studies[i], Parcels, Buildings, Roads.ToList());
                }
                else
                {
                    var parcelsInArea = Parcels
                        .Where(p => Studies[i].Boundary.IIsContaining(p.Boundary.ControlPointCentroid().ObjectToList()))
                        .ToList();

                    var buildingsInArea = Buildings
                        .Where(bldg =>
                        {
                            var boundaryPt = Studies[i].Boundary.IStartPoint();
                            var bldgPt = bldg.Origin.Project(Create.Plane(boundaryPt, Vector.ZAxis));
                            return Studies[i].Boundary.IIsContaining(bldgPt.ObjectToList());
                        })
                        .ToList();

                    var roadsInArea = Roads.Where(road =>
                    {
                        var roadPoint = road.Centerline.PointAtParameter(0.5);
                        return Studies[i].Boundary.IIsContaining(roadPoint.ObjectToList());
                    }).ToList();

                    Studies[i] = new StudyArea(Studies[i], parcelsInArea, buildingsInArea, roadsInArea);
                }
            }
        }
        public static List<Guid> GetBuildingIds()
        => Buildings.Select(b => b.EnvelopeId).ToList();
        
        private static void SplitCoreModel()
        {
            var studyBoundaries = Studies.Select(sa => sa.Boundary)
                .Where(crv => crv != null && crv.IIsClosed())
                .Cast<ICurve>()
                .ToList();

            var dividerCrvs = Dividers
                .Select(d => d.Curve)
                .Cast<ICurve>()
                .ToList();

            dividerCrvs.AddRange(studyBoundaries);

            var splitParcels = new List<Parcel>();
            foreach(var parcel in Parcels)
            {
                var fragments = SplitParcelsByPolylines(
                    parcel.Boundary.ObjectToList(),
                    dividerCrvs);

                splitParcels.AddRange(fragments.Select(f => new Parcel(parcel, f)));
            }

            Parcels = splitParcels;
        }

        private static List<PlanarSurface> SplitParcelsByPolylines(
            List<PlanarSurface> parcels, 
            List<ICurve> dividers)
        {
            var splitParcels = parcels.Select(p => p.ExternalBoundary)
                .SelectMany(crv => crv.Split(dividers))
                .Select(crv => new PlanarSurface(crv, new List<ICurve>()))
                .ToList();

            return splitParcels;
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
                //_roads = splitRoads;
            }
        }
       
        //public static bool TryRemoveRoad(Guid roadCLId)
        //{
        //    int count = Roads.RemoveAll(r => r.CenterLineId == roadCLId);
        //    //UpdateParcelsAndRoWs();
        //    return count > 0;
        //}

        private static void UpdateParcelsAndRoWs()
        {
            //var externalBoundaries = _roads.Select(r => r.RoW.ExternalBoundary);
            //var union = Compute.BooleanUnion(externalBoundaries)
            //    //.Select(pCrv => Create.PlanarSurface(pCrv))
            //    .ToList();

            //var roadPoints = Roads
            //    .Select(r => r.Centerline.PointAtParameter(0.5))
            //    .ToList();

            //var roads = union
            //    .Where(pCrv => pCrv.IsContainingAny(roadPoints))
            //    .ToList();

            ///// TODO: More detailed road ROWs
            //// var roadNetwork = new RoadNetwork(Roads);

            //var parcels = union.Where(pCrv => !roads.Contains(pCrv))
            //    //.Select(p => p.ExternalBoundary.Fillet())
            //    .Select(p => p.ToPolycurve())
            //    .ToList();

            //var studyCrvs = Studies
            //    .Select(s => s.Boundary)
            //    .Where(crv => crv is not null)
            //    .ToList();

            //union.AddRange(studyCrvs.Select(p => p.ToPolycurve()));

            //var dividers = Dividers.Select(d => d.Curve).ToList();
            //dividers.AddRange(studyCrvs);

            //// Divide created parcels
            //parcels = parcels
            //    .SelectMany(p => p.Split(dividers.Cast<ICurve>().ToList()))
            //    .Select(p => p.ToPolycurve())
            //    .ToList();
            
            //var roadSrfs = roads
            //    .Select(r => Create.PlanarSurface(
            //        r, 
            //        parcels.Cast<ICurve>().ToList()))
            //    .ToList();

            //List<Parcel> newParcels = new();
            //foreach(var pCrv in union)
            //{
            //    if (roads.Contains(pCrv)) continue;

            //    Landuse nearestUse = Parcels.Any()?
            //        Parcels.MinBy(p => p.Boundary.ControlPointCentroid().Distance(pCrv.ControlPointCentroid())).LandUse
            //        :Landuse.Undefined;

            //    newParcels.Add(new Parcel(Create.PlanarSurface(pCrv), nearestUse));
            //}

            //Parcels = newParcels;
            //ROWs = roadSrfs;
        }

        public static bool TryAddBuilding(Mesh envelope,
            List<PlanarSurface> floorPlates,
            Guid envelopeID,
            Guid footPrintId,
            double floorToFloorHt)
        {

            if (!TryGetBuilding(footPrintId, envelopeID, out Building building))
            {
                var origin = Building.CalculateOrigin(envelope);

                var nearest = Parcels.FirstOrDefault(p => p.Boundary.ExternalBoundary.IIsContaining(origin.ObjectToList()));
                var use = nearest != null ? nearest.LandUse : AppLanduses.Undefined;

                var newBldg = new Building(
                    envelope,
                    floorPlates,
                    floorToFloorHt,
                    envelopeID,
                    use,
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

        public static bool TryGetStudyArea(string name, out StudyArea study)
        {
            study = Studies.Find(s => s.Name == name);
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

        public static bool TryGetBuilding(Guid footPrintId, Guid massId, out Building? building)
        {
            building = Buildings.FirstOrDefault(b =>
            (footPrintId != Guid.Empty && b.FootprintId == footPrintId)
            || (massId != Guid.Empty && b.EnvelopeId == massId));
            return building != null;
        }

        public static List<Building> FindBuildingsInParcels(List<Parcel> parcels)
        {
            var parcelGuids = parcels.Select(p => p.Id).ToList();
            var buildings = Buildings.Where(b => parcelGuids.Contains(b.HostParcelId)).ToList();

            return buildings;
        }

        public static void ClearModel()
        {
            Roads = new();
            ROWs = new();
            Buildings = new();
            Parcels= new();
            Dividers= new();
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

        

        public static StudyArea ProjectWideStudy()
            => Studies.FirstOrDefault(s => s.Name == CoreGeneralConstants.FullProjectStudyName);

        public static bool TryGetParcelDivider(Guid objectId, out ParcelDivider divider)
        {
            divider = Dividers.FirstOrDefault(div => div.Id == objectId);
            return divider != null;
        }

        public static Dictionary<double, Color> SetRoadColorsByWidth()
        {
            var widths = Roads
                .GroupBy(r => r.Width, new DoubleAsDecimalComparer())
                .Select(x => x.Key)
                .OrderBy(x => x)
                .ToList();

            Dictionary<double, Color> widthColors = new();
            for (int i = 0; i < widths.Count; i++)
            {
                int cIndex = Math.Min(i, AppColours.StandardColors.Count - 1);
                var color = AppColours.StandardColors[cIndex];
                widthColors.Add(widths[i], AppColours.StandardColors[cIndex]);
            }

            return widthColors;
        }
    }
}
