namespace OuterloopLabApi.Auditing;

public interface IAuditRepository
{
    Task CreateAsync(ConversionAudit audit);
    Task<ConversionAudit> GetAsync(string auditId);
    Task<IReadOnlyList<ConversionAuditSummary>> ListAsync(int limit);
}

public sealed record ConversionAuditSummary(
    string auditId,
    decimal convertedAmount,
    string sourceCurrency,
    string targetCurrency,
    decimal rate,
    string? providerMarker,
    DateTime backendExecutedAtUtc);
