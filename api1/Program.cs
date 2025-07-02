using Microsoft.AspNetCore.Identity;
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
//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new OpenApiInfo
//    {
//        Title = "API 1 - Authentication Service",
//        Version = "v1",
//        Description = "API for handling user and client authentication",
//        Contact = new OpenApiContact
//        {
//            Name = "API Support",
//            Email = "support@example.com"
//        }
//    });

//    // Add JWT Authentication
//    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//    {
//        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
//        Name = "Authorization",
//        In = ParameterLocation.Header,
//        Type = SecuritySchemeType.ApiKey,
//        Scheme = "Bearer"
//    });

//    c.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        {
//            new OpenApiSecurityScheme
//            {
//                Reference = new OpenApiReference
//                {
//                    Type = ReferenceType.SecurityScheme,
//                    Id = "Bearer"
//                }
//            },
//            Array.Empty<string>()
//        }
//    });

//    // Include XML Comments
//    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
//    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
//    if (File.Exists(xmlPath))
//    {
//        c.IncludeXmlComments(xmlPath);
//    }
//});

var app = builder.Build();

// Seed the database
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    var context = services.GetRequiredService<AuthDbContext>();
//    var userManager = services.GetRequiredService<UserManager<AppUser>>();
//    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
//    await DbSeeder.SeedDataAsync(context, userManager, roleManager);
//}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{

    app.UseOpenApi();

    app.UseSwaggerUi(settings => settings.UseApplicationSwaggerSettings(app.Configuration));
}

//app.UseHttpsRedirection();

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

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
