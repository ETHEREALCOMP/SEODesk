using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SEODesk.Domain.Entities;
using SEODesk.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SEODesk.API.Controllers;

[ApiController]
[Route("")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly Application.Features.Sites.DiscoverSitesHandler _discoverSitesHandler;

    public AuthController(
        ApplicationDbContext dbContext, 
        IConfiguration configuration,
        Application.Features.Sites.DiscoverSitesHandler discoverSitesHandler)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _discoverSitesHandler = discoverSitesHandler;
    }

    // =========================
    // GOOGLE SIGN IN
    // =========================
    [HttpGet("signin-google")]
    public IActionResult SignInGoogle()
    {
        // Вказуємо RedirectUri, куди перейти ПІСЛЯ того, як Google Middleware закінчить роботу
        var properties = new AuthenticationProperties
        {
            RedirectUri = "/api/auth/callback"
        };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    // =========================
    // GOOGLE CALLBACK
    // =========================
    [HttpGet("api/auth/callback")]
    public async Task<IActionResult> SignInGoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        if (!result.Succeeded || result.Principal == null)
        {
            return BadRequest(new
            {
                error = "Google authentication failed",
                reason = result.Failure?.Message
            });
        }

        var claims = result.Principal.Claims.ToList();

        // 🔥 GOOGLE RETURNS "sub", NOT NameIdentifier
        var googleId =
            claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
            ?? claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        var picture = claims.FirstOrDefault(c => c.Type == "picture")?.Value;

        if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
        {
            return BadRequest(new { error = "Required claims missing" });
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);
        
        // 🔥 Отримуємо refresh token
        var refreshToken = result.Properties.GetTokenValue("refresh_token");

        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                GoogleId = googleId,
                Email = email,
                Name = name ?? email,
                Picture = picture,
                GoogleRefreshToken = refreshToken ?? "",
                Plan = PlanType.TRIAL,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);

            _dbContext.Groups.Add(new Group
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                DisplayName = "My sites",
                EmailOwner = email,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow
            });

            _dbContext.Tags.Add(new Tag
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Name = "All",
                IsDeletable = false,
                CreatedAt = DateTime.UtcNow
            });

            _dbContext.UserPreferences.Add(new UserPreference
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                SelectedMetrics = "clicks,impressions",
                LastRangePreset = "last28days",
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            user.Name = name ?? user.Name;
            user.Picture = picture ?? user.Picture;
            
            if (!string.IsNullOrEmpty(refreshToken))
            {
                user.GoogleRefreshToken = refreshToken;
            }
        }

        await _dbContext.SaveChangesAsync();

        // 🔥 Автоматично тягнемо сайти з GSC
        if (!string.IsNullOrEmpty(user.GoogleRefreshToken))
        {
            await _discoverSitesHandler.HandleAsync(
                new Application.Features.Sites.DiscoverSitesCommand { UserId = user.Id },
                _configuration["Google:ClientId"]!,
                _configuration["Google:ClientSecret"]!
            );
        }

        var jwt = GenerateJwtToken(user);

        // ✅ Redirect to frontend with JWT token in URL
        return Redirect($"http://localhost:3000/dashboard?token={jwt}");
    }

    // =========================
    // ME (JWT ONLY)
    // =========================
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("api/auth/me")]
    public async Task<IActionResult> GetMe()
    {
        var googleId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (googleId == null)
            return Unauthorized();

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);

        if (user == null)
            return NotFound();

        return Ok(new
        {
            user = new
            {
                id = user.Id,
                email = user.Email,
                name = user.Name,
                avatar = user.Picture,
                plan = user.Plan.ToString()
            }
        });
    }

    // =========================
    // JWT
    // =========================
    private string GenerateJwtToken(User user)
    {
        var jwt = _configuration.GetSection("JwtSettings");

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwt["SecretKey"]!)
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.GoogleId),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(ClaimTypes.NameIdentifier, user.GoogleId),
            new Claim("userId", user.Id.ToString()),
            new Claim("plan", user.Plan.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(
                int.Parse(jwt["ExpiryInHours"] ?? "24")
            ),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
