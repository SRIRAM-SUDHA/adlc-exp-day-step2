using System.Text.Json;
using OuterloopLabApi.Http;

namespace OuterloopLabApi.Providers;

public interface IConversionRateProvider
{
    Task<ProviderRateResult> GetRateAsync(string fromCurrency, string toCurrency);
}

public sealed class ProviderRateResult
{
    public bool Success { get; init; }
    public decimal Rate { get; init; }
    public string? ProviderMarker { get; init; }
    public UpstreamProviderException? Error { get; init; }
    public JsonMappingException? MappingError { get; init; }

    public Exception ToException() => Error ?? MappingError!;
}

public sealed class FrankfurterLikeRateProvider : IConversionRateProvider
{
    private readonly HttpClient _http;

    public FrankfurterLikeRateProvider(HttpClient http)
    {
        _http = http;
    }

    public async Task<ProviderRateResult> GetRateAsync(string fromCurrency, string toCurrency)
    {
        try
        {
            // Frankfurter v2: GET {base}/v2/rate/{base}/{quote}
            using var resp = await _http.GetAsync($"v2/rate/{fromCurrency}/{toCurrency}");
            if (!resp.IsSuccessStatusCode)
                return new ProviderRateResult { Success = false, Error = new UpstreamProviderException("Unable to fetch currency quote.") };

            var payload = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            // Flexible mapping: tolerate different property names.
            decimal? rate = null;
            string? providerMarker = null;

            if (root.TryGetProperty("rate", out var rateEl))
                rate = ReadDecimal(rateEl);
            else
                rate = TryReadFromRatesObjects(root, toCurrency);

            providerMarker = TryReadProviderMarker(root);

            if (rate is null)
                return new ProviderRateResult
                {
                    Success = false,
                    MappingError = new JsonMappingException("Unable to map upstream provider rate into internal model.")
                };

            return new ProviderRateResult
            {
                Success = true,
                Rate = rate.Value,
                ProviderMarker = providerMarker
            };
        }
        catch (JsonException)
        {
            return new ProviderRateResult
            {
                Success = false,
                MappingError = new JsonMappingException("Unable to parse upstream provider payload.")
            };
        }
        catch (HttpRequestException)
        {
            return new ProviderRateResult
            {
                Success = false,
                Error = new UpstreamProviderException("Currency provider is unavailable.")
            };
        }
        catch (OperationCanceledException)
        {
            return new ProviderRateResult
            {
                Success = false,
                Error = new UpstreamProviderException("Currency provider timed out.")
            };
        }
        catch (Exception)
        {
            return new ProviderRateResult
            {
                Success = false,
                Error = new UpstreamProviderException("Currency provider error.")
            };
        }
    }

    private static decimal? TryReadFromRatesObjects(JsonElement root, string targetCurrency)
    {
        if (TryGetObject(root, "rates", out var rates))
        {
            if (rates.TryGetProperty(targetCurrency, out var el))
                return ReadDecimal(el);
        }

        if (TryGetObject(root, "conversion_rates", out var conversionRates))
        {
            if (conversionRates.TryGetProperty(targetCurrency, out var el))
                return ReadDecimal(el);
        }

        return null;
    }

    private static bool TryGetObject(JsonElement root, string propName, out JsonElement obj)
    {
        obj = default;
        if (root.TryGetProperty(propName, out var el) && el.ValueKind == JsonValueKind.Object)
        {
            obj = el;
            return true;
        }

        return false;
    }

    private static string? TryReadProviderMarker(JsonElement root)
    {
        if (root.TryGetProperty("date", out var dateEl) && dateEl.ValueKind is JsonValueKind.String)
            return dateEl.GetString();
        if (root.TryGetProperty("timestamp", out var tsEl) && tsEl.ValueKind is JsonValueKind.String)
            return tsEl.GetString();

        return null;
    }

    private static decimal? ReadDecimal(JsonElement el)
    {
        try
        {
            return el.ValueKind switch
            {
                JsonValueKind.Number => el.GetDecimal(),
                JsonValueKind.String => decimal.TryParse(el.GetString(), out var d) ? d : null,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }
}
