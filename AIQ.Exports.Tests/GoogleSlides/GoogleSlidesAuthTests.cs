using AIQ.Exports.GoogleSlides;
using Google.Apis.Auth.OAuth2;
using System.IO;
using Xunit;

namespace AIQ.Exports.Tests.GoogleSlides;

[Collection("GoogleSlidesIntegration")]
public class GoogleSlidesAuthTests
{
    private static bool IsOAuthConfigured()
    {
        return GoogleSlidesConfig.OAuthClientId != "YOUR_CLIENT_ID_HERE" &&
               GoogleSlidesConfig.OAuthClientSecret != "YOUR_CLIENT_SECRET_HERE";
    }

    private static bool IsTokenCached()
    {
        var tokenDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Google.Apis.Auth", "AIQ.Exports.GoogleSlides");
        return Directory.Exists(tokenDir) && Directory.GetFiles(tokenDir).Length > 0;
    }

    [Fact]
    [Trait("Category", "OAuth")]
    public async Task SignInAsync_ReturnsUserCredential_WhenTokenCached()
    {
        if (!IsOAuthConfigured() || !IsTokenCached())
            return;

        var credential = await GoogleSlidesAuth.SignInAsync(CancellationToken.None);

        Assert.NotNull(credential);
        Assert.IsType<UserCredential>(credential);
    }
}
