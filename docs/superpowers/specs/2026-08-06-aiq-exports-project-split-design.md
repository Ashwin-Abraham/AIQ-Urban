# AIQ.Exports Project Split Design

## Purpose

Split the export code so the Rhino plugin release build does not receive packages for future export features.

The current dependency path is:

`AIQ.Urban.RhinoPlugin -> AIQ.Urban.RhinoInterface -> AIQ.Exports`

`AIQ.Urban.RhinoInterface` uses only the Excel export feature. The current `AIQ.Exports` project also contains Google Slides and PowerPoint code. Those features add packages that the plugin does not use.

## Project Boundary

`AIQ.Exports` will contain only the `Excel` source folder and the Excel template resource. It will reference:

- `ClosedXML` version `0.105.0`
- `System.Drawing.Common` version `8.0.0`

`AIQ.Exports.Future` will contain the `GoogleSlides` and `PowerpointExport` source folders. It will also contain the PowerPoint template and image resources. It will reference:

- `Google.Apis.Slides.v1` version `1.69.0.3701`
- `Google.Apis.Drive.v3` version `1.69.0.3790`
- `ShapeCrawler` version `0.75.2`
- `System.Drawing.Common` version `8.0.0`

The future project does not reference `AIQ.Exports`. The current source inspection shows no dependency between the future export features and the Excel feature.

## API Compatibility

The moved classes will keep their current `AIQ.Exports.*` namespaces. This change moves types to a different assembly, but it does not rename their namespaces or public members.

Current Excel consumers will continue to reference `AIQ.Exports`. A future-feature consumer must add an explicit reference to `AIQ.Exports.Future`.

## Tests

`AIQ.Exports.Tests` will keep the Excel tests and will reference `AIQ.Exports`.

A new `AIQ.Exports.Future.Tests` project will contain the Google Slides tests and their test image. It will reference `AIQ.Exports.Future`.

Both test projects will use xUnit. The existing unused `Moq` package will be removed if no moved test requires it.

There are no current PowerPoint tests to move.

## Solution and Build Behavior

The solution will contain these four export projects under the existing solution folders:

- `src/AIQ.Exports/AIQ.Exports.csproj`
- `src/AIQ.Exports.Future/AIQ.Exports.Future.csproj`
- `tests/AIQ.Exports.Tests/AIQ.Exports.Tests.csproj`
- `tests/AIQ.Exports.Future.Tests/AIQ.Exports.Future.Tests.csproj`

`AIQ.Urban.RhinoInterface` will continue to reference only `AIQ.Exports`. As a result, the Rhino plugin dependency graph will not include `AIQ.Exports.Future` or its Google and PowerPoint packages.

## Resource Placement

Resources will move with their related feature:

- `TemplateExcel.xlsx` stays in `AIQ.Exports`.
- `AIQUrbanTemplate.pptx` and `LEGO.png` move to `AIQ.Exports.Future`.
- `TestMasterplan.jpg` moves to `AIQ.Exports.Future.Tests` with the Google Slides tests.

The current project files do not assign special build actions to these resources. The split will preserve that behavior.

## Error Handling and Migration

This change does not alter runtime behavior or error handling. It changes only project ownership, package references, test ownership, and solution entries.

The existing uncommitted Rhino plugin version change from `1.0` to `3.0.0` is outside this work and must remain unchanged.

## Verification

The implementation is complete when:

1. `AIQ.Exports.Tests` passes.
2. `AIQ.Exports.Future.Tests` passes.
3. `AIQ.Exports` has no Google API or ShapeCrawler dependency.
4. `AIQ.Urban.RhinoPlugin` builds in `Release` configuration.
5. The Rhino plugin dependency assets do not include `AIQ.Exports.Future`, Google API packages, or ShapeCrawler through the export project path.
6. The full solution builds, subject to existing external UI and package restore requirements.
7. `graphify update .` completes after the code and project changes.
