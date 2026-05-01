# Phase 2: Test ReplaceTextAction

## Overview

Create an integration test for `ReplaceTextAction` that copies slide 1, performs text replacement on the copy, allows manual verification, then deletes the copy.

## Current State Analysis

**ReplaceTextAction** (`AIQ.Exports/GoogleSlides/Actions/ReplaceTextAction.cs:5-21`):
- Takes `TextFilter`, `findText`, and `replacementText`
- Uses `ReplaceAllTextRequest` with `ContainsText` and `ReplaceText`
- Optionally filters by `PageObjectIds` (slide name/ID)

**TextFilter** (`AIQ.Exports/GoogleSlides/Filters/TextFilter.cs:10-27`):
- Has `SlideName` (optional, filters to specific slide)
- Has `TextElementName` (not used in ReplaceTextAction directly)
- Has `MatchOption` (MatchCase/DontMatchCase)
- Has `ToSubstringMatchCriteria(findText)` method

**GoogleSlidesDocument** (`AIQ.Exports/GoogleSlides/GoogleSlidesDocument.cs:4-35`):
- Has `PendingActions` list
- `AddAction()` adds to pending
- `ExecuteAsync()` runs batch and clears pending
- Now has `ExecuteWithResponseAsync()` from Phase 1

**Test Config** (`AIQ.Exports/GoogleSlides/GoogleSlidesConfig.cs`):
- `PresentationId`: `1ybxIuFIfwUtJyaHhPeZXje04y9wW01TC2gGFcgEqeBk`
- `ServiceAccountPath`: `@"C:\Users\AshwinAbraham\Downloads\aiq-siteanalysis-5ebc035f0da2.json"`

## Desired End State

Integration test `CanReplaceTextOnCopiedSlide` that:
1. Copies slide 1
2. Creates a `ReplaceTextAction` targeting the copied slide
3. Executes the action
4. Asserts that `ExecuteAsync` returns expected number of occurrences changed
5. **Pauses for manual verification** - user visually confirms text was replaced
6. Deletes the copied slide
7. Test passes only after manual confirmation

### Key Discoveries:
- `ReplaceAllTextRequest` returns `OccurrencesChanged` in the response
- Need to use `TextFilter` with `SlideName` set to the copied slide ID to target only the copy
- Phase 1 `CopySlideAction` returns new slide ID in response

## What We're NOT Doing

