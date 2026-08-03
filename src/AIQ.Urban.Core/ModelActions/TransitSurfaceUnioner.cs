using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.Core.Models.GeometryElements;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rhino.Geometry;

namespace AIQ.Urban.Core.ModelActions;
public class TransitSurfaceUnioner(double _tolerance = 0.001, ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<TransitSurfaceUnioner>();

    public StepResult Run(UrbanModel model, List<IModelEditData> creationData)
    {
        var oldIds = model.TransitSurfaces.Select(x => x.Profile.Id).ToList();
        model.TransitSurfaces.Clear();
        var surfaceIds = new List<Guid>();

        var byType = model.Transits
            .GroupBy(t => t.TransitType)
            .ToList();

        foreach (var group in byType)
        {
            var outlines = group
                .Select(t => t.Outline.Geometry)
                .ToList();

            if (outlines.Count == 0)
                continue;

            var unionResult = Brep.CreatePlanarUnion(outlines, model.Plane, _tolerance);
            
            if (unionResult is null || unionResult.Length == 0)
                continue;

            foreach (var brep in unionResult)
            {
                var profile = Profile2D.Create(brep);
                if (profile is null)
                    continue;

                model.TransitSurfaces.Add((profile, group.Key));
                surfaceIds.Add(profile.Id);
            }
        }

        _logger.LogInformation("Unioned transit surfaces for {Count} types", model.TransitSurfaces.Count);

        return new(surfaceIds, oldIds);
    }
}
