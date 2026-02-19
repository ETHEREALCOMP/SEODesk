using SEODesk.Application.Features.Users.Commands;

namespace SEODesk.Application.Features.Users.Response;

public class UserInfoResponse
{
    public UserCommand User { get; set; } = new();
    public List<string> Promotions { get; set; } = new();
}
