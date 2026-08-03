using AIQ.Urban.Core.Models;
using Rhino.Geometry;

namespace AIQ.Urban.Core.ModelActions.EditData;

public record CentroidUseData(Point3d Centroid, Use Use) : IModelEditData;
