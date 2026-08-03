# Rhino UI Minimum Window Size Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prevent the AIQ Urban Rhino UI window from being resized below 312 by 630 pixels while preserving its 520-by-900-pixel default size and unrestricted enlargement.

**Architecture:** Keep the sizing policy in `UrbanHtmlUi`, where the Eto `Form` is constructed. Add two named integer constants and use them to initialize the form's built-in `MinimumSize`; do not constrain the embedded `WebView` separately.

**Tech Stack:** .NET 8, C# 12, Eto.Forms, Rhino 8 plugin

## Global Constraints

- Default window dimensions remain 520 pixels wide by 900 pixels high.
- Minimum window dimensions are stored as constants with values 312 pixels wide and 630 pixels high.
- Set the minimum on the top-level Eto `Form` only.
- Do not set a maximum window size.
- Do not add automated tests; verify with the Rhino plugin build and final diff inspection.

---

### Task 1: Enforce the Rhino UI minimum window size

**Files:**
- Modify: `src/AIQ.Urban.Rhino/HTML_UI/UrbanHtmlUi.cs:9-35`

**Interfaces:**
- Consumes: `Eto.Drawing.Size` and the existing `UrbanHtmlUi.OpenWindow(int httpPort, Action onClosed)` window construction.
- Produces: a `Form` whose `MinimumSize` is 312 by 630 pixels and whose initial `Width` and `Height` remain 520 by 900 pixels.

- [ ] **Step 1: Add named minimum-dimension constants**

Add these constants beside the existing default-dimension constants:

```csharp
private const int MinimumWindowWidth = 312;
private const int MinimumWindowHeight = 630;
```

- [ ] **Step 2: Apply the minimum size to the form**

Add this initializer to the `Form` construction without changing `Width`, `Height`, or the `WebView`:

```csharp
MinimumSize = new Size(MinimumWindowWidth, MinimumWindowHeight),
```

- [ ] **Step 3: Build the Rhino plugin**

Run:

```powershell
dotnet build src/AIQ.Urban.Rhino/AIQ.Urban.RhinoPlugin.csproj --no-restore
```

Expected: build exits with code 0 and reports no errors.

- [ ] **Step 4: Inspect the final diff**

Run:

```powershell
git diff --check
git diff -- src/AIQ.Urban.Rhino/HTML_UI/UrbanHtmlUi.cs
```

Expected: the diff contains only the two minimum-size constants and the `MinimumSize` form initializer; the 520-by-900 defaults remain unchanged and no maximum-size property appears.

- [ ] **Step 5: Commit the implementation**

```powershell
git add -- src/AIQ.Urban.Rhino/HTML_UI/UrbanHtmlUi.cs
git commit -m "Set Rhino UI minimum window size"
```