- Modifying `ReplaceTextAction` implementation
- Testing `SetChartDataAction`
- Testing `ReplaceImageAction` (that's Phase 3)

## Implementation Approach

1. Add integration test method `CanReplaceTextOnCopiedSlide` to `GoogleSlidesIntegrationTests.cs`
2. Test will:
   - Copy slide 1 using `CopySlideAction`
   - Get new slide ID from response
   - Create `TextFilter` with `SlideName` = new slide ID
   - Create `ReplaceTextAction` to replace some known text on the slide
   - Execute and assert occurrences changed > 0
   - **PAUSE** - output message for manual verification with slide ID
   - After user confirms, delete the slide using `DeleteSlideAction`

3. Need to add a way to "pause" for manual verification in xUnit test
   - Option A: Use `IAsyncLifetime` or assembly fixture with manual trigger
   - Option B: Output slide ID and have a separate "cleanup" test
   - Option C: Use `Debugger.Break()` or console prompt (not ideal for CI)
   - **Recommended**: Output the slide ID to console and have the test wait for a key press or timeout

## Phase 2: Test ReplaceTextAction

### Overview
Integration test for `ReplaceTextAction` using copy/verify/delete pattern.

### Changes Required:

#### 1. Add Helper Method to Get First Slide ID
**File**: `AIQ.Exports/GoogleSlides/GoogleSlidesService.cs`
**Changes**: Add method to get presentation details

```csharp
public async Task<Presentation> GetPresentationAsync(CancellationToken cancellationToken = default)
{
    return await _service.Presentations.Get(_presentationId).ExecuteAsync(cancellationToken);
}
```

**File**: `AIQ.Exports/GoogleSlides/GoogleSlidesDocument.cs`
**Changes**: Expose method to get presentation

```csharp
public async Task<Presentation> GetPresentationAsync(CancellationToken cancellationToken = default)
{
    return await _service.GetPresentationAsync(cancellationToken);
}
```

#### 2. Add Integration Test for ReplaceTextAction
**File**: `AIQ.Exports.Tests/GoogleSlides/GoogleSlidesIntegrationTests.cs`
**Changes**: Add test with manual verification pause

```csharp
[Fact]
public async Task CanReplaceTextOnCopiedSlide()
{
    var serviceAccountPath = GoogleSlidesConfig.ServiceAccountPath;
    var presentationId = GoogleSlidesConfig.PresentationId;
    var doc = new GoogleSlidesDocument(presentationId, serviceAccountPath);

    // Get first slide ID for copying
    var presentation = await doc.GetPresentationAsync();
    var firstSlideId = presentation.Slides.First().ObjectId;

    // Copy slide 1
    doc.AddAction(new CopySlideAction(firstSlideId));
    var copyResponse = await doc.ExecuteWithResponseAsync();
    var copiedSlideId = copyResponse.Replies?.First().DuplicateObject.ObjectId;
    Assert.NotNull(copiedSlideId);

    try
    {
        // Create filter targeting only the copied slide
        var filter = new TextFilter(copiedSlideId, "placeholder", TextMatchOption.DontMatchCase);
        
        // Replace "placeholder" with "TEST_REPLACED" on the copied slide
        doc.AddAction(new ReplaceTextAction(filter, "placeholder", "TEST_REPLACED"));
        var occurrencesChanged = await doc.ExecuteAsync();
        
        // Assert that at least one replacement occurred
        Assert.True(occurrencesChanged > 0);
        
        // PAUSE FOR MANUAL VERIFICATION
        Console.WriteLine($"=== MANUAL VERIFICATION REQUIRED ===");
        Console.WriteLine($"Copied Slide ID: {copiedSlideId}");
        Console.WriteLine($"Please verify in Google Slides that text was replaced to 'TEST_REPLACED'");
        Console.WriteLine($"Presentation: https://docs.google.com/presentation/d/{presentationId}/edit");
        Console.WriteLine($"Press any key to continue after verification...");
        Console.ReadKey();
        
        // TODO: Remove this console read for CI - only for manual verification
    }
    finally
    {
        // Cleanup: delete the copied slide
        doc.AddAction(new DeleteSlideAction(copiedSlideId));
        await doc.ExecuteAsync();
    }
}
```

**Note**: The `Console.ReadKey()` is for manual verification during development. Before committing, either:
- Remove it and rely on automated assertions only
- Or wrap in `#if DEBUG` to only pause in development

### Success Criteria:

#### Automated Verification:
- [ ] `AIQ.Exports.Tests` builds: `dotnet build AIQ.Exports.Tests/AIQ.Exports.Tests.csproj`
- [ ] Test `CanReplaceTextOnCopiedSlide` runs without exceptions: `dotnet test --filter "FullyQualifiedName~CanReplaceTextOnCopiedSlide"`
- [ ] Test asserts `occurrencesChanged > 0`

#### Manual Verification:
- [ ] Open the presentation URL shown in test output
- [ ] Navigate to the copied slide (ID will be printed)
- [ ] Verify that text "placeholder" was replaced with "TEST_REPLACED"
- [ ] Confirm manual verification to proceed with commit

**Implementation Notes**: 
- This phase is only complete if all automated verification passes
- Pause here for manual confirmation from the human that the manual testing was successful before proceeding to Phase 3
- The `Console.ReadKey()` should be removed or wrapped before final commit

## Testing Strategy

### Integration Tests:
- `CanReplaceTextOnCopiedSlide` - Full flow: copy slide → replace text → verify → delete slide

### Manual Testing Steps:
1. Run `dotnet test --filter "FullyQualifiedName~CanReplaceTextOnCopiedSlide" -v n`
2. Note the copied slide ID from output
3. Open the presentation in browser
4. Find the copied slide and verify text replacement
5. Close the test (press any key) to allow cleanup

## References

- ReplaceTextAction: `AIQ.Exports/GoogleSlides/Actions/ReplaceTextAction.cs`
- TextFilter: `AIQ.Exports/GoogleSlides/Filters/TextFilter.cs`
- Phase 1 plan: `thoughts/2026-05-01-PLAN-Phase1-google-slide-copy-delete-actions.md`
