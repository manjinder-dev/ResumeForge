using Microsoft.AspNetCore.Http.Features;
using ResumeForge.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 12 * 1024 * 1024;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDevelopment", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
builder.Services.AddScoped<IResumeService, ResumeService>();
builder.Services.AddScoped<IEndpointSecurityValidator, EndpointSecurityValidator>();
builder.Services.AddScoped<IAiProviderGateway, AiProviderGateway>();
builder.Services.AddScoped<IAiProviderClient, OpenAiCompatibleProviderClient>();
builder.Services.AddScoped<IAiProviderClient, AnthropicProviderClient>();
builder.Services.AddScoped<IAiProviderClient, GeminiProviderClient>();

builder.Services
    .AddHttpClient("AiProviders", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(90);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors("LocalDevelopment");
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
