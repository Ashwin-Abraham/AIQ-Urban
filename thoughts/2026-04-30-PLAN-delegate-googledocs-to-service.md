# GoogleSlidesDocument Refactoring - Delegate to Service

## Overview

Refactor `GoogleSlidesDocument` to delegate its service responsibilities (API calls, credential handling, HTTP operations) to a private `GoogleSlidesService` instance, while keeping the same public API. This follows the delegation pattern and separates concerns: Document = state, Service = behavior.

## Current State Analysis

**`GoogleSlidesDocument.cs`** (`AIQ.Exports/GoogleSlides/GoogleSlidesDocument.cs`):
- Line 69-82: `CreateService()` creates and configures `SlidesService` with OAuth credentials
- Line 26-44: `ValidateAsync()` creates service inline to call Google API
- Line 46-67: `ExecuteAsync()` creates service inline to batch update slides
- Mixes document state (presentationId, pending actions) with service behavior (API calls)

**`GoogleSlidesService.cs`** (`AIQ.Exports/GoogleSlides/GoogleSlidesService.cs`):
- Line 7-24: Already has its own `SlidesService` instance and credential handling
- Line 28-51: Has `GetSlidesAsync()`, `FindSlideByIndexAsync()`, `FindSlideByTitleAsync()`
- Line 53-107: Has `CreateReplaceAllTextRequest()`, `ExecuteBatchUpdateAsync()`, `ReplaceTextAsync()` overloads
- Line 109-112: `ExecuteBatchAsync(GoogleSlidesDocument)` is a no-op delegation back to document (to be ignored per requirements)

**Consumers** (unchanged by this refactoring):
- `GoogleSlidesDocComponent.cs:69-78` - Creates `GoogleSlidesDocument`, calls `ValidateAsync().Result`
- `GoogleSlidesDocumentGoo.cs` - Wraps `GoogleSlidesDocument`
- `AddActionComponent.cs` - Uses `GoogleSlidesDocument.AddAction()`
- `BatchExecuteComponent.cs` - Uses `GoogleSlidesDocument.ExecuteAsync()`
- `GoogleSlidesIntegrationTests.cs:12-13` - Creates document, calls `ValidateAsync()`

## Desired End State

### `GoogleSlidesDocument` (state + delegation)
```csharp
public class GoogleSlidesDocument
{
    private readonly GoogleSlidesService _service;
    
    public string PresentationId { get; }
    public List<IGoogleSlidesAction> PendingActions { get; } = [];
    
    public GoogleSlidesDocument(string presentationId, string serviceAccountJsonPath)
    {
        PresentationId = presentationId;
        _service = new GoogleSlidesService(serviceAccountJsonPath, presentationId);
    }
    
    public async Task<bool> ValidateAsync(CancellationToken ct = default) 
        => await _service.ValidateAsync(ct);
    
    public async Task<int> ExecuteAsync(CancellationToken ct = default)
    {
        var count = await _service.ExecuteBatchAsync(PendingActions, ct);
        ClearActions();
        return count;
    }
    
    // Existing ReplaceTextAsync overloads delegate to service
    public async Task<int> ReplaceTextAsync(string findText, string replaceText, string slideTitle)
        => await _service.ReplaceTextAsync(findText, replaceText, slideTitle);
    
    public async Task<int> ReplaceTextAsync(string findText, string replaceText, int slideIndex)
        => await _service.ReplaceTextAsync(findText, replaceText, slideIndex);
    
    public async Task<int> ReplaceTextAsync(string findText, string replaceText)
        => await _service.ReplaceTextAsync(findText, replaceText);
    
    public void AddAction(IGoogleSlidesAction action) => PendingActions.Add(action);
    public void ClearActions() => PendingActions.Clear();
}
```

### `GoogleSlidesService` (all behavior)
```csharp
public class GoogleSlidesService
{
    private readonly SlidesService _service;
    private readonly string _presentationId;
    
    public GoogleSlidesService(string serviceAccountJsonPath, string presentationId)
    {
        // Read JSON file, create credential, init SlidesService
        _presentationId = presentationId;
    }
    
    public async Task<bool> ValidateAsync(CancellationToken ct = default);
    public async Task<int> ExecuteBatchAsync(List<IGoogleSlidesAction> actions, CancellationToken ct = default);
    public async Task<int> ReplaceTextAsync(string findText, string replaceText, string slideTitle);
    public async Task<int> ReplaceTextAsync(string findText, string replaceText, int slideIndex);
    public async Task<int> ReplaceTextAsync(string findText, string replaceText);
    
    // Existing helper methods stay
    private static ReplaceAllTextRequest CreateReplaceAllTextRequest(...);
    private async Task<int> ExecuteBatchUpdateAsync(...);
    public async Task<IList<Page>> GetSlidesAsync();
    public async Task<Page?> FindSlideByIndexAsync(int index);
    public async Task<Page?> FindSlideByTitleAsync(string title);
}
```

