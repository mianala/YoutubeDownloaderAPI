using ModelContextProtocol.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerUI;
using YoutubeDownloader.Api.Endpoints;
using YoutubeDownloader.Api.Mcp;
using YoutubeDownloader.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<YoutubeDownloadApiService>();
builder.Services.AddSingleton<DownloadProgressTracker>();
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<YoutubeDownloaderMcpTools>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Content-Disposition");
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

app.UseStaticFiles();
app.UseCors();

app.MapOpenApi("/openapi/{documentName}.json");
app.MapMcp("/mcp");

app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "YoutubeDownloader API";
    options.SwaggerEndpoint("/openapi/v1.json", "YoutubeDownloader API v1");
    options.InjectStylesheet("/swagger-ui/custom.css");
    options.DocExpansion(DocExpansion.List);
    options.DisplayRequestDuration();
});

app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

app.MapGet("/openapi", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

app.MapResolveEndpoints();
app.MapDownloadOptionsEndpoints();
app.MapDownloadEndpoints();

app.Run();
