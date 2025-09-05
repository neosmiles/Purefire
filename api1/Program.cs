using api1.Data;
using api1.Extensions;
using api1.Services;
using api3.Extensions;
using Keycloak.AuthServices.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext use in-memory database
builder.Services.AddDbContext<DataContext>(options =>
    options.UseInMemoryDatabase("InMemoryDb"));

// Add services to the container.
builder.Services.AddControllers(options => options.AddProtectedResources());

// Add authentication services
builder.Services.AddAuthServices(builder.Configuration);

builder.Services.AddScoped<ExampleApiCallService>();


// Configure Swagger/OpenAPI
builder.Services.AddApplicationSwagger();
//builder.Services.AddEndpointsApiExplorer();


var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();

    app.UseSwaggerUi(settings => settings.UseApplicationSwaggerSettings(app.Configuration));

}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();



app.MapControllers();



app.Run();
