using Coworking.API;
using Coworking.API.Infrastructure.Extensions;
using Coworking.API.Infrastructure.Extensions.Initialization;
using Coworking.Application;
using Coworking.Infrastructure;
using Coworking.Infrastructure.Persistence;
using Coworking.Messaging;

var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureHostOptions(o => o.ServicesStopConcurrently = true);

var config = builder.Configuration;

builder.Services.AddApplication();
builder.Services.AddPersistence(config);
builder.Services.AddInfrastructure(config);

builder.Services.AddMessaging(config);

builder.Services.ConfigureApi(config);

var app = builder.Build();

await app.InitializeApplicationAsync(config);

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAppLocalization();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(CorsExtensions.DefaultCorsPolicyName);

app.UseRateLimiter();

app.UseAuthorization();
app.MapControllers();
app.MapAppHealthChecks();

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