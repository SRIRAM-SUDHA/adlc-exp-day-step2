namespace OuterloopLabApi.Options;

public sealed record CosmosOptions(
    Uri CosmosDbUri,
    string CosmosDbDatabase,
    string CosmosDbContainer,
    string CosmosDbAccountName,
    string CosmosDbResourceGroup,
    string CosmosDbRegion,
    string ManagedIdentityClientId)
{
    public static CosmosOptions FromEnvironment()
    {
        static string ReadRequired(string key)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Missing required environment variable: {key}");
            return value;
        }

        // Exact keys as defined in docs\CONTAINER_ENVIRONMENT_VARIABLES.md.
        var cosmosDbUriString = ReadRequired("COSMOS_DB_URI");
        var cosmosDbDatabase = ReadRequired("COSMOS_DB_DATABASE");
        var cosmosDbContainer = ReadRequired("COSMOS_DB_CONTAINER");
        var cosmosDbAccountName = ReadRequired("COSMOS_DB_ACCOUNT_NAME");
        var cosmosDbResourceGroup = ReadRequired("COSMOS_DB_RESOURCE_GROUP");
        var cosmosDbRegion = ReadRequired("COSMOS_DB_REGION");
        var miClientId = ReadRequired("AZURE_MANAGED_IDENTITY_CLIENT_ID");

        return new CosmosOptions(
            CosmosDbUri: new Uri(cosmosDbUriString, UriKind.Absolute),
            CosmosDbDatabase: cosmosDbDatabase,
            CosmosDbContainer: cosmosDbContainer,
            CosmosDbAccountName: cosmosDbAccountName,
            CosmosDbResourceGroup: cosmosDbResourceGroup,
            CosmosDbRegion: cosmosDbRegion,
            ManagedIdentityClientId: miClientId);
    }
}
