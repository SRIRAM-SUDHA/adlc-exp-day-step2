namespace OuterloopLabApi.Conversion;

public interface IConversionService
{
    Task<ConversionResponse> ConvertAndPersistAsync(ConversionRequest request);
}

public sealed record ConversionResponse(
    string auditId,
    decimal sourceAmount,
    string sourceCurrency,
    string targetCurrency,
    decimal convertedAmount,
    decimal rate,
    string? providerMarker,
    DateTime backendExecutedAtUtc);
