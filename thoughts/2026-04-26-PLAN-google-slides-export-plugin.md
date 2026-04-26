# Google Slides Export Implementation Plan

## Overview

Create a Google Slides export plugin with filter/action pattern for Grasshopper. Components will allow creating, updating, and batch-executing text/image replacements on Google Slides presentations.

## Current State Analysis

**Existing Code:**
- `AIQ.Exports/GoogleSlides/GoogleSlidesService.cs` (lines 1-108): Basic service with immediate-execution methods (`ReplaceTextAsync`)
- `AIQ.Exports/GoogleSlides/GoogleSlidesConfig.cs`: Static config with hardcoded credentials
- `AIQ.GH.Exports/Components/`: 15+ Grasshopper components following GH_Component pattern

**Missing:**
- Filter-based selection (by slide name, text element)
- Action batching system (deferred execution)
- Image replacement
- Chart data modification (dummy)
- Goo wrappers for document handle

**Key Google Slides API Limitations:**
- `SubstringMatchCriteria`: Only supports `text` + `matchCase` fields. No "Contains" or "DoesNotContain" at API level.
- Text filtering uses `matchCase: true` for exact match, `false` for case-insensitive
- Image replacement via `ReplaceImageRequest` requires object ID
- Charts: Cannot modify data directly - requires Sheets API (skipped for now)

## Desired End State

A complete Google Slides export system with:
1. Ability to authenticate and load a presentation by ID
2. Filter slides by name and text elements
3. Create replace text/image actions
4. Batch execute actions on trigger
5. All components registered in Grasshopper

### Key Discoveries:
- `GoogleSlidesService.cs:53-67` - Uses `ReplaceAllTextRequest` with `SubstringMatchCriteria`
- `AIQ.GH.Exports/Enums/TextMatchStrategy.cs` - Existing enum can be reused (Contains, DoesNotContain, ExactMatch) but mapped to MatchCase flags
- Action pattern: `IShapeReplaceAction.cs` in PowerPoint export shows filter/predicate design to follow

## What We're NOT Doing

- Google Sheets API integration for chart data (skipped, placeholder only)
- Real chart data modification (dummy implementation)
- Direct execution methods in new components (batch only)
- Creating separate project (adding to existing AIQ.GH.Exports)

## Implementation Approach

Use a document-centric batch pattern:
1. `GoogleSlidesDocument` - Holds presentation ID + auth + action queue
2. Filter objects select which elements to target
3. Action objects represent changes to apply
4. BatchExecute commits all pending actions

## Phase 1: Core Data Models (AIQ.Exports)

### Overview
Create the filter/action classes and document handle in AIQ.Exports.GoogleSlides.

### Changes Required:

#### 1. GoogleSlidesDocument.cs
**File**: `AIQ.Exports/GoogleSlides/GoogleSlidesDocument.cs`
**Changes**: New class to hold presentation state + action queue

```csharp
public class GoogleSlidesDocument(
    string presentationId,
    string serviceAccountJson)
{
    public string PresentationId { get; } = presentationId;
    public string ServiceAccountJson { get; } = serviceAccountJson;
    public List<IGoogleSlidesAction> PendingActions { get; } = [];
    
    public async Task ExecuteAsync() { /* batch execution */ }
    public void ClearActions() { /* clear queue */ }
}
```

#### 2. IGoogleSlidesAction interface
**File**: `AIQ.Exports/GoogleSlides/IGoogleSlidesAction.cs`
**Changes**: New interface for actions

```csharp
public interface IGoogleSlidesAction
{
    Request ToRequest(string presentationId);
}
```

#### 3. TextFilter class
**File**: `AIQ.Exports/GoogleSlides/TextFilter.cs`
**Changes**: Filter criteria for text elements

```csharp
public enum TextMatchCase
{
    MatchCase,
    DontMatchCase
}

public class TextFilter(
    string? slideName,
    string textElementName,
    TextMatchCase matchCase)
{
    public string? SlideName { get; } = slideName;
    public string TextElementName { get; } = textElementName;
    public TextMatchCase MatchCase { get; } = matchCase;
}
```

#### 4. ReplaceTextAction class
**File**: `AIQ.Exports/GoogleSlides/ReplaceTextAction.cs`
**Changes**: Action to replace text

```csharp
public class ReplaceTextAction(TextFilter filter, string replacementText) : IGoogleSlidesAction
{
    public TextFilter Filter { get; } = filter;
    public string ReplacementText { get; } = replacementText;
    
    public Request ToRequest(string presentationId) { /* convert to API request */ }
}
```

#### 5. ImageFilter class
**File**: `AIQ.Exports/GoogleSlides/ImageFilter.cs`
**Changes**: Filter for image elements

```csharp
public class ImageFilter(
    string? slideName,
    string imageElementName)
{
    // Similar to TextFilter but for images
}
```

#### 6. ReplaceImageAction class
**File**: `AIQ.Exports/GoogleSlides/ReplaceImageAction.cs`
**Changes**: Action to replace image

```csharp
public class ReplaceImageAction(ImageFilter filter, string imageSource) : IGoogleSlidesAction
{
    // imageSource can be URL or local file path
}
```

#### 7. SetChartDataAction (dummy)
**File**: `AIQ.Exports/GoogleSlides/SetChartDataAction.cs`
**Changes**: Placeholder implementation

```csharp
public class SetChartDataAction(TextFilter filter, List<string> seriesData) : IGoogleSlidesAction
{
    // Logs placeholder, returns dummy request
}
```

#### 8. Update GoogleSlidesService
**File**: `AIQ.Exports/GoogleSlides/GoogleSlidesService.cs`
**Changes**: Add batch execution method