### Key Changes:
- `GoogleSlidesService` constructor changes from `(string presentationId, string serviceAccountJson)` to `(string serviceAccountJsonPath, string presentationId)` - file path instead of JSON content
- `GoogleSlidesService.ExecuteBatchAsync` gains proper implementation that takes actions list and makes API call
- `GoogleSlidesDocument` removes `CreateService()`, all `using` statements for Google APIs
- All public API on `GoogleSlidesDocument` stays the same

## What We're NOT Doing

- Changing `GoogleSlidesDocument` constructor signature
- Changing `IGoogleSlidesDocument` interface
- Changing `GoogleSlidesDocumentGoo` or any Grasshopper components
- Removing `ExecuteBatchAsync(GoogleSlidesDocument)` from service (leave for now)
- Changing test file paths or test behavior

## Implementation Approach

Single phase: move service logic from document to service, update document to delegate. Tests verify same external behavior.

## Phase 1: Delegate Document Behavior to Service

### Overview

Move `CreateService()` and API call logic from `GoogleSlidesDocument` into `GoogleSlidesService`, then update document methods to delegate to the service instance.

### Changes Required:

#### 1. `GoogleSlidesService` - Update constructor and add `ExecuteBatchAsync` with actions

**File**: `AIQ.Exports/GoogleSlides/GoogleSlidesService.cs`

**Changes**:
- Change constructor to accept `serviceAccountJsonPath` (file path) instead of `serviceAccountJson` (string content)
- Read file content internally: `File.ReadAllText(serviceAccountJsonPath)`
- Add proper `ExecuteBatchAsync(List<IGoogleSlidesAction> actions, CancellationToken ct)` implementation that converts actions to requests and calls batch update API
- Add `using AIQ.Exports.GoogleSlides.Actions;`

```csharp
// Constructor change
public GoogleSlidesService(string serviceAccountJsonPath, string presentationId)
{
    string json = File.ReadAllText(serviceAccountJsonPath);
    var credential = GoogleCredential.FromJson(json).CreateScoped(Scopes);
    _service = new SlidesService(new BaseClientService.Initializer
    {
        HttpClientInitializer = credential,
        ApplicationName = "AIQ.Exports"
    });
    _presentationId = presentationId;
}

// New ExecuteBatchAsync implementation
public async Task<int> ExecuteBatchAsync(List<IGoogleSlidesAction> actions, CancellationToken cancellationToken = default)
{
    if (actions.Count == 0) return 0;
    
    var requests = actions.Select(action => action.ToRequest(_presentationId)).ToList();
    var batchRequest = new BatchUpdatePresentationRequest { Requests = requests };
    
    var response = await _service.Presentations.BatchUpdate(batchRequest, _presentationId).ExecuteAsync(cancellationToken);
    
    return response.Replies?.Sum(r => 
        (r.ReplaceAllText?.OccurrencesChanged ?? 0) +
        (r.ReplaceAllShapesWithImage?.OccurrencesChanged ?? 0)) ?? 0;
}
```

#### 2. `GoogleSlidesDocument` - Remove service logic, delegate to service

**File**: `AIQ.Exports/GoogleSlides/GoogleSlidesDocument.cs`

**Changes**:
- Add `private readonly GoogleSlidesService _service;` field
- Remove `CreateService()` method entirely
- Remove `using` statements no longer needed: `Google.Apis.Slides.v1`, `Google.Apis.Slides.v1.Data`, `Google.Apis.Auth.OAuth2`, `Google.Apis.Services`
- Add `using AIQ.Exports.GoogleSlides.Actions;` if not present
- Constructor creates `_service` instance
- `ValidateAsync()` delegates: `await _service.ValidateAsync(cancellationToken)`
- `ExecuteAsync()` delegates: `await _service.ExecuteBatchAsync(PendingActions, cancellationToken)`, then `ClearActions()`, return count
- Add `ReplaceTextAsync` overloads that delegate to service
- `PresentationId`, `PendingActions`, `AddAction()`, `ClearActions()` stay unchanged

