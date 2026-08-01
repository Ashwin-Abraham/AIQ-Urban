# ADR 3: Load the Rhino plugin as a DLL in its test project

## Status
Accepted

## Context
`AIQ.Urban.RhinoInterface.Tests` tests both the Rhino interface library and types implemented in `AIQ.Urban.RhinoPlugin`. The plugin project produces `AIQ.Urban.RhinoPlugin.rhp`, which is a managed .NET assembly with Rhino's plugin extension.

When the plugin project reference copied the `.rhp` into the test output as a runtime asset, `testhost` aborted before NUnit discovery:

```text
Failed to create CoreCLR, HRESULT: 0x80070057
Test Run Aborted.
```

Because the failure occurred while the test runtime was starting, filtering to a test that did not use the plugin did not help. It also made healthy tests appear not to run.

The failure was isolated by comparing clean test outputs:

- Including the plugin `.rhp` as a runtime asset reproduced the CoreCLR abort.
- Excluding the `.rhp` allowed NUnit discovery and execution to start.
- Supplying the same managed assembly under the `.dll` extension allowed tests that reference plugin types to run.

Static RhinoCommon geometry in some fixtures remains an initialization-order risk, but it was not the cause of this abort: those fixtures ran after the `.rhp` runtime issue was removed.

## Decision
The test project will compile against `AIQ.Urban.RhinoPlugin`, but it will not copy the plugin project's `.rhp` output into the test runtime dependency graph.

After the target-framework build, the test project will:

1. Delete any stale `AIQ.Urban.RhinoPlugin.rhp` from its output directory.
2. Copy the referenced plugin assembly into the test output as `AIQ.Urban.RhinoPlugin.dll`.

The copied `.dll` is byte-for-byte identical to the `.rhp`; only the filename used by the test runtime differs. Rhino's distributable plugin output remains `.rhp`.

## Considered alternatives
- **Exclude the plugin assembly entirely:** testhost starts, but tests that directly use plugin types fail with `FileNotFoundException`.
- **Keep the `.rhp` and filter problematic fixtures:** ineffective because CoreCLR aborts before NUnit can apply filters or execute test bodies.
- **Change the plugin project's output extension to `.dll`:** would make tests simpler but break the Rhino plugin packaging contract.
- **Move or rewrite all plugin-facing tests:** possible longer-term restructuring, but unnecessary to resolve the runtime-host failure.

## Consequences
- The test project can discover and execute all of its NUnit tests without aborting CoreCLR.
- Plugin-facing tests resolve a normal `.dll`, while Rhino builds continue to produce the required `.rhp`.
- The test project owns a small MSBuild target that depends on resolved project-reference paths.
- Stale `.rhp` files are actively removed, so an incremental build cannot silently reintroduce the failure.
- Ordinary assertion or behavioral failures are reported individually instead of aborting the whole test assembly.

## Verification
After applying this decision, `dotnet test tests/AIQ.Urban.RhinoPlugin.Tests/AIQ.Urban.RhinoInterface.Tests.csproj` discovered and executed all 86 tests. The run completed with 82 passing tests and four ordinary test failures; there was no CoreCLR failure or aborted run.
