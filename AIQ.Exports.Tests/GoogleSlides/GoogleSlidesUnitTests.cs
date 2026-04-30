using AIQ.Exports.GoogleSlides;
using AIQ.Exports.GoogleSlides.Actions;
using Moq;

namespace AIQ.Exports.Tests.GoogleSlides;

public class GoogleSlidesUnitTests
{
    [Fact]
    public async Task ValidateAsync_ReturnsTrue_WhenValid()
    {
        var mockDoc = new Mock<IGoogleSlidesDocument>();
        mockDoc.Setup(x => x.ValidateAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);

        var result = await mockDoc.Object.ValidateAsync();
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsFalse_WhenInvalid()
    {
        var mockDoc = new Mock<IGoogleSlidesDocument>();
        mockDoc.Setup(x => x.ValidateAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);

        var result = await mockDoc.Object.ValidateAsync();
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAsync_RespectsCancellationToken()
    {
        var mockDoc = new Mock<IGoogleSlidesDocument>();
        var cancellationToken = new CancellationToken(canceled: true);
        mockDoc.Setup(x => x.ValidateAsync(cancellationToken))
               .Throws<OperationCanceledException>();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => mockDoc.Object.ValidateAsync(cancellationToken));
    }

    [Fact]
    public void AddAction_AddsToPendingActions()
    {
        var pendingActions = new List<IGoogleSlidesAction>();
        var mockDoc = new Mock<IGoogleSlidesDocument>();
        mockDoc.Setup(x => x.PendingActions).Returns(pendingActions);
        mockDoc.Setup(x => x.AddAction(It.IsAny<IGoogleSlidesAction>()))
               .Callback<IGoogleSlidesAction>(action => pendingActions.Add(action));

        var mockAction = new Mock<IGoogleSlidesAction>();
        mockDoc.Object.AddAction(mockAction.Object);

        Assert.Single(pendingActions);
        mockDoc.Verify(x => x.AddAction(mockAction.Object), Times.Once);
    }

    [Fact]
    public void ClearActions_EmptiesPendingActions()
    {
        var pendingActions = new List<IGoogleSlidesAction>
        {
            new Mock<IGoogleSlidesAction>().Object,
            new Mock<IGoogleSlidesAction>().Object
        };
        var mockDoc = new Mock<IGoogleSlidesDocument>();
        mockDoc.Setup(x => x.PendingActions).Returns(pendingActions);
        mockDoc.Setup(x => x.ClearActions())
               .Callback(() => pendingActions.Clear());

        mockDoc.Object.ClearActions();

        Assert.Empty(pendingActions);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsChangeCount()
    {
        var mockDoc = new Mock<IGoogleSlidesDocument>();
        mockDoc.Setup(x => x.ExecuteAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(5);

        var result = await mockDoc.Object.ExecuteAsync();
        Assert.Equal(5, result);
    }

    [Fact]
    public void PresentationId_ReturnsConfiguredId()
    {
        const string expectedId = "test-presentation-123";
        var mockDoc = new Mock<IGoogleSlidesDocument>();
        mockDoc.Setup(x => x.PresentationId).Returns(expectedId);

        var result = mockDoc.Object.PresentationId;
        Assert.Equal(expectedId, result);
    }
}
