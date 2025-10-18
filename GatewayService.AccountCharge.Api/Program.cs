// File: D:\GatewayService.AccountCharge\GatewayService.AccountCharge.Api\Program.cs
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using GatewayService.AccountCharge.Application;
using GatewayService.AccountCharge.Infrastructure;
using GatewayService.AccountCharge.Infrastructure.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ===================== Config: Nobitex options + env fallback =====================
var nobitexSection = builder.Configuration.GetSection(NobitexOptionsConfig.SectionName);
builder.Services.Configure<NobitexOptionsConfig>(nobitexSection);

var tokenFromEnv = Environment.GetEnvironmentVariable("NOBITEX_API_TOKEN");
if (string.IsNullOrWhiteSpace(nobitexSection["Token"]) && !string.IsNullOrWhiteSpace(tokenFromEnv))
{
    builder.Configuration["Nobitex:Token"] = tokenFromEnv;
}

// ===================== Config: PriceQuote =====================
var priceSection = builder.Configuration.GetSection(PriceQuoteOptionsConfig.SectionName);
builder.Services.Configure<PriceQuoteOptionsConfig>(priceSection);

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

    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new QueryStringApiVersionReader("api-version"),
        new HeaderApiVersionReader("X-Api-Version"));
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// CORS
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("InnerNetwork", b =>
        b.WithOrigins("http://172.31.18.2:3000", "http://localhost:3000")
         .AllowAnyHeader()
         .AllowAnyMethod());
});

// ------------ HttpClients ------------
builder.Services.AddHttpClient<GatewayService.AccountCharge.Application.Abstractions.INobitexClient, GatewayService.AccountCharge.Infrastructure.Http.NobitexClient>(http =>
{
    var baseUrl = nobitexSection["BaseUrl"] ?? "https://apiv2.nobitex.ir";
    http.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
    var ua = nobitexSection["UserAgent"];
    if (!string.IsNullOrWhiteSpace(ua)) http.DefaultRequestHeaders.UserAgent.ParseAdd(ua);
    var token = builder.Configuration["Nobitex:Token"];
    if (!string.IsNullOrWhiteSpace(token))
        http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Token {token}");
});

builder.Services.AddHttpClient<GatewayService.AccountCharge.Application.Abstractions.IAccountingClient, GatewayService.AccountCharge.Infrastructure.Http.AccountingClient>(http =>
{
    var baseUrl = builder.Configuration["Accounting:BaseUrl"] ?? "https://localhost:7123/";
    http.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
    var ua = builder.Configuration["Accounting:UserAgent"];
    if (!string.IsNullOrWhiteSpace(ua)) http.DefaultRequestHeaders.UserAgent.ParseAdd(ua);
});

// ⬇️ PriceQuote client (سرویس همکار)
builder.Services.AddHttpClient<GatewayService.AccountCharge.Application.Abstractions.IPriceQuoteClient, GatewayService.AccountCharge.Infrastructure.Http.PriceQuoteClient>(http =>
{
    var baseUrl = priceSection["BaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("PriceQuote:BaseUrl is required.");
    http.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
    var ua = priceSection["UserAgent"];
    if (!string.IsNullOrWhiteSpace(ua)) http.DefaultRequestHeaders.UserAgent.ParseAdd(ua);
});

var app = builder.Build();

// ===================== Middleware =====================
app.UseCors("InnerNetwork"); // <-- از سیاست تعریف‌شده استفاده کن

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

var disableHttpsRedirect = builder.Configuration.GetValue<bool>("DisableHttpsRedirection");
if (!disableHttpsRedirect && app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    app.UseSwaggerUI(options =>
    {
        foreach (var desc in provider.ApiVersionDescriptions)
            options.SwaggerEndpoint($"/swagger/{desc.GroupName}/swagger.json", desc.GroupName.ToUpperInvariant());
    });
}

app.MapControllers();
app.Run();
