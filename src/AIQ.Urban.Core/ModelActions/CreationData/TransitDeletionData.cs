namespace AIQ.Urban.Core.ModelActions.CreationData;

public record TransitDeletionData(List<Guid> CenterlineIds) : ICreationData;
