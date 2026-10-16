var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:8081/");
    client.Timeout = TimeSpan.FromSeconds(5);
});
var app = builder.Build();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; style-src 'self'; script-src 'self'; frame-ancestors 'none'; base-uri 'self'";
    await next();
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/health", async (IHttpClientFactory factory, CancellationToken ct) =>
{
    try
    {
        using var response = await factory.CreateClient("api").GetAsync("health", ct);
        return response.IsSuccessStatusCode ? Results.Ok(new { status = "healthy", service = "web" }) : Results.StatusCode(503);
    }
    catch (HttpRequestException) { return Results.StatusCode(503); }
    catch (OperationCanceledException) { return Results.StatusCode(503); }
});
// The browser calls its own origin. Only the web service talks to the internal API.
app.MapMethods("/api/tasks/{**path}", ["GET", "POST", "PUT", "DELETE"], async (HttpContext context, IHttpClientFactory factory) =>
{
    var path = context.Request.Path.Value![5..];
    using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), path);
    if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
    {
        request.Content = new StreamContent(context.Request.Body);
        if (context.Request.ContentType is not null)
            request.Content.Headers.TryAddWithoutValidation("Content-Type", context.Request.ContentType);
    }
    try
    {
        using var response = await factory.CreateClient("api").SendAsync(request, context.RequestAborted);
        context.Response.StatusCode = (int)response.StatusCode;
        if (response.Content.Headers.ContentType is not null)
            context.Response.ContentType = response.Content.Headers.ContentType.ToString();
        await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    }
    catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
    {
        if (!context.Response.HasStarted && !context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = 503;
            await context.Response.WriteAsJsonAsync(new { detail = "The tasks service is unavailable. Please try again." });
        }
    }
});
await app.RunAsync();
return 0;
