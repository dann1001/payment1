using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using GatewayService.AccountCharge.Application;
using GatewayService.AccountCharge.Infrastructure;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// ===================== Config: Nobitex options + env fallback =====================
// Read Nobitex config + fall back to env for Token
var nobitexSection = builder.Configuration.GetSection(GatewayService.AccountCharge.Infrastructure.Options.NobitexOptionsConfig.SectionName);
builder.Services.Configure<GatewayService.AccountCharge.Infrastructure.Options.NobitexOptionsConfig>(nobitexSection);

// If Token missing in appsettings, pull from env
var tokenFromEnv = Environment.GetEnvironmentVariable("NOBITEX_API_TOKEN");
if (string.IsNullOrWhiteSpace(nobitexSection["Token"]) && !string.IsNullOrWhiteSpace(tokenFromEnv))
{
    builder.Configuration["Nobitex:Token"] = tokenFromEnv;
}

// ===================== Services =====================
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Controllers & JSON
builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Swagger (base)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// API Versioning (Asp.Versioning)
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;

    // Version readers
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new QueryStringApiVersionReader("api-version"),
        new HeaderApiVersionReader("X-Api-Version"));
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";       // v1, v1.1, ...
    options.SubstituteApiVersionInUrl = true; // replace {version} in route
});

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("InnerNetwork", b =>
        b.WithOrigins(
            "http://172.31.18.2:3000",    // Frontend dev
            "http://localhost:3000")      // Optional local fallback
         .AllowAnyHeader()
         .AllowAnyMethod());
});

var app = builder.Build();

// ===================== Middleware =====================

// CORS
app.UseCors("AllowAll");

// Minimal ProblemDetails for unhandled errors
app.UseExceptionHandler(a =>
{
    a.Run(async ctx =>
    {
        ctx.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Unexpected error",
            Detail = "Something went wrong. Check logs for correlation."
        };
        await ctx.Response.WriteAsJsonAsync(problem);
    });
});

// Only redirect to HTTPS when not explicitly disabled AND environment is Production
var disableHttpsRedirect = builder.Configuration.GetValue<bool>("DisableHttpsRedirection");
if (!disableHttpsRedirect && app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

// Swagger (only in Development here; adjust if you want it in Prod)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    // Versioned Swagger endpoints
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    app.UseSwaggerUI(options =>
    {
        foreach (var desc in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/swagger/{desc.GroupName}/swagger.json", desc.GroupName.ToUpperInvariant());
        }
    });
}

// Routing & endpoints
app.MapControllers();

app.Run();
