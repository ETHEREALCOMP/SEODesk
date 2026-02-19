using Google.Apis.SearchConsole.v1;

namespace SEODesk.Infrastructure.Services.Interfaces;

public interface IGoogleAuthService
{
    SearchConsoleService CreateService(string refreshToken);
}
