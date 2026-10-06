using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.OData;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Repository;
using Scalar.AspNetCore;
using Service;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.secret.json", optional: true, reloadOnChange: true);

// Add services to the container.

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!))
        };
    });

builder.Services.AddRepositoryLevelServices(builder.Configuration);

builder.Services.AddServiceLevelServices(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); })
    .AddOData(options => options.Select().Filter().OrderBy().SetMaxTop(100).Count());

builder.Services.AddMemoryCache();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer((schema, context, _) =>
    {
        if (context.JsonTypeInfo.Type.IsEnum)
        {
            schema.Type = JsonSchemaType.String;
            schema.Enum = [.. Enum.GetNames(context.JsonTypeInfo.Type).Select(name => JsonValue.Create(name))];
        }

        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

//await app.SeedDataAsync();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

var healthGroup = app.MapGroup("/health")
    .AddEndpointFilter(async (context, next) =>
    {
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        if (!context.HttpContext.Request.Headers.TryGetValue(config["SystemHealthCheck:HeaderName"]!,
                out var extractedKey) || !string.Equals(extractedKey, config["SystemHealthCheck:ApiKey"]!))
        {
            return Results.Unauthorized();
        }

        return await next(context);
    });
healthGroup.MapHealthChecks("/live", new HealthCheckOptions { Predicate = _ => false });
healthGroup.MapHealthChecks("/database",
    new HealthCheckOptions { Predicate = check => check.Tags.Contains("database") });
healthGroup.MapHealthChecks("/storage", new HealthCheckOptions { Predicate = check => check.Tags.Contains("storage") });
healthGroup.MapHealthChecks("");

app.Run();