using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.ModelActions.CreationData;

public record FloorPlateReplaceData(FloorPlate OldPlate, double NewZ) : ICreationData;
