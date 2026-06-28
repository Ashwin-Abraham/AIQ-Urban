---
name: xunit-tests
description: 'Get best practices for XUnit unit testing, including data-driven tests'
---

# XUnit Best Practices

Your goal is to help me write effective unit tests with XUnit, covering both standard and data-driven testing approaches.

## Project Setup

- Use a separate test project with naming convention `[ProjectName].Tests`
- Reference Microsoft.NET.Test.Sdk, xunit, and xunit.runner.visualstudio packages
- Create test classes that match the classes being tested (e.g., `CalculatorTests` for `Calculator`)
- Use .NET SDK test commands: `dotnet test` for running tests

## Test Structure

- No test class attributes required (unlike MSTest/NUnit)
- Use fact-based tests with `[Fact]` attribute for simple tests
- Follow the Arrange-Act-Assert (AAA) pattern
- Name tests using the pattern `MethodName_Scenario_ExpectedBehavior`
- When multiple cases exercise the same behavior shape, prefer a single `[Theory]` with descriptive case names over many repetitive `[Fact]` tests.

## Standard Tests

- Keep tests focused on a single behavior
- Avoid testing multiple behaviors in one test method
- Use clear assertions that express intent
- Include only the assertions needed to verify the test case
- Make tests independent and idempotent (can run in any order)
- Use focused `[Fact]` tests when the scenario is unique enough that sharing data would reduce clarity.

## Data-Driven Tests

- Use `[Theory]` combined with data source attributes
- Use `[InlineData]` for inline test data
- Create custom data attributes by implementing `DataAttribute`
- Use meaningful parameter names in data-driven tests
- Keep the runner-visible case name in `[InlineData("<case name>")]` and look up richer test data from records or strongly typed case collections inside the test class.

### Primitive-Only Records for RhinoCommon Tests

When a test constructs RhinoCommon objects (Point3d, Line, etc.), keep the test data record **primitive-only** and construct the Rhino types **inside the test body**. This avoids initialization-order problems with RhinoCommon in static contexts.

**Record — only primitives:**
```csharp
private record IntersectTestCase(
    string CaseName,
    double[][] NodeCoords,
    double[] LineFrom,
    double[] LineTo,
    bool Expected);
```

**Data factory — named arguments, switch expression:**
```csharp
private static IntersectTestCase GetTestData(string caseName)
{
    return caseName switch
    {
        "CrossingEdge" => new IntersectTestCase(
            CaseName: "CrossingEdge",
            NodeCoords: [[5, 5, 0], [5, -5, 0]],
            LineFrom: [3, 0, 0],
            LineTo: [7, 0, 0],
            Expected: true),
        _ => throw new ArgumentException($"Unknown case: {caseName}")
    };
}
```

**Test body — construct Rhino types inline:** The test looks up the primitive record by case name, then constructs `Point3d`, `Line`, `GraphNode` etc. directly from the coordinate arrays before exercising the method under test.

## Assertions

- Use `Assert.Equal` for value equality
- Use `Assert.Same` for reference equality
- Use `Assert.True`/`Assert.False` for boolean conditions
- Use `Assert.Contains`/`Assert.DoesNotContain` for collections
- Use `Assert.Matches`/`Assert.DoesNotMatch` for regex pattern matching
- Use `Assert.Throws<T>` or `await Assert.ThrowsAsync<T>` to test exceptions
- Use fluent assertions library for more readable assertions

## Mocking and Isolation

- Consider using Moq or NSubstitute alongside XUnit
- Mock dependencies to isolate units under test
- Use interfaces to facilitate mocking
- Consider using a DI container for complex test setups

## Test Organization

- Group tests by feature or component
- Use `[Trait("Category", "CategoryName")]` for categorization
- Use collection fixtures to group tests with shared dependencies
- Consider output helpers (`ITestOutputHelper`) for test diagnostics
- Skip tests conditionally with `Skip = "reason"` in fact/theory attributes
