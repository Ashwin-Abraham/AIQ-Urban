using Rhino.Geometry;
using AIQ.Urban.Core.Extensions;

namespace AIQ.Urban.Core.ModelActions.EditData;

public record TransitUpdateData : IModelEditData
{
    public Guid OldCenterlineId { get; }
    public Curve NewCenterline { get; }

    public TransitUpdateData(Guid oldCenterlineId, Curve newCenterline)
    {
        OldCenterlineId = oldCenterlineId;
        NewCenterline = newCenterline.NormalizeTransitCenterline();
        if (NewCenterline.IsSelfIntersecting())
            throw new ArgumentException("Transit centerline must not self-intersect.", nameof(newCenterline));
    }
}
