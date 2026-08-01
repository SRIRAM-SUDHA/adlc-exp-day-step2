using OuterloopLabApi.Auditing;
using OuterloopLabApi;
using OuterloopLabApi.Conversion;
using OuterloopLabApi.Http;
using OuterloopLabApi.Providers;
using Xunit;

namespace Tests;

public sealed class ConversionServiceTests
{
    [Fact]
    public async Task Persists_Immutable_Audit_Using_Rounded_ConvertedAmount()
    {
        var provider = new FixedRateProvider(0.92345m, "2026-08-01");
        var repo = new InMemoryAuditRepository();
        var clock = new AuditClock();
        var service = new ConversionService(provider, repo, clock);

        var request = new ConversionRequest(amount: 100.00m, fromCurrency: "USD", toCurrency: "EUR");
        var response = await service.ConvertAndPersistAsync(request);

        Assert.NotNull(response.auditId);
        Assert.Equal(92.35m, response.convertedAmount);
        Assert.Equal(0.92345m, response.rate);
        Assert.Equal("2026-08-01", response.providerMarker);

        Assert.Equal(1, repo.Items.Count);
        var saved = repo.Items[0];
        Assert.Equal(response.auditId, saved.AuditId);
        Assert.Equal(92.35m, saved.ConvertedAmount);
        Assert.Equal(0.92345m, saved.ProviderRate);
    }

    private sealed class FixedRateProvider : IConversionRateProvider
    {
        private readonly decimal _rate;
        private readonly string _marker;

        public FixedRateProvider(decimal rate, string marker)
        {
            _rate = rate;
            _marker = marker;
        }

        public Task<ProviderRateResult> GetRateAsync(string fromCurrency, string toCurrency)
        {
            return Task.FromResult(new ProviderRateResult
            {
                Success = true,
                Rate = _rate,
                ProviderMarker = _marker
            });
        }
    }

    private sealed class InMemoryAuditRepository : IAuditRepository
    {
        public List<ConversionAudit> Items { get; } = new();

        public Task CreateAsync(ConversionAudit audit)
        {
            Items.Add(audit);
            return Task.CompletedTask;
        }

        public Task<ConversionAudit> GetAsync(string auditId)
        {
            var found = Items.FirstOrDefault(x => x.AuditId == auditId);
            if (found is null)
                throw new NotFoundException("Audit record not found.");
            return Task.FromResult(found);
        }

        public Task<IReadOnlyList<ConversionAuditSummary>> ListAsync(int limit)
        {
            var list = Items
                .Take(limit)
                .Select(x => new ConversionAuditSummary(
                    x.AuditId,
                    x.ConvertedAmount,
                    x.SourceCurrency,
                    x.TargetCurrency,
                    x.ProviderRate,
                    x.ProviderMarker,
                    x.BackendExecutedAtUtc))
                .ToList();
            return Task.FromResult((IReadOnlyList<ConversionAuditSummary>)list);
        }
    }
}