```csharp
public async Task<int> ExecuteBatchAsync(
    GoogleSlidesDocument document,
    IList<IGoogleSlidesAction> actions)
{
    // Convert actions to batch requests and execute
}
```

### Success Criteria:

#### Automated Verification:
- [x] Solution builds: `dotnet build AIQ.Exports/AIQ.Exports.csproj`
- [x] Unit tests pass: `dotnet test AIQ.Exports.Tests/`

---

## Phase 2: Grasshopper Components (AIQ.GH.Exports)

### Overview
Create 6 Grasshopper components that wrap the core models.

### Changes Required:

#### 1. GoogleSlidesDocComponent
**File**: `AIQ.GH.Exports/Components/Google Slides/GoogleSlidesDocComponent.cs`
**Changes**: Creates document from ID + auth

```csharp
public class GoogleSlidesDocComponent : GH_Component
{
    // Input: Presentation ID (text), Service Account JSON (text)
    // Output: GoogleSlidesDocumentGoo
}
```

#### 2. TextFilterComponent
**File**: `AIQ.GH.Exports/Components/Google Slides/TextFilterComponent.cs`
**Changes**: Creates text filter

```csharp
public class TextFilterComponent : GH_Component
{
    // Input: Slide Name (text, optional), Text Element Name (text), Match Case (bool)
    // Output: TextFilterGoo
}
```

#### 3. ReplaceTextActionComponent
**File**: `AIQ.GH.Exports/Components/Google Slides/ReplaceTextActionComponent.cs`
**Changes**: Creates text replacement action

```csharp
public class ReplaceTextActionComponent : GH_Component
{
    // Input: TextFilterGoo, Replacement Text (text)
    // Output: ReplaceTextActionGoo
}
```

#### 4. ReplaceImageActionComponent
**File**: `AIQ.GH.Exports/Components/Google Slides/ReplaceImageActionComponent.cs`
**Changes**: Creates image replacement action

```csharp
public class ReplaceImageActionComponent : GH_Component
{
    // Input: ImageFilterGoo, Image Source (text - URL or path)
    // Output: ReplaceImageActionGoo
}
```

#### 5. SetChartDataComponent
**File**: `AIQ.GH.Exports/Components/Google Slides/SetChartDataComponent.cs`
**Changes**: Creates dummy chart action

```csharp
public class SetChartDataComponent : GH_Component
{
    // Input: TextFilterGoo, Series Data (list of text)
    // Output: SetChartDataActionGoo
}
```

#### 6. BatchExecuteComponent
**File**: `AIQ.GH.Exports/Components/Google Slides/BatchExecuteComponent.cs`
**Changes**: Executes batch actions

```csharp
public class BatchExecuteComponent : GH_Component
{
    // Input: GoogleSlidesDocumentGoo, List<IGoogleSlidesActionGoo>, Execute (bool)
    // Output: Success (bool), Modified Count (int)
}
```

#### 7. ImageFilterComponent
**File**: `AIQ.GH.Exports/Components/Google Slides/ImageFilterComponent.cs`
**Changes**: Creates image filter

```csharp
public class ImageFilterComponent : GH_Component
{
    // Input: Slide Name (text, optional), Image Element Name (text)
    // Output: ImageFilterGoo
}
```

#### 8. AddActionComponent
**File**: `AIQ.GH.Exports/Components/Google Slides/AddActionComponent.cs`
**Changes**: Adds action to document's pending queue

```csharp
public class AddActionComponent : GH_Component
{
    // Input: GoogleSlidesDocumentGoo, IGoogleSlidesActionGoo
    // Output: GoogleSlidesDocumentGoo (updated)
}
```

### Success Criteria:

#### Automated Verification:
- [ ] Solution builds: `dotnet build AIQ.GH.Exports/AIQ.GH.Exports.csproj`
- [ ] All components compile without errors
- [ ] No regressions in existing components

---

## Phase 3: Goo Wrappers and Type Converters

### Overview
Create Goo wrappers for Grasshopper type registration.

### Changes Required:

#### 1. Create Goo types
**Files**: 
- `AIQ.GH.Exports/Types/GoogleSlidesDocumentGoo.cs`
- `AIQ.GH.Exports/Types/TextFilterGoo.cs`
- `AIQ.GH.Exports/Types/ReplaceTextActionGoo.cs`
- `AIQ.GH.Exports/Types/ImageFilterGoo.cs`
- `AIQ.GH.Exports/Types/ReplaceImageActionGoo.cs`
- `AIQ.GH.Exports/Types/SetChartDataActionGoo.cs`

Each wraps the corresponding core class + provides Marshal/Unmarshal for GH serialization.

### Success Criteria:

#### Automated Verification:
- [ ] All Goo types can be serialized/deserialized
- [ ] Component registration works in Grasshopper

---

## Testing Strategy

### Unit Tests:
- TextFilter creation with various inputs
- ReplaceTextAction conversion to API request
- ReplaceImageAction with URL vs file path
- Batch execution with multiple actions

### Integration Tests:
- Full flow: Create doc → Create filter → Create action → Execute

### Manual Testing Steps:
1. Create a test presentation in Google Slides
2. Add text boxes with known names
3. Run component chain with Execute=true
4. Verify text was replaced
5. Test with Execute=false (verify no change)

---

## Performance Considerations

- Batch execution is atomic - all succeed or all fail
- Action queue held in memory - clear between runs
- Network latency is the bottleneck for batch operations

---

## References

- Existing GoogleSlidesService: `AIQ.Exports/GoogleSlides/GoogleSlidesService.cs:1-108`
- Action pattern from PowerPoint: `AIQ.Exports/PowerpointExport/ShapeActions/IShapeReplaceAction.cs`
- TextMatchStrategy: `AIQ.GH.Exports/Enums/TextMatchStrategy.cs`