# Phase 1: Copy Slide and Delete Slide Actions

## Overview

Implement and integration-test two new Google Slides actions: `CopySlideAction` (creates a duplicate of an existing slide) and `DeleteSlideAction` (removes a slide by ID). These actions will be used in subsequent phases to set up/tear down test state.

## Current State Analysis

**Existing `IGoogleSlidesAction` interface** (`AIQ.Exports/GoogleSlides/Actions/IGoogleSlidesAction.cs:4-7`):
```csharp
public interface IGoogleSlidesAction
{
    Request ToRequest(string presentationId);
}
```

**Google Slides API methods available:**
- `presentations.pages.copy` - Copies a slide (requires `pageObjectId`)
- `presentations.batchUpdate` with `DeleteObjectRequest` - Deletes a slide by object ID

**GoogleSlidesService** (`AIQ.Exports/GoogleSlides/GoogleSlidesService.cs:46-58`) already has `ExecuteBatchAsync` that processes `List<IGoogleSlidesAction>`.

## Desired End State

Two new action classes exist and work:
- `CopySlideAction` - Takes a `slideId` (source slide), creates a copy, returns the new slide ID
- `DeleteSlideAction` - Takes a `slideId`, deletes that slide

Integration test verifies both actions work against the real presentation.

### Key Discoveries:
- Google Slides API `DuplicateObjectRequest` can duplicate a slide (https://developers.google.com/slides/api/reference/rest/v1/presentations/request#duplicateobjectrequest)
- `DeleteObjectRequest` can delete a slide by its object ID
- Need to extract the new slide ID from the `DuplicateObjectRequest` response

## What We're NOT Doing

- Implementing a "reorder slide" action
- Adding slide creation from template
- Modifying existing action implementations

## Implementation Approach

1. Create `CopySlideAction` class implementing `IGoogleSlidesAction`
   - Uses `DuplicateObjectRequest` with `objectId` set to source slide ID
   - Returns the new slide ID from the response (need to handle response parsing)

2. Create `DeleteSlideAction` class implementing `IGoogleSlidesAction`
   - Uses `DeleteObjectRequest` with `objectId` set to slide ID

3. Extend `GoogleSlidesService` or `GoogleSlidesDocument` to handle responses that return new object IDs
   - `DuplicateObjectRequest` returns `objectId` of the new duplicate
   - Need a way to capture this for test verification

4. Write integration test that:
   - Copies slide 1 (known slide)
   - Verifies copy exists (slide count increased)
   - Deletes the copied slide
   - Verifies deletion (slide count restored)

## Phase 1: Copy Slide and Delete Slide Actions

### Overview
Implement `CopySlideAction` and `DeleteSlideAction` and verify they work via integration test.

### Changes Required:

#### 1. New Action: CopySlideAction
**File**: `AIQ.Exports/GoogleSlides/Actions/CopySlideAction.cs`
**Changes**: Create new class that duplicates a slide using `DuplicateObjectRequest`

```csharp
namespace AIQ.Exports.GoogleSlides.Actions;
public class CopySlideAction(string slideId) : IGoogleSlidesAction
{
    public string SlideId { get; } = slideId;

    public Request ToRequest(string presentationId)
    {
        return new Request
        {
            DuplicateObject = new DuplicateObjectRequest
            {
                ObjectId = SlideId
            }
        };
    }
}
```

#### 2. New Action: DeleteSlideAction
**File**: `AIQ.Exports/GoogleSlides/Actions/DeleteSlideAction.cs`
**Changes**: Create new class that deletes a slide using `DeleteObjectRequest`

```csharp
namespace AIQ.Exports.GoogleSlides.Actions;
public class DeleteSlideAction(string slideId) : IGoogleSlidesAction
{
    public string SlideId { get; } = slideId;

    public Request ToRequest(string presentationId)
    {
        return new Request
        {
            DeleteObject = new DeleteObjectRequest
            {
                ObjectId = SlideId
            }
        };
    }
}
```

#### 3. Extend GoogleSlidesService to Return New Object IDs
**File**: `AIQ.Exports/GoogleSlides/GoogleSlidesService.cs`
**Changes**: Add method to execute batch and return detailed replies including new object IDs

```csharp
public async Task<BatchUpdatePresentationResponse> ExecuteBatchWithResponseAsync(
    List<IGoogleSlidesAction> actions,
    CancellationToken cancellationToken = default)
{
    if (actions.Count == 0) return new BatchUpdatePresentationResponse();

    var requests = actions.Select(action => action.ToRequest(_presentationId)).ToList();
    var batchRequest = new BatchUpdatePresentationRequest { Requests = requests };

    return await _service.Presentations.BatchUpdate(batchRequest, _presentationId)
        .ExecuteAsync(cancellationToken);
}
```

#### 4. Extend GoogleSlidesDocument to Support New Method
**File**: `AIQ.Exports/GoogleSlides/GoogleSlidesDocument.cs`
**Changes**: Add `ExecuteWithResponseAsync` method

```csharp
public async Task<BatchUpdatePresentationResponse> ExecuteWithResponseAsync(
    CancellationToken cancellationToken = default)
{
    var response = await _service.ExecuteBatchWithResponseAsync(PendingActions, cancellationToken);
    ClearActions();
    return response;
}
```

#### 5. Integration Test for Copy and Delete
**File**: `AIQ.Exports.Tests/GoogleSlides/GoogleSlidesIntegrationTests.cs`
**Changes**: Add test that copies slide 1, verifies, then deletes

```csharp
[Fact]
public async Task CanCopyAndDeleteSlide()
{
    var serviceAccountPath = GoogleSlidesConfig.ServiceAccountPath;
    var presentationId = GoogleSlidesConfig.PresentationId;
    var doc = new GoogleSlidesDocument(presentationId, serviceAccountPath);

    // Get initial slide count
    var initialSlides = await GetSlideCountAsync(doc);
    
    // Copy slide 1 (assumes slide with ID "p" or get first slide)
    var firstSlideId = await GetFirstSlideIdAsync(doc);
    doc.AddAction(new CopySlideAction(firstSlideId));
    var response = await doc.ExecuteWithResponseAsync();
    
    // Verify new slide created - get the new slide ID from response
    var newSlideId = response.Replies?.FirstOrDefault()?.DuplicateObject?.ObjectId;
    Assert.NotNull(newSlideId);
    
    var slidesAfterCopy = await GetSlideCountAsync(doc);
    Assert.Equal(initialSlides + 1, slidesAfterCopy);
    
    // Delete the copied slide
    doc.AddAction(new DeleteSlideAction(newSlideId));
    await doc.ExecuteAsync();
    
    var slidesAfterDelete = await GetSlideCountAsync(doc);
    Assert.Equal(initialSlides, slidesAfterDelete);
}

private async Task<int> GetSlideCountAsync(GoogleSlidesDocument doc)
{
    // Need to get presentation and count slides
    var service = new GoogleSlidesService(GoogleSlidesConfig.ServiceAccountPath, doc.PresentationId);
    var presentation = await service.GetPresentationAsync(); // May need to add this method
    return presentation.Slides.Count;
}
```

**Note**: May need to add a `GetPresentationAsync` method to `GoogleSlidesService` to retrieve slide count.

### Success Criteria:

#### Automated Verification:
- [ ] `AIQ.Exports` builds: `dotnet build AIQ.Exports/AIQ.Exports.csproj`
- [ ] `AIQ.Exports.Tests` builds: `dotnet build AIQ.Exports.Tests/AIQ.Exports.Tests.csproj`
- [ ] New action classes exist: `CopySlideAction.cs`, `DeleteSlideAction.cs`
- [ ] Integration test `CanCopyAndDeleteSlide` passes: `dotnet test --filter "FullyQualifiedName~CanCopyAndDeleteSlide"`

#### Manual Verification:
- [ ] After running the test, verify in Google Slides that no extra slides remain (slide count unchanged)
- [ ] Confirm the test output shows successful copy and delete operations

**Implementation Notes**: 
- This phase is only complete if all automated verification passes
- Pause here for manual confirmation from the human that the manual testing was successful before proceeding to Phase 2

## Testing Strategy

### Unit Tests:
- `CopySlideAction` creates correct `DuplicateObjectRequest`
- `DeleteSlideAction` creates correct `DeleteObjectRequest`

### Integration Tests:
- `CanCopyAndDeleteSlide` - Full integration test against real presentation

## References

- Google Slides API DuplicateObject: https://developers.google.com/slides/api/reference/rest/v1/presentations/request#duplicateobjectrequest
- Google Slides API DeleteObject: https://developers.google.com/slides/api/reference/rest/v1/presentations/request#deleteobjectrequest
- Existing actions pattern: `AIQ.Exports/GoogleSlides/Actions/ReplaceTextAction.cs`