```csharp
public class GoogleSlidesDocument : IGoogleSlidesDocument
{
    private readonly GoogleSlidesService _service;
    
    public GoogleSlidesDocument(string presentationId, string serviceAccountJsonPath)
    {
        PresentationId = presentationId;
        _service = new GoogleSlidesService(serviceAccountJsonPath, presentationId);
    }
    
    public string PresentationId { get; }
    public List<IGoogleSlidesAction> PendingActions { get; } = [];
    
    public void AddAction(IGoogleSlidesAction action) => PendingActions.Add(action);
    public void ClearActions() => PendingActions.Clear();
    
    public async Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
        => await _service.ValidateAsync(cancellationToken);
    
    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var count = await _service.ExecuteBatchAsync(PendingActions, cancellationToken);
        ClearActions();
        return count;
    }
    
    public async Task<int> ReplaceTextAsync(string findText, string replaceText, string slideTitle)
        => await _service.ReplaceTextAsync(findText, replaceText, slideTitle);
    
    public async Task<int> ReplaceTextAsync(string findText, string replaceText, int slideIndex)
        => await _service.ReplaceTextAsync(findText, replaceText, slideIndex);
    
    public async Task<int> ReplaceTextAsync(string findText, string replaceText)
        => await _service.ReplaceTextAsync(findText, replaceText);
}
```

#### 3. Update `IGoogleSlidesDocument` interface if needed

**File**: `AIQ.Exports/GoogleSlides/IGoogleSlidesDocument.cs`

**Changes**:
- Add `ReplaceTextAsync` overloads to interface to match new document methods:

```csharp
public interface IGoogleSlidesDocument
{
    string PresentationId { get; }
    List<IGoogleSlidesAction> PendingActions { get; }
    Task<bool> ValidateAsync(CancellationToken cancellationToken = default);
    Task<int> ExecuteAsync(CancellationToken cancellationToken = default);
    void AddAction(IGoogleSlidesAction action);
    void ClearActions();
    Task<int> ReplaceTextAsync(string findText, string replaceText, string slideTitle);
    Task<int> ReplaceTextAsync(string findText, string replaceText, int slideIndex);
    Task<int> ReplaceTextAsync(string findText, string replaceText);
}
```

#### 4. Update integration test to use document constructor as-is

**File**: `AIQ.Exports.Tests/GoogleSlides/GoogleSlidesIntegrationTests.cs`

**Changes**: No changes needed - constructor signature is unchanged, `ValidateAsync()` behavior is the same.

### Success Criteria:

#### Automated Verification:
- [x] Build succeeds: `dotnet build ArchSitePlan.sln`
- [x] All tests pass: `dotnet test` (8/8 GoogleSlides tests pass; 1 pre-existing unrelated failure in ModifyChartData)
- [x] No compilation errors in `AIQ.Exports` project: `dotnet build AIQ.Exports/AIQ.Exports.csproj`
- [x] No compilation errors in `AIQ.GH.Exports` project: `dotnet build AIQ.GH.Exports/AIQ.GH.Exports.csproj` (4 pre-existing errors unrelated to this change)

## Testing Strategy

### Unit Tests:
- Existing `GoogleSlidesUnitTests.cs` uses mocks of `IGoogleSlidesDocument` - no changes needed since interface stays same
- Existing `GoogleSlidesIntegrationTests.cs` creates real `GoogleSlidesDocument` and calls `ValidateAsync()` - should work unchanged since public API is identical

### Edge Cases:
- `ExecuteAsync()` with empty pending actions should return 0 (handled by service)
- `ValidateAsync()` with invalid credentials should return false (behavior unchanged)

## Migration Notes

- Zero migration needed - public API of `GoogleSlidesDocument` is unchanged
- Grasshopper components (`GoogleSlidesDocComponent`, `AddActionComponent`, `BatchExecuteComponent`) require no changes
- `GoogleSlidesDocumentGoo` wrapping requires no changes

## References

- Original concern: `GoogleSlidesDocument` carries service responsibilities
- `GoogleSlidesDocument.cs`: `AIQ.Exports/GoogleSlides/GoogleSlidesDocument.cs`
- `GoogleSlidesService.cs`: `AIQ.Exports/GoogleSlides/GoogleSlidesService.cs`
- `IGoogleSlidesDocument.cs`: `AIQ.Exports/GoogleSlides/IGoogleSlidesDocument.cs`
- Similar delegation pattern: none in codebase yet - this establishes the pattern
