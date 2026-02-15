namespace SEODesk.API.Middleware;

public class HttpsRedirectMiddleware
{
    private readonly RequestDelegate _next;

    public HttpsRedirectMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.IsHttps &&
            !string.IsNullOrEmpty(context.Request.Headers["X-Forwarded-Proto"]) &&
            context.Request.Headers["X-Forwarded-Proto"] != "https")
        {
            context.Request.Scheme = "https";
        }

        await _next(context);
    }
}
