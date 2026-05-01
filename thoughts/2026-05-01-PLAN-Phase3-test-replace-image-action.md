# Phase 3: Test ReplaceImageAction

## Overview

Create an integration test for `ReplaceImageAction` that copies slide 1, performs image replacement on the copy, allows manual verification, then deletes the copy.

## Current State Analysis

**ReplaceImageAction** (`AIQ.Exports/GoogleSlides/Actions/ReplaceImageAction.cs:5-51`):
- Takes `ImageFilter` and `imageSource`
- Uses `ReplaceAllShapesWithImageRequest`
- Filters by `ImageElementName` in the `ContainsText` field
- Supports both URL and local file (converts to base64 data URI)
- Returns `OccurrencesChanged` from response

**ImageFilter** (`AIQ.Exports/GoogleSlides/Filters/ImageFilter.cs:2-7`):
- Has `SlideName` (optional, but NOT used in current implementation)
- Has `ImageElementName` (used to identify which image to replace)

**Important Issue Found**: Looking at `ReplaceImageAction.cs:14-21`, the `PageObjectIds` is NOT set, meaning it replaces images across ALL slides. This differs from `ReplaceTextAction` which uses `PageObjectIds = Filter.SlideName != null ? [Filter.SlideName] : null`.

For our test, we need to either:
1. Fix `ReplaceImageAction` to respect `ImageFilter.SlideName` (recommended)
2. Or accept that it will replace images on all slides (not ideal for testing)

**GoogleSlidesService** (`AIQ.Exports/GoogleSlides/GoogleSlidesService.cs:55-57`):
- `ExecuteBatchAsync` already counts `ReplaceAllShapesWithImage.OccurrencesChanged`

## Desired End State

Integration test `CanReplaceImageOnCopiedSlide` that:
1. Copies slide 1
2. Creates a `ReplaceImageAction` targeting the copied slide
3. Executes the action
4. Asserts that `ExecuteAsync` returns expected number of occurrences changed
5. **Pauses for manual verification** - user visually confirms image was replaced
6. Deletes the copied slide
7. Test passes only after manual confirmation

### Key Discoveries:
- `ReplaceAllShapesWithImageRequest` returns `OccurrencesChanged` in the response
- **BUG**: `ReplaceImageAction` doesn't filter by slide - need to fix to use `ImageFilter.SlideName`
- Need a test image file in the test resources

## What We're NOT Doing

- Modifying `ReplaceTextAction` (already tested in Phase 2)
- Testing `SetChartDataAction`
- Adding new action types

## Implementation Approach

1. **Fix `ReplaceImageAction`** to respect `ImageFilter.SlideName`:
   - Add `PageObjectIds` to the `ReplaceAllShapesWithImageRequest`

2. Add a test image to `AIQ.Exports.Tests/ExportTestResources/` (or use existing `TestMasterplan.jpg`)

3. Add integration test method `CanReplaceImageOnCopiedSlide` to `GoogleSlidesIntegrationTests.cs`

4. Test will:
   - Copy slide 1 using `CopySlideAction`
   - Get new slide ID from response
   - Create `ImageFilter` with `SlideName` = new slide ID and `ImageElementName` = some identifier
   - Create `ReplaceImageAction` with test image
   - Execute and assert occurrences changed > 0
   - **PAUSE** - output message for manual verification with slide ID
   - After user confirms, delete the slide using `DeleteSlideAction`

## Phase 3: Test ReplaceImageAction

### Overview
Integration test for `ReplaceImageAction` using copy/verify/delete pattern. Fix slide filtering bug.

### Changes Required:

#### 1. Fix ReplaceImageAction to Filter by Slide
**File**: `AIQ.Exports/GoogleSlides/Actions/ReplaceImageAction.cs`
**Changes**: Add `PageObjectIds` when `Filter.SlideName` is set

