# Debug Preview Guards Design

## Goal

Keep test preview code available in Debug builds and exclude it from Release builds.

## Scope

Review C# files under `tests/`. Add `#if DEBUG` and `#endif` around preview-only code that uses preview or debug-visualization APIs, including `PreviewHub`, `PreviewViaHub`, `AddToPreviewDoc`, `DEBUGMODEL`, `OpenAsGlb`, and test visualization helpers.

Do not change production code. Preserve existing working-tree changes.

## Guard Boundaries

Each guard must include the complete preview operation. This includes temporary values and geometry transformations that exist only to prepare the preview. A guard must not include Arrange, Act, or Assert code.

Preview code must not change values that an assertion reads. If preview preparation currently changes an asserted value, use a preview-only copy or keep the test behavior unchanged and report the case for review.

Existing correct `#if DEBUG` blocks remain unchanged.

## Verification

Build the solution in Release configuration to confirm that test projects do not depend on preview APIs. Run focused tests for changed projects when the environment supports them. Run `graphify update .` after the source changes.

The cleanup is complete when unguarded preview-only calls are absent from test code and Release compilation has no preview-related errors.
