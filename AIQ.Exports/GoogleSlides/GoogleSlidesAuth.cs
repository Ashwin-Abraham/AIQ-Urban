using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using static AIQ.Exports.GoogleSlides.GoogleSlidesConstants;

namespace AIQ.Exports.GoogleSlides;

public static class GoogleSlidesAuth
{
    public static FileDataStore DataStore => new FileDataStore("AIQ.Exports.GoogleSlides");

    public static async Task<UserCredential> SignInAsync(CancellationToken cancellationToken = default)
    {
        var clientSecrets = new ClientSecrets
        {
            ClientId = GoogleSlidesConfig.OAuthClientId,
            ClientSecret = GoogleSlidesConfig.OAuthClientSecret
        };

        return await GoogleWebAuthorizationBroker.AuthorizeAsync(
            clientSecrets,
            [SlidesScope, DriveScope],
            "default",
            cancellationToken,
            DataStore);
    }
}
