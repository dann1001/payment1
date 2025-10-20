// File: D:\GatewayService.AccountCharge\GatewayService.AccountCharge.Api\Program.cs
using System;
using System.Text;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using GatewayService.AccountCharge.Application;
using GatewayService.AccountCharge.Infrastructure;
using GatewayService.AccountCharge.Infrastructure.Http;
using GatewayService.AccountCharge.Infrastructure.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ===================== Config: Nobitex options + env fallback =====================
var nobitexSection = builder.Configuration.GetSection(NobitexOptionsConfig.SectionName);
builder.Services.Configure<NobitexOptionsConfig>(nobitexSection);

var tokenFromEnv = Environment.GetEnvironmentVariable("NOBITEX_API_TOKEN");
if (string.IsNullOrWhiteSpace(nobitexSection["Token"]) && !string.IsNullOrWhiteSpace(tokenFromEnv))
    builder.Configuration["Nobitex:Token"] = tokenFromEnv;

// ===================== Config: PriceQuote =====================
var priceSection = builder.Configuration.GetSection(PriceQuoteOptionsConfig.SectionName);
builder.Services.Configure<PriceQuoteOptionsConfig>(priceSection);

// ===================== JWT Authentication =====================
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.RequireHttpsMetadata = false;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSection["SigningKey"]!))
        };
        opt.MapInboundClaims = false;
    });

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

// ===================== Swagger (with JWT Auth button) =====================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "GatewayService.AccountCharge API", Version = "v1" });

    // 🔐 Add JWT authentication support to Swagger
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT token as: **Bearer {token}**",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    opt.AddSecurityDefinition("Bearer", securityScheme);
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

// ===================== API Versioning =====================
builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ReportApiVersions = true;
    opt.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new QueryStringApiVersionReader("api-version"),
        new HeaderApiVersionReader("X-Api-Version"));
})
.AddApiExplorer(opt =>
{
    opt.GroupNameFormat = "'v'VVV";
    opt.SubstituteApiVersionInUrl = true;
});

// ===================== CORS =====================
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("InnerNetwork", b =>
        b.WithOrigins("http://172.31.18.2:3000", "http://localhost:3000")
         .AllowAnyHeader()
         .AllowAnyMethod());
});

// ===================== HttpClients =====================
var useFakeQuote = builder.Configuration.GetValue<bool>("UseFakePriceQuote");
if (useFakeQuote)
{
    builder.Services.AddSingleton<GatewayService.AccountCharge.Application.Abstractions.IPriceQuoteClient, FakePriceQuoteClient>();
    Console.WriteLine("[Startup] Using FakePriceQuoteClient (mocked USDT rates).");
}
else
{
    builder.Services.AddHttpClient<GatewayService.AccountCharge.Application.Abstractions.IPriceQuoteClient, PriceQuoteClient>(http =>
    {
        var baseUrl = priceSection["BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("PriceQuote:BaseUrl is required.");
        http.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
        var ua = priceSection["UserAgent"];
        if (!string.IsNullOrWhiteSpace(ua)) http.DefaultRequestHeaders.UserAgent.ParseAdd(ua);
    });
}

var app = builder.Build();

// ===================== Middleware =====================
app.UseCors("InnerNetwork");
app.UseAuthentication();
app.UseAuthorization();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opt =>
    {
        opt.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        opt.DisplayRequestDuration();
        opt.DefaultModelExpandDepth(1);
        opt.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });
}

app.MapControllers();
app.Run();
