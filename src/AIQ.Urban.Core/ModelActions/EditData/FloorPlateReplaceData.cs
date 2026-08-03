using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.ModelActions.EditData;

public record FloorPlateReplaceData(FloorPlate OldPlate, double NewZ) : IModelEditData;
