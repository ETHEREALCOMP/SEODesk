using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SEODesk.API.Middleware;
using SEODesk.Application.Features.Dashboard;
using SEODesk.Application.Features.Groups;
using SEODesk.Application.Features.Sites;
using SEODesk.Application.Features.Tags;
using SEODesk.Application.Features.Users;
using SEODesk.Infrastructure.Data;
using SEODesk.Infrastructure.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                              ForwardedHeaders.XForwardedProto |
                              ForwardedHeaders.XForwardedHost;
    
    // Explicitly use the namespace to avoid ambiguity with System.Net.IPNetwork in .NET 8+
    options.KnownProxies.Clear();
    options.KnownNetworks.Clear();
});

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrEmpty(databaseUrl))
    throw new InvalidOperationException("DATABASE_URL environment variable is missing");

var databaseUri = new Uri(databaseUrl);
var userInfo = databaseUri.UserInfo.Split(':');

var npgsqlBuilder = new Npgsql.NpgsqlConnectionStringBuilder
{
    Host = databaseUri.Host,
    Port = databaseUri.Port,
    Username = userInfo[0],
    Password = userInfo[1],
    Database = databaseUri.LocalPath.TrimStart('/'),
    SslMode = Npgsql.SslMode.Require,
    Pooling = true
};

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(npgsqlBuilder.ConnectionString, npgsql => npgsql.EnableRetryOnFailure())
);

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>()
    .SetApplicationName("SEODesk");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000", "https://seodesk.tech" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var googleClientId = builder.Configuration["Google:ClientId"];
var googleClientSecret = builder.Configuration["Google:ClientSecret"];

if (string.IsNullOrWhiteSpace(googleClientId) || string.IsNullOrWhiteSpace(googleClientSecret))
{
    var envClient = Environment.GetEnvironmentVariable("Google__ClientId")
                    ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
    var envSecret = Environment.GetEnvironmentVariable("Google__ClientSecret")
                    ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");

    if (!string.IsNullOrWhiteSpace(envClient) && !string.IsNullOrWhiteSpace(envSecret))
    {
        googleClientId = envClient;
        googleClientSecret = envSecret;
    }
    else
    {
        throw new InvalidOperationException(
            "Google OAuth credentials not configured.");
    }
}

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JwtSettings:SecretKey missing");
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "SEODesk.TempAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        options.SlidingExpiration = false;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    })
    .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = googleClientId!;
        options.ClientSecret = googleClientSecret!;
        options.SaveTokens = true;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.Scope.Add("https://www.googleapis.com/auth/webmasters.readonly");

        options.ClaimActions.MapJsonKey("picture", "picture", "url");

        options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
        {
            OnCreatingTicket = context =>
            {
                var picture = context.User.GetProperty("picture").GetString();
                if (!string.IsNullOrEmpty(picture))
                {
                    context.Identity?.AddClaim(new System.Security.Claims.Claim("picture", picture));
                }
                return Task.CompletedTask;
            },
            OnRemoteFailure = context =>
            {
                context.HandleResponse();
                context.Response.Redirect("https://seodesk.tech?error=oauth_failed");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<GoogleSearchConsoleService>();
builder.Services.AddScoped<GetDashboardHandler>();
builder.Services.AddScoped<GetTagsHandler>();
builder.Services.AddScoped<CreateTagHandler>();
builder.Services.AddScoped<UpdateTagHandler>();
builder.Services.AddScoped<DeleteTagHandler>();
builder.Services.AddScoped<GetGroupsHandler>();
builder.Services.AddScoped<UpdateGroupHandler>();
builder.Services.AddScoped<DiscoverSitesHandler>();
builder.Services.AddScoped<UpdateSiteTagsHandler>();
builder.Services.AddScoped<ToggleFavoriteHandler>();
builder.Services.AddScoped<SyncSiteDataHandler>();
builder.Services.AddScoped<ExportSiteDataHandler>();
builder.Services.AddScoped<GetUserInfoHandler>();
builder.Services.AddScoped<GetUserPreferencesHandler>();
builder.Services.AddScoped<UpdateUserPreferencesHandler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration failed: {ex.Message}");
    }
}

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow
}));

app.Run();