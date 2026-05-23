namespace AIQ.MCP.Models;

/// <summary>Summary of a single urban element for AI consumption. Serialized via custom JSON serializer.</summary>
public record ElementSummary(
    Guid Id,
    string Type,
    string? Use,
    string Summary,
    Dictionary<string, object>? Details);

/// <summary>Response from the list_elements tool.</summary>
public record ListElementsResponse(
    List<ElementSummary> Transits,
    List<ElementSummary> Buildings,
    List<ElementSummary> Parcels,
    List<ElementSummary> Zones);
