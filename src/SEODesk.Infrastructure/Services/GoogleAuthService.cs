using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.SearchConsole.v1;
using Google.Apis.Services;
using Microsoft.Extensions.Options;
using SEODesk.Infrastructure.Options;
using SEODesk.Infrastructure.Services.Interfaces;

namespace SEODesk.Infrastructure.Services;

public class GoogleAuthService(IOptions<GoogleSearchConsoleOptions> options) : IGoogleAuthService
{
    public SearchConsoleService CreateService(string refreshToken)
    {
        var credential = new UserCredential(
            new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = options.Value.ClientId,
                        ClientSecret = options.Value.ClientSecret
                    }
                }),
            "user",
            new Google.Apis.Auth.OAuth2.Responses.TokenResponse
            {
                RefreshToken = refreshToken
            });

        return new SearchConsoleService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = options.Value.ApplicationName
        });
    }
}
