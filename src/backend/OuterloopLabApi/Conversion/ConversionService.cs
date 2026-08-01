using OuterloopLabApi.Auditing;
using OuterloopLabApi.Http;
using OuterloopLabApi.Options;
using OuterloopLabApi.Providers;

namespace OuterloopLabApi.Conversion;

public sealed class ConversionService : IConversionService
{
    private readonly IConversionRateProvider _provider;
    private readonly IAuditRepository _repo;
    private readonly AuditClock _clock;

    public ConversionService(
        IConversionRateProvider provider,
        IAuditRepository repo,
        AuditClock clock)
    {
        _provider = provider;
        _repo = repo;
        _clock = clock;
    }

    public async Task<ConversionResponse> ConvertAndPersistAsync(ConversionRequest request)
    {
        var amount = request.amount;
        if (amount <= 0)
            throw new ValidationException("amount must be > 0.");

        var from = NormalizeCurrency(request.fromCurrency, "fromCurrency");
        var to = NormalizeCurrency(request.toCurrency, "toCurrency");
        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("fromCurrency and toCurrency must differ.");

        var rateResult = await _provider.GetRateAsync(from, to);
        if (!rateResult.Success)
            throw rateResult.ToException();

        var converted = CurrencyMath.RoundTwo(amount * rateResult.Rate);

        var auditId = Guid.NewGuid().ToString("n");
        var audit = new ConversionAudit(
            AuditId: auditId,
            SourceAmount: CurrencyMath.RoundTwo(amount),
            SourceCurrency: from,
            TargetCurrency: to,
            ConvertedAmount: converted,
            ProviderRate: rateResult.Rate,
            ProviderMarker: rateResult.ProviderMarker,
            BackendExecutedAtUtc: _clock.UtcNow);

        await _repo.CreateAsync(audit);

        return new ConversionResponse(
            audit.AuditId,
            audit.SourceAmount,
            audit.SourceCurrency,
            audit.TargetCurrency,
            audit.ConvertedAmount,
            audit.ProviderRate,
            audit.ProviderMarker,
            audit.BackendExecutedAtUtc);
    }

    private static string NormalizeCurrency(string currency, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ValidationException($"{fieldName} is required.");

        currency = currency.Trim().ToUpperInvariant();
        if (currency.Length is < 3 or > 3)
            throw new ValidationException($"{fieldName} must be an ISO-4217 3-letter code.");

        return currency;
    }
}
