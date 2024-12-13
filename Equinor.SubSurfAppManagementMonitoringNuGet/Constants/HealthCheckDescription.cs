namespace Equinor.SubSurfAppManagementMonitoringNuget.Constants;


    /// <summary>
    /// Contains descriptions of Each Named HealthCheck in <see cref="HealthCheckNames"/>
    /// </summary>
    public static class HealthCheckDescriptions
    {
        /// <summary>
        /// Contains descriptions of Each Named HealthCheck in <see cref="HealthCheckNames"/>
        /// </summary>
        private static readonly Dictionary<string, string> _descriptions = new()
        {
            { HealthCheckNames.SMDA, "SMDA (Subsurface Master Data) is a system that consolidates data across Equinor’s internal and external sources and provides the best available subsurface data basis for stakeholder applications and users in Petec, Exploration, Drilling & Well and more."},
            { HealthCheckNames.SSDL, "SSDL (Subsurface Data Lake) allows accessibility to high quality and analytics ready datasets for subsurface disciplines in Exploration, Petec and Drilling & well"},
            { HealthCheckNames.PDM, "Production data mart (PDM) is a SQL server database in Omnia consolidating production related data from a multitude of different data sources."},
            { HealthCheckNames.Api, "Backend, API"},
            { HealthCheckNames.KeyVault, "Azure Key Vault is a secure container for storing sensitive data"},
            { HealthCheckNames.CosmosDB, "Azure Cosmos DB is a fully managed NoSQL and relational database"},
            { HealthCheckNames.Redis, "Redis cache is an open-source, in-memory data store that uses key-value data structures to store and serve data fast."},
            { HealthCheckNames.BlobStorage, "Azure Blob Storage is an object storage solution that can store unstructured data" },
            { HealthCheckNames.QueueStorage, "Azure Queue Storage is a service for storing large numbers of messages"},
            { HealthCheckNames.SQLServer, "SQL Server is a Database management system for SQL databases"},
            { HealthCheckNames.Servicebus, "Azure Service bus is a message broker with queues and publish-subscribe topics. It is used for Realtime notifications"},
            { HealthCheckNames.ApplicationInsights, "Application Insights is a feature of Azure Monitor that monitors and analyzes the performance and usage of web applications"},
        };
        
        public static IReadOnlyDictionary<string, string> Descriptions => _descriptions;
        
        public static void AddOrUpdateDescription(string key, string description)
        {
            if (_descriptions.ContainsKey(key))
            {
                _descriptions[key] = description;
            }
            else
            {
                _descriptions.Add(key, description);
            }
        }
    }