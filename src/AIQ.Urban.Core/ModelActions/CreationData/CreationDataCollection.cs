namespace AIQ.Urban.Core.ModelActions.CreationData;

public record CreationDataCollection<T>(List<T> Items) : ICreationData
    where T : ICreationData;
