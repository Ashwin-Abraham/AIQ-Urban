using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Models;
using BH.Engine.Data;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Metrics
{
    public class StudyAreaMetrics
    {
        public double BoundedArea { get; private set; } = 25;
        public double FAR { get; private set; } = 12;
        public double DevelopableArea { get; private set; } = 5;
        public double BuiltArea { get; private set; } = 2;
        public double BuiltCoverageArea { get; private set; } = 0;
        public double CoverageRatio { get; private set; } = 0;
        public List<ParcelMetrics> ParcelMetrics = new();
        public List<TransitMetrics> TransitMetrics = new();

        private void UpdateParcelMetrics(
            ICurve? boundary, 
            List<Parcel> parcels, 
            List<Building> buildings,
            List<Road> transitRoutes)
        {
            CalculateParcelMetrics(parcels, buildings);
            CalculateTransitMetrics(transitRoutes);

            BoundedArea = Math.Abs(boundary?.IArea()?? 0);
            DevelopableArea = ParcelMetrics.Sum(p => p.Area);
            BuiltArea = buildings.SelectMany(b => b.FloorPlates)
                .Sum(flr => Math.Abs(flr.Profile.Area()));
            FAR = BuiltArea / DevelopableArea;
            BuiltCoverageArea = SetBuiltCoverage(buildings);
            CoverageRatio = BuiltCoverageArea / DevelopableArea;
        }

        private double SetBuiltCoverage(List<Building> buildings)
        {
            return buildings.Select(x => x.FloorPlates.MinBy(f => f.Profile.ExternalBoundary.IStartPoint().Z))
                .Sum(floor => floor.Profile.ExternalBoundary.IArea());
        }

        private void CalculateParcelMetrics(List<Parcel> parcels, List<Building> buildings)
        {
            ParcelMetrics = new();
            if (parcels.Count == 0)
                return;

            foreach (Parcel p in parcels)
            {
                var buildingsOnParcel = buildings
                    .Where(b => p.Boundary.ExternalBoundary.IIsContaining([b.Origin]))
                    .ToList();

                ParcelMetrics.Add(new ParcelMetrics(p, buildingsOnParcel));
            }
        }

        private void CalculateTransitMetrics(List<Road> transitRoutes)
        {
            if (transitRoutes.Count == 0)
                return;

            var groupedByWidth = transitRoutes.GroupBy(b =>  Math.Round(b.Width, CoreGeneralConstants.TransitRouteWidthPrecision));
            TransitMetrics.Clear();

            foreach(var roadType in groupedByWidth)
            {
                TransitMetrics.Add(new TransitMetrics(roadType));
            }    
        }

        public StudyAreaMetrics(
            ICurve? boundary, 
            List<Parcel> parcels, 
            List<Building> buildings,
            List<Road> transitRoutes)
            //List<Building> buildings, 
            //List<Road> roads,
            //List<PlanarSurface> roadRoWs) 
        {
            UpdateParcelMetrics(boundary, parcels, buildings, transitRoutes);
        }
    }
}
