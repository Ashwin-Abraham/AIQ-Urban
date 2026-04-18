# Google Slides Text Editing Implementation Plan

## Overview

Implement a service within AIQ.Exports to find and replace text in an existing Google Slides presentation. The service will target slides by title and/or index, and replace all occurrences of a specified text string.

## Current State Analysis

- AIQ.Exports contains PowerPoint export functionality using ShapeCrawler
- No existing Google Slides integration
- New dependency required: `Google.Apis.Slides.v1`
- Tests exist in AIQ.Exports.Tests using xUnit

## Desired End State

A functional Google Slides editing service that can:
1. Connect to a Google Slides presentation using credentials
2. Find a slide by title and/or index
3. Find text elements containing specific text
4. Replace all occurrences of the target text with new text

### Key Discoveries:
- PowerPoint pattern uses `IShapeEditAction` but this is a separate workflow (per user request)
- Will use Google Slides API directly (Google.Apis.Slides.v1)
- No existing auth setup - placeholder constants will be used

## What We're NOT Doing

- Creating new slides or presentations
- Modifying shapes/images/charts - text only
- Implementing real-time OAuth flow (using hardcoded credentials as requested)
- Any PowerPoint integration (completely separate system)

## Implementation Approach

Create a dedicated Google Slides service class that:
1. Uses a static configuration class for credentials/constants
2. Provides methods to find slides by title/index and replace text
3. Uses Google Slides API's `SearchAndReplaceTextRequest` for the replace operation

## Phase 1: Setup and Core Infrastructure

### Overview
Add Google Slides API dependency and create the core service structure.

### Changes Required:

#### 1. Add NuGet Package
**File**: `AIQ.Exports/AIQ.Exports.csproj`
**Changes**: Add Google Slides API package reference

```xml
<PackageReference Include="Google.Apis.Slides.v1" Version="1.68.0.3644" />
```

#### 2. Create Credentials Configuration
**File**: `AIQ.Exports/GoogleSlides/GoogleSlidesConfig.cs`
**Changes**: Create static class with hardcoded credential placeholders

```csharp
namespace AIQ.Exports.GoogleSlides;
public static class GoogleSlidesConfig
{
    public const string PresentationId = "YOUR_PRESENTATION_ID";
    public const string ServiceAccountKey = "YOUR_SERVICE_ACCOUNT_JSON";
}
```

#### 3. Create Google Slides Service
**File**: `AIQ.Exports/GoogleSlides/GoogleSlidesService.cs`
**Changes**: Create service class with connection logic and text editing methods

```csharp
namespace AIQ.Exports.GoogleSlides;
public class GoogleSlidesService
{
    private readonly SlidesService _service;
    private readonly string _presentationId;

    public GoogleSlidesService(string presentationId);
    public Task<List<Slide>> GetSlidesAsync();
    public Task<Slide?> FindSlideByTitleAsync(string title);
    public Task<Slide?> FindSlideByIndexAsync(int index);
    public Task<int> ReplaceTextAsync(string findText, string replaceText, int? slideIndex = null, string? slideTitle = null);
}
```

### Success Criteria:

#### Automated Verification:
- [x] Package reference added: `dotnet build AIQ.Exports/AIQ.Exports.csproj`
- [x] All unit tests pass: `dotnet test AIQ.Exports.Tests/AIQ.Exports.Tests.csproj`

---

## Phase 2: Implementation Details

### Overview
Implement the core find-and-replace functionality for text in Google Slides.

### Changes Required:

#### 1. Text Replacement Logic
**File**: `AIQ.Exports/GoogleSlides/GoogleSlidesService.cs`
**Changes**: Implement `ReplaceTextAsync` using Google Slides API

Key implementation points:
- Use `SearchAndReplaceTextRequest` for simple text replacement
- Set `matchCase: false` for case-insensitive matching (or make configurable)
- Target specific slide if index/title provided, otherwise all slides
- Return count of replacements made

#### 2. Slide Finding Methods
**File**: `AIQ.Exports/GoogleSlides/GoogleSlidesService.cs`
**Changes**: Implement slide lookup methods

- `FindSlideByTitleAsync`: Query slides, find one matching title
- `FindSlideByIndexAsync`: Get slide by zero-based index
- Both return null if not found

### Success Criteria:

#### Automated Verification:
- [x] Build succeeds: `dotnet build AIQ.Exports/AIQ.Exports.csproj`
- [x] Code compiles without errors

---

## Phase 3: Unit Tests

### Overview
Write unit tests for the Google Slides service in the test project.

### Changes Required:

#### 1. Create Test File
**File**: `AIQ.Exports.Tests/GoogleSlidesServiceTests.cs`
**Changes**: Add unit tests

Test scenarios:
- Finding slide by title (mock the API response)
- Finding slide by index
- Text replacement returns correct count
- Handling slide not found scenarios

### Success Criteria:

#### Automated Verification:
- [x] All tests pass: `dotnet test AIQ.Exports.Tests/AIQ.Exports.Tests.csproj`

---

## Testing Strategy

### Unit Tests:
- Mock Google Slides API responses
- Test slide finding logic
- Test text replacement count return
- Test edge cases (slide not found, no text found)

### Manual Testing Steps:
1. Replace placeholder credentials in `GoogleSlidesConfig` with real values
2. Create a test Google Slides presentation
3. Run the service against it and verify text is replaced

## Performance Considerations

- API calls are network-bound; consider async/await patterns
- Text replacement is a single API call per operation (efficient)

## Migration Notes

No existing data migration required - this is new functionality.

## References

- Google Slides API: https://developers.google.com/slides/api
- Package: Google.Apis.Slides.v1 on NuGet
- Existing pattern reference: `AIQ.Exports/PowerpointExport/ShapeActions/SetShapeTextAction.cs`