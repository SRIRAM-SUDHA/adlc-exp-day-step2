using System.Text.Json.Serialization;

namespace OuterloopLabApi.Auditing;

public sealed class ConversionAudit
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("auditId")]
    public string AuditId { get; set; } = default!;

    [JsonPropertyName("sourceAmount")]
    public decimal SourceAmount { get; set; }

    [JsonPropertyName("sourceCurrency")]
    public string SourceCurrency { get; set; } = default!;

    [JsonPropertyName("targetCurrency")]
    public string TargetCurrency { get; set; } = default!;

    [JsonPropertyName("convertedAmount")]
    public decimal ConvertedAmount { get; set; }

    [JsonPropertyName("rate")]
    public decimal ProviderRate { get; set; }

    [JsonPropertyName("providerMarker")]
    public string? ProviderMarker { get; set; }

    [JsonPropertyName("backendExecutedAtUtc")]
    public DateTime BackendExecutedAtUtc { get; set; }

    public ConversionAudit(
        string AuditId,
        decimal SourceAmount,
        string SourceCurrency,
        string TargetCurrency,
        decimal ConvertedAmount,
        decimal ProviderRate,
        string? ProviderMarker,
        DateTime BackendExecutedAtUtc)
    {
        Id = AuditId;
        this.AuditId = AuditId;
        this.SourceAmount = SourceAmount;
        this.SourceCurrency = SourceCurrency;
        this.TargetCurrency = TargetCurrency;
        this.ConvertedAmount = ConvertedAmount;
        this.ProviderRate = ProviderRate;
        this.ProviderMarker = ProviderMarker;
        this.BackendExecutedAtUtc = BackendExecutedAtUtc;
    }

    // Parameterless constructor required by System.Text.Json.
    public ConversionAudit() { }
}
