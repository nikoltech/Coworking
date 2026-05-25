using Coworking.API;
using Coworking.API.Infrastructure.Extensions;
using Coworking.API.Infrastructure.Extensions.Initialization;
using Coworking.Application;
using Coworking.Infrastructure;
using Coworking.Infrastructure.Persistence;
using Coworking.Messaging;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;

// Core layers
builder.Services.AddApplication();
builder.Services.AddPersistence(config);
builder.Services.AddInfrastructure(config);
builder.Services.AddMessaging(config);

// API layer
builder.Services.ConfigureApi(config);

// Build
var app = builder.Build();

await app.InitializeApplicationAsync(config);

app.UseForwardedHeaders();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsExtensions.DefaultCorsPolicyName);

app.UseRateLimiter();

app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/", context =>
    {
        context.Response.Redirect("/swagger");
        return Task.CompletedTask;
    });
}

app.Run();



//app.MapGet("/check-my-ip", (HttpContext context) =>
//{
//    return Results.Ok(new
//    {
//        RemoteIp = context.Connection.RemoteIpAddress?.ToString(),
//        CF_Ip = context.Request.Headers["CF-Connecting-IP"].ToString(),
//        ForwardedFor = context.Request.Headers["X-Forwarded-For"].ToString()
//    });
//});