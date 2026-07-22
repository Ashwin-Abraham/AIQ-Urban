---
name: nunit-tests
description: Best practices for creating, updating, reviewing, and debugging NUnit tests in .NET, including Test, TestCase, TestCaseSource, fixtures, constraint-based assertions, RhinoCommon test data, and Arrange-Act-Preview-Assert. Use whenever you work on NUnit test projects or C# tests using NUnit.Framework.
---

# NUnit Best Practices

Help me write effective unit tests with NUnit, covering both standard and data-driven testing approaches.

## Project Setup

- Use a separate test project with naming convention `[ProjectName].Tests`
- Reference Microsoft.NET.Test.Sdk, NUnit, and NUnit3TestAdapter packages
- Create test classes that match the classes being tested (e.g., `CalculatorTests` for `Calculator`)
- Use .NET SDK test commands: `dotnet test` for running tests

## Test Structure

- Use `[TestFixture]` only when it adds value; NUnit discovers ordinary classes containing tests without it
- Use `[Test]` for simple tests
- Follow the Arrange-Act-Preview-Assert pattern
- Name tests using the pattern `MethodName_Scenario_ExpectedBehavior`
- When multiple cases exercise the same behavior shape, prefer a single test with descriptive `[TestCase]` or `[TestCaseSource]` cases over many repetitive `[Test]` methods.

## Preview

- Preview the result after Act and before Assert
- Use previews for diagnostics and visual inspection, especially geometry
- Guard visualization-only code with `#if DEBUG`
- Ensure previews do not mutate the result or determine whether the test passes
- Keep tests runnable in CI when preview tooling is unavailable

```csharp
// Arrange
var input = CreateInput();

// Act
var result = sut.Execute(input);

// Preview
#if DEBUG
result.ToElements().Visualize();
#endif

// Assert
Assert.That(result, Is.Not.Null);
```

## Standard Tests

- Keep tests focused on a single behavior
- Avoid testing multiple behaviors in one test method
- Use clear assertions that express intent
- Include only the assertions needed to verify the test case
- Make tests independent and idempotent (can run in any order)
- Use focused `[Test]` methods when the scenario is unique enough that sharing data would reduce clarity.

## Data-Driven Tests

- Use `[TestCase]` for inline test data
- Use `[TestCaseSource]` with records or strongly typed case collections for richer test data
- Use `TestCaseData` when a case needs an explicit runner-visible name or additional metadata
- Use meaningful parameter names in data-driven tests
- Keep the runner-visible case name in `[TestCase("<case name>")]` and look up richer test data from records or strongly typed case collections inside the test class.

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

- Use `Assert.That(actual, Is.EqualTo(expected))` for value equality
- Use `Assert.That(actual, Is.SameAs(expected))` for reference equality
- Use `Assert.That(condition, Is.True)` or `Is.False` for boolean conditions
- Use `Assert.That(collection, Does.Contain(item))` or `Does.Not.Contain(item)` for collections
- Use `Assert.That(text, Does.Match(pattern))` or `Does.Not.Match(pattern)` for regex pattern matching
- Use `Assert.Throws<T>` or `Assert.ThrowsAsync<T>` to test exceptions
- Use fluent assertions library for more readable assertions

## Mocking and Isolation

- Consider using Moq or NSubstitute alongside NUnit
- Mock dependencies to isolate units under test
- Use interfaces to facilitate mocking
- Consider using a DI container for complex test setups

## Test Organization

- Group tests by feature or component
- Use `[Category("CategoryName")]` for categorization
- Use `[SetUp]`, `[TearDown]`, `[OneTimeSetUp]`, and `[OneTimeTearDown]` for shared fixture lifecycle
- Use `TestContext` for test diagnostics
- Skip tests with `[Ignore("reason")]` or `Assert.Ignore("reason")`
