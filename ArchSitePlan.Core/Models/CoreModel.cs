using System.Drawing;
using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.Core.Comparer;
using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Models
{
    public static class CoreModel
    {
        private static StudyArea _allStudy = new(null, Guid.NewGuid(), CoreGeneralConstants.FullProjectStudyName, new(), new(), new()); 
        public static List<Road> Roads = new List<Road>();
        public static List<PlanarSurface> ROWs = new List<PlanarSurface>();
        public static List<Parcel> Parcels = new List<Parcel>();
        public static List<Building> Buildings = new List<Building>();
        public static List<StudyArea> Studies = new([_allStudy]);
        public static List<ParcelDivider> Dividers = new ();
        static CoreModel() { }

        public static void AddStudyArea(StudyArea studyArea) => Studies.Add(studyArea);
        public static void RemoveStudyArea(StudyArea studyArea) => Studies.Remove(studyArea);

        public static void RecalculateBuildingHostParcelIDs()
        {
            foreach(var building in Buildings)
            {
                var parcel = Parcels.FirstOrDefault(p => p.Boundary.ExternalBoundary.IIsContaining([building.Origin]));
                building.HostParcelCoreModelId = parcel?.CoreModelId?? Guid.Empty;
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
                        .Where(p => Studies[i].Boundary.IIsContaining([p.Boundary.ControlPointCentroid()]))
                        .ToList();

                    var buildingsInArea = Buildings
                        .Where(bldg =>
                        {
                            var boundaryPt = Studies[i].Boundary.IStartPoint();
                            var bldgPt = bldg.Origin.Project(Create.Plane(boundaryPt, Vector.ZAxis));
                            return Studies[i].Boundary.IIsContaining([bldgPt]);
                        })
                        .ToList();

                    var roadsInArea = Roads.Where(road =>
                    {
                        var roadPoint = road.Centerline.PointAtParameter(0.5);
                        return Studies[i].Boundary.IIsContaining([roadPoint]);
                    }).ToList();

                    Studies[i] = new StudyArea(Studies[i], parcelsInArea, buildingsInArea, roadsInArea);
                }
            }
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

                var nearest = Parcels.FirstOrDefault(p => p.Boundary.ExternalBoundary.IIsContaining([origin]));
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

        public static bool TryGetStudyArea(string name, out StudyArea? study)
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
            var parcelGuids = parcels.Select(p => p.CoreModelId).ToList();
            var buildings = Buildings.Where(b => parcelGuids.Contains(b.HostParcelCoreModelId)).ToList();

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
