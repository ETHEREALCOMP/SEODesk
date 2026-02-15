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
        if (context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto))
        {
            context.Request.Scheme = proto;
        }

        await _next(context);
    }
}
