using Finbuckle.MultiTenant;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Purefire.Auth.Data;
using Purefire.Auth.Extensions;
using Purefire.Auth.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();


// Add authentication services
builder.Services.AddAuthServices(builder.Configuration);

// Configure Swagger/OpenAPI
//builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApplicationSwagger();


var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var tenantContext = services.GetRequiredService<TenantContext>();
    await TenantDbSeeder.SeedDataAsync(tenantContext);
    //var context = services.GetRequiredService<AuthDbContext>();
    //var userManager = services.GetRequiredService<UserManager<AppUser>>();
    //var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    //await DbSeeder.SeedDataAsync(context, userManager, roleManager);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{

    app.UseOpenApi();

    app.UseSwaggerUi(settings => settings.UseApplicationSwaggerSettings(app.Configuration));
}

//app.UseHttpsRedirection();
app.UseMultiTenant();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers(); // Remove global authorization requirement

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

app.MapGet("/weatherforecastAuth", (HttpContext context, ILogger<Program> logger) =>
    {
        // Log the request
        logger.LogInformation("Weather forecast endpoint called at {time}", DateTime.UtcNow);

        // Log authentication status
        var user = context.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            logger.LogInformation("User is authenticated. Username: {username}", user.Identity.Name);
            logger.LogInformation("User claims: {claims}", string.Join(", ", user.Claims.Select(c => $"{c.Type}={c.Value}")));
        }
        else
        {
            logger.LogWarning("User is not authenticated");
        }

        // Log headers for debugging
        logger.LogInformation("Request Headers:");
        foreach (var header in context.Request.Headers)
        {
            logger.LogInformation("  {header}: {value}", header.Key, header.Value);
        }

        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecastAuth")
    .WithOpenApi().RequireAuthorization();

app.MapGet("/weatherforecastfree", (HttpContext context, ILogger<Program> logger) =>
    {
        // Log the request
        logger.LogInformation("Weather forecast endpoint called at {time}", DateTime.UtcNow);

        // Log authentication status
        var user = context.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            logger.LogInformation("User is authenticated. Username: {username}", user.Identity.Name);
            logger.LogInformation("User claims: {claims}",
                string.Join(", ", user.Claims.Select(c => $"{c.Type}={c.Value}")));
        }
        else
        {
            logger.LogWarning("User is not authenticated");
        }

        // Log headers for debugging
        logger.LogInformation("Request Headers:");
        foreach (var header in context.Request.Headers)
        {
            logger.LogInformation("  {header}: {value}", header.Key, header.Value);
        }

        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecastfree")
    .WithOpenApi();

app.MapGet("/newendpoint", async (TenantContext tenantContext) => await tenantContext.TenantInfo.ToListAsync())
.WithName("GetNewEndpoint")
.WithOpenApi();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
