using YoutubeDownloader.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

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

app.UseDeveloperExceptionPage();
app.UseCors();

app.MapResolveEndpoints();
app.MapDownloadOptionsEndpoints();
app.MapDownloadEndpoints();

app.Run();
