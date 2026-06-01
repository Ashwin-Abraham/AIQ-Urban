using System;
using System.Collections.Generic;

namespace AIQ.Urban.RhinoInterface.MCP_Bridge;

public record ElementSummary(
    Guid Id,
    string Type,
    string? Use,
    string Summary,
    Dictionary<string, object>? Details);

public record ListElementsResponse(
    List<ElementSummary> Transits,
    List<ElementSummary> Buildings,
    List<ElementSummary> Parcels,
    List<ElementSummary> Zones);