```csharp
public Request ToRequest(string presentationId)
{
    var imageUrl = GetImageUrl();
    
    var replaceRequest = new ReplaceAllShapesWithImageRequest
    {
        ContainsText = new SubstringMatchCriteria
        {
            Text = Filter.ImageElementName,
            MatchCase = false
        },
        ImageUrl = imageUrl,
        PageObjectIds = Filter.SlideName != null ? [Filter.SlideName] : null
    };

    return new Request { ReplaceAllShapesWithImage = replaceRequest };
}
```

#### 2. Add Integration Test for ReplaceImageAction
**File**: `AIQ.Exports.Tests/GoogleSlides/GoogleSlidesIntegrationTests.cs`
**Changes**: Add test with manual verification pause

```csharp
[Fact]
public async Task CanReplaceImageOnCopiedSlide()
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
        // Path to test image (use existing or add one)
        var testImagePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "ExportTestResources",
            "TestMasterplan.jpg");
        
        // Create filter targeting only the copied slide
        // ImageElementName should match the "alt text" or name of an image placeholder on the slide
        var filter = new ImageFilter(copiedSlideId, "image_placeholder"); // Adjust based on actual slide content
        
        // Replace image on the copied slide
        doc.AddAction(new ReplaceImageAction(filter, testImagePath));
        var occurrencesChanged = await doc.ExecuteAsync();
        
        // Assert that at least one replacement occurred
        Assert.True(occurrencesChanged > 0);
        
        // PAUSE FOR MANUAL VERIFICATION
        Console.WriteLine($"=== MANUAL VERIFICATION REQUIRED ===");
        Console.WriteLine($"Copied Slide ID: {copiedSlideId}");
        Console.WriteLine($"Please verify in Google Slides that the image was replaced");
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

**Note**: The `ImageElementName` in `ImageFilter` needs to match an actual image placeholder on the test slide. This might be:
- The image's "alt text" in Google Slides
- A text string contained in the image shape
- You may need to inspect the test presentation to find the correct value

### Success Criteria:

#### Automated Verification:
- [ ] `AIQ.Exports` builds: `dotnet build AIQ.Exports/AIQ.Exports.csproj`
- [ ] `AIQ.Exports.Tests` builds: `dotnet build AIQ.Exports.Tests/AIQ.Exports.Tests.csproj`
- [ ] `ReplaceImageAction` now filters by slide when `ImageFilter.SlideName` is set
- [ ] Test `CanReplaceImageOnCopiedSlide` runs without exceptions: `dotnet test --filter "FullyQualifiedName~CanReplaceImageOnCopiedSlide"`
- [ ] Test asserts `occurrencesChanged > 0`

#### Manual Verification:
- [ ] Open the presentation URL shown in test output
- [ ] Navigate to the copied slide (ID will be printed)
- [ ] Verify that an image was replaced with `TestMasterplan.jpg`
- [ ] Confirm manual verification to proceed with commit

**Note**: Manual verification skipped by user request. Automated test passes.

**Implementation Notes**: 
- This phase is only complete if all automated verification passes
- Pause here for manual confirmation from the human that the manual testing was successful
- The `Console.ReadKey()` should be removed or wrapped before final commit
- May need to adjust `ImageElementName` based on actual content of slide 1 in the test presentation

## Testing Strategy

### Integration Tests:
- `CanReplaceImageOnCopiedSlide` - Full flow: copy slide → replace image → verify → delete slide

### Manual Testing Steps:
1. Run `dotnet test --filter "FullyQualifiedName~CanReplaceImageOnCopiedSlide" -v n`
2. Note the copied slide ID from output
3. Open the presentation in browser
4. Find the copied slide and verify image replacement
5. Close the test (press any key) to allow cleanup

## Performance Considerations

- Image encoding to base64 can be slow for large images
- Consider adding file size limit or compression for `ReplaceImageAction`

## References

- ReplaceImageAction: `AIQ.Exports/GoogleSlides/Actions/ReplaceImageAction.cs`
- ImageFilter: `AIQ.Exports/GoogleSlides/Filters/ImageFilter.cs`
- Phase 1 plan: `thoughts/2026-05-01-PLAN-Phase1-google-slide-copy-delete-actions.md`
- Phase 2 plan: `thoughts/2026-05-01-PLAN-Phase2-test-replace-text-action.md`
