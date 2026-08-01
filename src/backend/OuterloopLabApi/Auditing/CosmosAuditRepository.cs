using Microsoft.Azure.Cosmos;
using OuterloopLabApi.Http;

namespace OuterloopLabApi.Auditing;

public sealed class CosmosAuditRepository : IAuditRepository
{
    private readonly Container _container;

    public CosmosAuditRepository(Container container)
    {
        _container = container;
    }

    public async Task CreateAsync(ConversionAudit audit)
    {
        await _container.CreateItemAsync(audit, new PartitionKey(audit.AuditId));
    }

    public async Task<ConversionAudit> GetAsync(string auditId)
    {
        try
        {
            var resp = await _container.ReadItemAsync<ConversionAudit>(auditId, new PartitionKey(auditId));
            return resp.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new NotFoundException("Audit record not found.");
        }
    }

    public async Task<IReadOnlyList<ConversionAuditSummary>> ListAsync(int limit)
    {
        var query = "SELECT * FROM c ORDER BY c.backendExecutedAtUtc DESC";
        var iterator = _container.GetItemQueryIterator<ConversionAudit>(new QueryDefinition(query), requestOptions: new QueryRequestOptions
        {
            MaxItemCount = limit,
        });

        var results = new List<ConversionAuditSummary>();
        while (iterator.HasMoreResults && results.Count < limit)
        {
            var page = await iterator.ReadNextAsync();
            foreach (var item in page)
            {
                results.Add(new ConversionAuditSummary(
                    item.AuditId,
                    item.ConvertedAmount,
                    item.SourceCurrency,
                    item.TargetCurrency,
                    item.ProviderRate,
                    item.ProviderMarker,
                    item.BackendExecutedAtUtc));
                if (results.Count >= limit)
                    break;
            }
        }

        return results;
    }
}
