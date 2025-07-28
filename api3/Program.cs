using api3.Extensions;
using Finbuckle.MultiTenant;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Add authentication services
builder.Services.AddAuthServices(builder.Configuration);

// Configure Swagger/OpenAPI
builder.Services.AddApplicationSwagger();




var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();

    app.UseSwaggerUi(settings => settings.UseApplicationSwaggerSettings(app.Configuration));
}

app.UseHttpsRedirection();

app.UseMultiTenant();
// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
