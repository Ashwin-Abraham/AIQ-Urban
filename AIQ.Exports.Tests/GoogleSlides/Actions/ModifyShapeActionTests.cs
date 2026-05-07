using System.Collections.Generic;
using System.Drawing;
using AIQ.Exports.GoogleSlides;
using AIQ.Exports.GoogleSlides.Actions;
using AIQ.Exports.GoogleSlides.Utils;
using Google.Apis.Slides.v1.Data;
using Xunit;

namespace AIQ.Exports.Tests.GoogleSlides.Actions;

public class ModifyShapeActionUnitTests
{
    private static Presentation CreateSingleShapePresentation(string shapeId, string shapeText, int slideCount = 1, int targetSlide = 1)
    {
        var slides = new List<Page>();
        for (int i = 1; i <= slideCount; i++)
        {
            slides.Add(new Page
            {
                PageElements = new List<PageElement>
                {
                    new PageElement
                    {
                        ObjectId = i == targetSlide ? shapeId : $"other{i}",
                        Shape = new Shape
                        {
                            Text = new TextContent
                            {
                                TextElements = new List<TextElement>
                                {
                                    new TextElement { TextRun = new TextRun { Content = shapeText } }
                                }
                            }
                        }
                    }
                }
            });
        }
        return new Presentation { Slides = slides };
    }

    [Theory]
    [InlineData("", false, null, null, 0, 0, 0, "ReturnsEmpty_WhenFilterTextIsEmpty")]
    [InlineData("test", false, null, null, 0, 0, 0, "ReturnsEmpty_WhenNoVisualChanges")]
    public void ToRequest_ReturnsExpectedRequests(
        string filterText, bool deleteText, string fillColorName, int? slideNumber,
        int expectedRequestCount, int slideCount, int targetSlide, string scenario)
    {
        // Arrange
        Color? fillColor = fillColorName != null ? Color.FromName(fillColorName) : null;
        var shapeId = "shape123";
        var presentation = string.IsNullOrEmpty(filterText)
            ? new Presentation()
            : CreateSingleShapePresentation(shapeId, "TestShape", slideCount, targetSlide);

        var action = new ModifyShapeAction(
            filterText: filterText,
            deleteText: deleteText,
            fillColor: fillColor,
            slideNumber: slideNumber);

        // Act
        var requests = action.ToRequest(presentation).ToList();

        // Assert
        Assert.Equal(expectedRequestCount, requests.Count);
    }

    [Theory]
    [InlineData("TestShape", true, null, 0, "shape123", true, "GeneratesDeleteTextRequest")]
    [InlineData("TestShape", false, "Blue", 0, "shape123", false, "GeneratesUpdateShapeRequest_WhenFillColorIsSet")]
    public void ToRequest_GeneratesExpectedRequest(
        string filterText, bool deleteText, string fillColorName, int slideNumber,
        string expectedShapeId, bool expectDeleteText, string scenario)
    {
        // Arrange
        var shapeId = "shape123";
        var presentation = CreateSingleShapePresentation(shapeId, filterText);
        Color? fillColor = fillColorName != null ? Color.FromName(fillColorName) : null;

        var action = new ModifyShapeAction(
            filterText: filterText,
            deleteText: deleteText,
            fillColor: fillColor,
            slideNumber: slideNumber > 0 ? slideNumber : null);

        // Act
        var requests = action.ToRequest(presentation).ToList();

        // Assert
        Assert.Single(requests);
        if (expectDeleteText)
        {
            Assert.NotNull(requests[0].DeleteText);
            Assert.Equal(expectedShapeId, requests[0].DeleteText.ObjectId);
        }
        else
        {
            var updateRequest = requests[0].UpdateShapeProperties;
            Assert.NotNull(updateRequest);
            Assert.Equal(expectedShapeId, updateRequest.ObjectId);
        }
    }

