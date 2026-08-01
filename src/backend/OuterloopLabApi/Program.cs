using System.Text.Json;
using Azure.Identity;
using Microsoft.Azure.Cosmos;

using OuterloopLabApi;
using OuterloopLabApi.Auditing;
using OuterloopLabApi.Conversion;
using OuterloopLabApi.CosmosProvisioning;
using OuterloopLabApi.Http;
using OuterloopLabApi.Options;
using OuterloopLabApi.Providers;

var builder = WebApplication.CreateBuilder(args);

// We intentionally bind all required values from environment variables (no appsettings/local fallbacks).
var cosmosOptions = CosmosOptions.FromEnvironment();

var credentialOptions = new DefaultAzureCredentialOptions
{
    ManagedIdentityClientId = cosmosOptions.ManagedIdentityClientId
};
var credential = new DefaultAzureCredential(credentialOptions);

// External provider base URL is allowed to default.
var currencyProviderBaseUrl = Environment.GetEnvironmentVariable("CURRENCY_API_BASE_URL")
    ?? "https://frankfurter.dev";

// Create Cosmos DB database/container before starting the web app.
var cosmosClient = new CosmosClient(cosmosOptions.CosmosDbUri, credential);
await BestEffortArmProvisioner.EnsureSqlDatabaseAndContainerAsync(
    cosmosOptions,
    credential,
    cosmosOptions.CosmosDbDatabase,
    cosmosOptions.CosmosDbContainer);

await cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosOptions.CosmosDbDatabase, throughput: null);
var database = cosmosClient.GetDatabase(cosmosOptions.CosmosDbDatabase);

var containerProps = new ContainerProperties(cosmosOptions.CosmosDbContainer, "/auditId");
await database.CreateContainerIfNotExistsAsync(containerProps, throughput: null);

var container = database.GetContainer(cosmosOptions.CosmosDbContainer);

builder.Services.AddSingleton(cosmosOptions);
builder.Services.AddSingleton(credential);
builder.Services.AddSingleton(container);

builder.Services.AddHttpClient<IConversionRateProvider, FrankfurterLikeRateProvider>(client =>
{
    // Frankfurter v2 uses: GET {base}/v2/rate/{baseCurrency}/{quoteCurrency}
    // We isolate provider details behind an adapter and map payloads into our internal model.
    client.BaseAddress = new Uri(currencyProviderBaseUrl.TrimEnd('/') + "/");
});

builder.Services.AddSingleton<IAuditRepository, CosmosAuditRepository>();
builder.Services.AddSingleton<IConversionService, ConversionService>();
builder.Services.AddSingleton<AuditClock>();

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

app.MapPost("/api/conversions", async (
    ConversionRequest request,
    IConversionService conversionService,
    HttpContext httpContext) =>
{
    try
    {
        var result = await conversionService.ConvertAndPersistAsync(request);
        return Results.Ok(result);
    }
    catch (ValidationException ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid request");
    }
    catch (UpstreamProviderException ex)
    {
        return Results.Problem(
            detail: ex.SafeMessage,
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Upstream provider error");
    }
    catch (JsonMappingException ex)
    {
        return Results.Problem(
            detail: ex.SafeMessage,
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Upstream provider payload error");
    }
    catch (Exception)
    {
        return Results.Problem(
            detail: "Currency conversion failed.",
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Conversion error");
    }
});

app.MapGet("/api/conversions/{auditId}", async (
    string auditId,
    IAuditRepository repo) =>
{
    try
    {
        var audit = await repo.GetAsync(auditId);
        return Results.Ok(audit);
    }
    catch (NotFoundException)
    {
        return Results.Problem(
            detail: "Audit record not found.",
            statusCode: StatusCodes.Status404NotFound,
            title: "Not Found");
    }
});

app.MapGet("/api/conversions", async (
    int? limit,
    IAuditRepository repo) =>
{
    var effectiveLimit = limit ?? 10;

    if (effectiveLimit is < 1 or > 100)
    {
        return Results.Problem(
            detail: "limit must be between 1 and 100.",
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid request");
    }

    var items = await repo.ListAsync(effectiveLimit);
    return Results.Ok(items);
};

app.Run();

record ConversionRequest(decimal amount, string fromCurrency, string toCurrency);
