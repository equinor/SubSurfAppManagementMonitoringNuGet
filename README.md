## SubsurfAppManagementMonitoringNuget

## About

This Repository is a nuget package responsible standardizing HVS (Health Vulnerability and security) for subsurface applications. The package is hosted in the [github registry](https://github.com/orgs/equinor/packages?repo_name=SubSurfAppManagementMonitoringNuGet).

[github nuget registry documentation](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry)

## How to use

### Assumptions
- Assumes you are using Application Insight and that it is configured for recieving telemetry with for example:
```
services.AddApplicationInsightsTelemetry(Configuration);
```

### Configure services:
- Use the Configure method:
```
Equinor.SubSurfAppManagementMonitoringNuGet.Configure(services, Configuration);
```
This will ensure that necessary Telemetry is sent to Application Insights

### Steps for HealthChecks
This AddDefaultHealthController extension method is designed to simplify the integration of a default health checker service into your application. It registers the necessary components and provides health-checking capabilities with a controller endpoint

- In your Startup.cs or Program.cs (depending on your project type), add one of the health checkers (controllers) to the IServiceCollection:
Example:
```
services.AddDefaultHealthController(Configuration, Configuration["ApplicationConfig:AppName"]!)
  .AddSqlServer(....
```

- If you are using some of the default HealthCheckServices such as the SmdaHealthCheck you need to create an implementation of the IAccessTokenService
Example:
```
    public class TempTokenService : IAccessTokenService
    {
        private readonly someTokenService _someTokenService;
        public TempTokenService(SomeTokenService someTokenService)
        {
            _someTokenService = someTokenService;
        }
        public async Task<string> GetAccessTokenAsync(string resourceId)
        {
            return await _someTokenService.GetAccessTokenAsync(resourceId);
        }

        public async Task<string> GetAccessTokenOnBehalfOfAsync(string resourceId)
        {
            return await _someTokenService.GetAccessTokenOnBehalfOfAsync(resourceId);
        }
    }
```
- Example how to use:
```
.AddSmdaHealthChecks<TempTokenService>(
                    requestUrl: Configuration["SmdaApi:ApiUrl"]!,
...
```

## Uploading new version of the nuget package

In the github repository, Click Create new release. Create a tag in the fromat `v#.#.#`, where `#` are one or more numbers. Upon publishing the release Github actions will pack and upload a new package with version `v#.#.#`.


#### Preview Release

To make a preview release create a release with a tag in the following format `v#.#.#-preview#.#.#`.

