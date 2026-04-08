using Coworking.Application;
using Coworking.Infrastructure;
using Coworking.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);


var config = builder.Configuration;

builder.Services.AddApplication();
builder.Services.AddPersistence(config);
builder.Services.AddInfrastructure(config);

//builder.Services.AddHttpContextAccessor(); // for minimal apis. for ctor optional

builder.Services.AddProblemDetails();

builder.Services.AddControllers();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
