# Google Slide Actions Testing Implementation Plan

## Overview

Test Google Slide actions (ReplaceText, ReplaceImage) through integration tests that create a copy of a slide, run the action, manually verify the result, then delete the copy. This is phased to allow incremental implementation with manual verification and commit after each phase.

## Current State Analysis

**Existing Action Types:**
- `ReplaceTextAction` (`AIQ.Exports/GoogleSlides/Actions/ReplaceTextAction.cs`) - Implemented using `ReplaceAllTextRequest`
- `ReplaceImageAction` (`AIQ.Exports/GoogleSlides/Actions/ReplaceImageAction.cs`) - Implemented using `ReplaceAllShapesWithImageRequest`
- `SetChartDataAction` (`AIQ.Exports/GoogleSlides/Actions/SetChartDataAction.cs`) - **Not implemented** (returns empty request) - OUT OF SCOPE

**Existing Test Infrastructure:**
- `GoogleSlidesIntegrationTests.cs` - Has `CanGetSlide()` test validating connection
- `GoogleSlidesConfig.cs` - Contains `PresentationId` and `ServiceAccountPath` for the test presentation
- Test presentation ID: `1ybxIuFIfwUtJyaHhPeZXje04y9wW01TC2gGFcgEqeBk`

**Missing:**
- Copy Slide action
- Delete Slide action
- Integration tests for ReplaceTextAction
- Integration tests for ReplaceImageAction

## What We're NOT Doing

- Implementing or testing `SetChartDataAction`
- Adding other Google Slides action types (CreateShape, DeleteObject, UpdateTextStyle, etc.)
- Modifying the PowerPoint export functionality
- Changing the existing `IGoogleSlidesAction` interface

## Implementation Phases

| Phase | Description | Document |
|-------|-------------|----------|
| 1 | Copy Slide and Delete Slide Actions | [2026-05-01-PLAN-Phase1-google-slide-copy-delete-actions.md](./2026-05-01-PLAN-Phase1-google-slide-copy-delete-actions.md) |
| 2 | Test ReplaceTextAction | [2026-05-01-PLAN-Phase2-test-replace-text-action.md](./2026-05-01-PLAN-Phase2-test-replace-text-action.md) |
| 3 | Test ReplaceImageAction | [2026-05-01-PLAN-Phase3-test-replace-image-action.md](./2026-05-01-PLAN-Phase3-test-replace-image-action.md) |

## Testing Strategy Per Phase

Each action test phase (2 and 3) will follow this pattern:
1. Copy slide 1 of the test presentation
2. Run the action on the copied slide
3. Validate success via assertion (check occurrences changed)
4. **Pause for manual verification** - user verifies the slide visually
5. Delete the copied slide
6. Commit once user confirms manual verification passed

## References

- Google Slides API: https://developers.google.com/slides/api/reference/rest/v1/presentations/request
- Existing actions: `AIQ.Exports/GoogleSlides/Actions/`
- Test config: `AIQ.Exports/GoogleSlides/GoogleSlidesConfig.cs`
- Existing integration tests: `AIQ.Exports.Tests/GoogleSlides/GoogleSlidesIntegrationTests.cs`
