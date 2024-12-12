namespace Equinor.SubSurfAppManagementMonitoringNuget.Constants;

/// <summary>
/// Names of HealthChecks; Used as keys in <see cref="HealthCheckDescriptions"/> dictionary
/// </summary>
public static class HealthCheckNames
{
    public const string SMDA = "SMDA";
    public const string SSDL = "SSDL";
    public const string PDM = "PDM";
    public const string KeyVault = "KeyVault";
    public const string CosmosDB = "Cosmos DB";
    public const string Redis = "Redis";
    public const string BlobStorage = "Blob Storage";
    public const string QueueStorage = "Queue Storage";
    public const string Api = "API";
    public const string SQLServer = "SQL Server";
    public const string Servicebus = "Servicebus";
    public const string ApplicationInsights = "Application Insights";
}