using Rhino.Geometry;
using AIQ.Urban.Core.Extensions;

namespace AIQ.Urban.Core.ModelActions.CreationData;

public record TransitUpdateData : ICreationData
{
    public Guid OldCenterlineId { get; }
    public Curve NewCenterline { get; }

    public TransitUpdateData(Guid oldCenterlineId, Curve newCenterline)
    {
        OldCenterlineId = oldCenterlineId;
        NewCenterline = newCenterline.NormalizeTransitCenterline();
    }
}
