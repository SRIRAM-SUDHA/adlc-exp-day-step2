using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using OuterloopLabApi.Options;

namespace OuterloopLabApi.CosmosProvisioning;

internal static class BestEffortArmProvisioner
{
    public static async Task EnsureSqlDatabaseAndContainerAsync(
        CosmosOptions options,
        TokenCredential credential,
        string databaseId,
        string containerId)
    {
        // Best-effort ARM provisioning.
        // Subscription id is not part of the required env var list, so we only attempt ARM when AZURE_SUBSCRIPTION_ID is available.
        try
        {
            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            if (string.IsNullOrWhiteSpace(subscriptionId))
                return;

            var armClient = new ArmClient(credential, subscriptionId);

            // Resource ids for Cosmos DB SQL API resources.
            var cosmosAccountResourceId = $"/subscriptions/{subscriptionId}/resourceGroups/{options.CosmosDbResourceGroup}/providers/Microsoft.DocumentDB/databaseAccounts/{options.CosmosDbAccountName}";
            var account = armClient.GetResource<CosmosDBAccountResource>(new ResourceIdentifier(cosmosAccountResourceId));

            // SQL database.
            var dbCollection = account.GetCosmosDBSqlDatabases();
            var dbInfo = new CosmosDBSqlDatabaseResourceInfo(databaseId);
            var dbContent = new CosmosDBSqlDatabaseCreateOrUpdateContent(new AzureLocation(options.CosmosDbRegion), dbInfo);
            await dbCollection.CreateOrUpdateAsync(WaitUntil.Completed, databaseId, dbContent);

            // SQL container.
            var dbResource = dbCollection.Get(databaseId);
            var containerCollection = dbResource.GetCosmosDBSqlContainers();
            var containerInfo = new CosmosDBSqlContainerResourceInfo(containerId);
            var containerContent = new CosmosDBSqlContainerCreateOrUpdateContent(new AzureLocation(options.CosmosDbRegion), containerInfo);
            await containerCollection.CreateOrUpdateAsync(WaitUntil.Completed, containerId, containerContent);
        }
        catch
        {
            // Per spec: ARM provisioning is best-effort; Managed Identity RBAC for ARM may differ from data-plane RBAC.
        }
    }
}
