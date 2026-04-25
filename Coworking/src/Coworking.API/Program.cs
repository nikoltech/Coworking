using Coworking.API;
using Coworking.API.Infrastructure.Extensions;
using Coworking.Application;
using Coworking.External.Squidex.Localization;
using Coworking.Infrastructure;
using Coworking.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;

// API layer
builder.Services.AddApi(config);

// Core layers
builder.Services.AddApplication();
builder.Services.AddPersistence(config);
builder.Services.AddInfrastructure(config);

// OpenAPI (modern Swagger replacement in .NET 10)
builder.Services.AddOpenApi();

var app = builder.Build();

// Initialize Squidex locales once before serving requests
await app.Services
    .GetRequiredService<SquidexLocaleProvider>()
    .InitializeAsync(
        app.Services.GetRequiredService<SquidexClientFactory>().Create(),
        app.Lifetime.ApplicationStopping);

// Global error handling
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors(CorsExtensions.DefaultCorsPolicyName);

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

app.Run();