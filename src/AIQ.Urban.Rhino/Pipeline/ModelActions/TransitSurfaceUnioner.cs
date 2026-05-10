using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoPlugin.Pipeline.ModelActions;
internal class TransitSurfaceUnioner(double _tolerance = 0.001) : IAddUrbanElementsStep
{
    List<Guid> IUrbanModelStep.Run(UrbanModel model)
    {
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

            var unionResult = Brep.CreateBooleanUnion(outlines, _tolerance);

            if (unionResult is null || unionResult.Length == 0)
                continue;

            foreach (var brep in unionResult)
            {
                var face = brep.Faces[0];

                if (face is null)
                    continue;

                var profile = Profile2D.Create(face);
                if (profile is null)
                    continue;

                model.TransitSurfaces.Add((profile, group.Key));
                surfaceIds.Add(profile.Id);
            }
        }

        return surfaceIds;
    }
}
