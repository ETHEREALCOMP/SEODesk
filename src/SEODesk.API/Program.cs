using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
    options.KnownProxies.Clear();
});

// Ensure user secrets are loaded in the Development environment so
// values like Google:ClientId and Google:ClientSecret are available
// even when running from the IDE or dotnet run.
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

// =======================
// Database
// =======================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.EnableRetryOnFailure()
    )
);

// =======================
// MVC / Swagger
// =======================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =======================
// CORS
// =======================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// =======================
// Authentication
// =======================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JwtSettings:SecretKey missing");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })

    // ---- TEMP COOKIE (OAuth only)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "SEODesk.TempAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        options.SlidingExpiration = false;
    })

    // ---- JWT (API only)
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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey)
            ),

            ClockSkew = TimeSpan.Zero
        };
    })

    // ---- GOOGLE OAUTH
    .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = builder.Configuration["Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Google:ClientSecret"]!;
        options.CallbackPath = "/api/auth/callback";
        options.SaveTokens = true;

        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.Scope.Add("https://www.googleapis.com/auth/webmasters.readonly");

        options.ClaimActions.MapJsonKey("picture", "picture", "url");

        // ✅ Обробка Railway proxy
        options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
        {
            OnRedirectToAuthorizationEndpoint = context =>
            {
                // Перевірити чи правильна схема
                if (context.RedirectUri.StartsWith("http://"))
                {
                    context.RedirectUri = context.RedirectUri.Replace("http://", "https://");
                }

                Console.WriteLine($"OAuth Redirect URI: {context.RedirectUri}");
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnCreatingTicket = context =>
            {
                var picture = context.User.GetProperty("picture").GetString();
                if (!string.IsNullOrEmpty(picture))
                {
                    context.Identity?.AddClaim(new System.Security.Claims.Claim("picture", picture));
                }
                return Task.CompletedTask;
            }
        };
    });

// =======================
// Authorization
// =======================
builder.Services.AddAuthorization();

// =======================
// DI
// =======================
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

// =======================
// Pipeline
// =======================
var app = builder.Build();

app.Use(async (context, next) =>
{
    // Railway forwarding
    var forwardedProto = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault();
    if (!string.IsNullOrEmpty(forwardedProto))
    {
        context.Request.Scheme = forwardedProto;
    }

    var forwardedHost = context.Request.Headers["X-Forwarded-Host"].FirstOrDefault();
    if (!string.IsNullOrEmpty(forwardedHost))
    {
        context.Request.Host = new HostString(forwardedHost);
    }

    await next();
});


app.Use(async (context, next) =>
{
    if (context.Request.Headers.ContainsKey("X-Forwarded-Proto"))
    {
        context.Request.Scheme = "https";
    }
    await next();
});

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
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


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}