using Coworking.API;
using Coworking.API.Infrastructure.Extensions;
using Coworking.Application;
using Coworking.Infrastructure;
using Coworking.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

/// TODO:
/// Models
/// Mappings
/// Swagger
/// 
/// controllers
/// rate limiting
/// 
/// finish configs

var config = builder.Configuration;

builder.Services.AddApi(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddPersistence(config);
builder.Services.AddInfrastructure(config);

builder.Services.AddOpenApi(); // does it need

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

//app.UseRouting(); // for middlewares?

app.UseCors(CorsExtensions.DefaultCorsPolicyName);

app.UseAuthorization();

app.MapControllers(); // here is uses UseRouting

app.Run();