    [Theory]
    [InlineData(2, "shape2", "TargetsSpecificSlide_WhenSlideNumberIsSet")]
    public void ToRequest_TargetsCorrectSlide(
        int slideNumber, string expectedShapeId, string scenario)
    {
        // Arrange
        var shape1Id = "shape1";
        var shape2Id = "shape2";
        var presentation = new Presentation
        {
            Slides = new List<Page>
            {
                new Page // Slide 1
                {
                    PageElements = new List<PageElement>
                    {
                        new PageElement
                        {
                            ObjectId = shape1Id,
                            Shape = new Shape
                            {
                                Text = new TextContent
                                {
                                    TextElements = new List<TextElement>
                                    {
                                        new TextElement { TextRun = new TextRun { Content = "TestShape" } }
                                    }
                                }
                            }
                        }
                    }
                },
                new Page // Slide 2
                {
                    PageElements = new List<PageElement>
                    {
                        new PageElement
                        {
                            ObjectId = shape2Id,
                            Shape = new Shape
                            {
                                Text = new TextContent
                                {
                                    TextElements = new List<TextElement>
                                    {
                                        new TextElement { TextRun = new TextRun { Content = "TestShape" } }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
        var action = new ModifyShapeAction(filterText: "TestShape", deleteText: true, slideNumber: slideNumber);

        // Act
        var requests = action.ToRequest(presentation).ToList();

        // Assert
        Assert.Single(requests);
        Assert.Equal(expectedShapeId, requests[0].DeleteText?.ObjectId);
    }

    [Theory]
    [InlineData("hello", "Hello World", TextMatchOption.DontMatchCase, 1, "TextMatches_CaseInsensitive")]
    [InlineData("Hello", "Hello World", TextMatchOption.MatchCase, 1, "TextMatches_CaseSensitive")]
    [InlineData("hello", "Hello World", TextMatchOption.MatchCase, 0, "TextDoesNotMatch_WhenCaseSensitive")]
    public void TextMatches_AccordingToMatchOption(
        string filterText, string shapeText, TextMatchOption matchOption, int expectedCount, string scenario)
    {
        // Arrange
        var shape = new Shape
        {
            Text = new TextContent
            {
                TextElements = new List<TextElement>
                {
                    new TextElement { TextRun = new TextRun { Content = shapeText } }
                }
            }
        };
        var action = new ModifyShapeAction(filterText: filterText, matchOption: matchOption);

        // Act
        var presentation = new Presentation
        {
            Slides = new List<Page>
            {
                new Page { PageElements = new List<PageElement> { new PageElement { Shape = shape } } }
            }
        };
        var requests = action.ToRequest(presentation).ToList();

        // Assert
        Assert.Equal(expectedCount, requests.Count);
    }
}

[Collection("GoogleSlidesIntegration")]
public class ModifyShapeActionIntegrationTests
{
    [Fact]
    public async Task CanModifyShapeFillColorOnCopiedSlide()
    {
        var credential = ActionTestPipeline.CreateCredentialFromServiceAccount(GoogleSlidesConfig.ServiceAccountPath);
        var testPipeline = ActionTestPipeline
            .Create(GoogleSlidesConfig.PresentationId, credential)
            .WithTest(async (doc, copiedSlideNumber) =>
            {
                // Arrange
                var action = new ModifyShapeAction(
                    filterText: "placeholder",
                    deleteText: false,
                    fillColor: Color.Red,
                    slideNumber: copiedSlideNumber);

                // Act
                doc.AddAction(action);
                var occurrencesChanged = await doc.ExecuteAsync();

                // Assert
                Assert.True(occurrencesChanged > 0);
            });

        await testPipeline.Run();
    }

    [Fact]
    public async Task CanModifyShapeOutlineOnCopiedSlide()
    {
        await ActionTestPipeline
            .Create(GoogleSlidesConfig.PresentationId,
                ActionTestPipeline.CreateCredentialFromServiceAccount(GoogleSlidesConfig.ServiceAccountPath))
            .WithTest(async (doc, copiedSlideNumber) =>
            {
                // Arrange
                var action = new ModifyShapeAction(
                    filterText: "placeholder",
                    deleteText: false,
                    outlineColor: Color.Blue,
                    outlineWeight: 2,
                    slideNumber: copiedSlideNumber);

                // Act
                doc.AddAction(action);
                var occurrencesChanged = await doc.ExecuteAsync();

                // Assert
                Assert.True(occurrencesChanged > 0);
            })
            .Run();
    }

    [Fact]
    public async Task CanDeleteTextFromShape()
    {
        await ActionTestPipeline
            .Create(GoogleSlidesConfig.PresentationId,
                ActionTestPipeline.CreateCredentialFromServiceAccount(GoogleSlidesConfig.ServiceAccountPath))
            .WithTest(async (doc, copiedSlideNumber) =>
            {
                // Arrange
                var action = new ModifyShapeAction(
                    filterText: "placeholder",
                    deleteText: true,
                    slideNumber: copiedSlideNumber);

                // Act
                doc.AddAction(action);
                var occurrencesChanged = await doc.ExecuteAsync();

                // Assert
                Assert.True(occurrencesChanged > 0);
            })
            .Run();
    }
}
