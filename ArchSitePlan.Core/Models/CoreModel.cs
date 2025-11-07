using System.Drawing;
using ArchSitePlan.Core.Comparer;
using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Models.CreationData;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using Newtonsoft.Json;

namespace ArchSitePlan.Core.Models
{
    public class CoreModel
    {
        private StudyArea _allStudy;

        public List<Road> Roads { get; set; }
        public List<PlanarSurface> ROWs { get; set; }
        public List<Parcel> Parcels { get; set; } 
        public List<Landuse> Landuses { get; set; }
        public List<Building> Buildings { get; set; }

        [JsonIgnore]
        public List<StudyArea> Zones { get; set; }

        public List<ParcelDivider> Dividers { get; set; }
        public CoreModel() 
        {
            Roads = [];
            ROWs = [];
            Parcels = [];
            Buildings = [];
            Dividers = [];

            _allStudy = new(null, Guid.NewGuid(), CoreGeneralConstants.FullProjectStudyName, new(), new(), new());
            Zones = new List<StudyArea>() { _allStudy };

            Landuses = AppLanduses.InitialUses.ToList();
        }

        public void AddStudyArea(StudyArea studyArea) => Zones.Add(studyArea);

        public void RecalculateBuildingHostParcelIDs()
        {
            foreach(var building in Buildings)
            {
                var parcel = Parcels.FirstOrDefault(p => p.Boundary.ExternalBoundary.IIsContaining([building.Origin]));
                building.HostParcelCoreModelId = parcel?.CoreModelId?? Guid.Empty;
            }
        }

        public void ClearModel()
        {
            Roads = new();
            ROWs = new();
            Buildings = new();
            Parcels= new();
            Dividers= new();
            Landuses = AppLanduses.InitialUses.ToList();
            ResetStudyAreas();
        }

        public void ResetStudyAreas()
        {
            Zones = new();
            AddStudyArea(new StudyArea(
                null,
                Guid.NewGuid(),
                CoreGeneralConstants.FullProjectStudyName,
                new List<Parcel>(),
                new List<Building>(),
                new List<Road>()));
        }

        public StudyArea ProjectWideStudy()
            => Zones.FirstOrDefault(s => s.Name == CoreGeneralConstants.FullProjectStudyName);

        public bool TryGetParcelDivider(Guid objectId, out ParcelDivider divider)
        {
            divider = Dividers.FirstOrDefault(div => div.Id == objectId);
            return divider != null;
        }

        public Dictionary<double, Color> SetRoadColorsByWidth()
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

        public Landuse GetUndefinedUse()
        {
            var undefinedTag = AppLanduses.Undefined;
            return Landuses.First(
                lu => lu.Name == undefinedTag.Name
                || lu.Id == undefinedTag.Id);
        }

        internal Landuse GetUse(LanduseData useData)
        {
            var use = Landuses.First(
                lu => lu.Name == useData.Name
                || lu.Id == useData.Id);

            return use == null? GetUndefinedUse() : use;
        }
    }
}
