<img alt="intro-logo" src="https://raw.githubusercontent.com/equinor/amplify-component-lib/main/static/amplify.png" width="300px" />

[![SCM Compliance](https://scm-compliance-api.radix.equinor.com/repos/equinor/amplify-component-lib/badge)](https://scm-compliance-api.radix.equinor.com/repos/equinor/amplify-component-lib/badge)

SubsurfAppManagementMonitoringNuget contains software designed to be impemented by APIs that are to be integrated with our monitoring solution.

This package is first and foremost for our own Amplify applications (with some exceptions). If you have an issue or find any bugs, create an issue in Github as we don't accept PRs from contributors outside of the Amplify team.

# SubsurfAppManagementMonitoringNuget
## About

This Repository is a nuget package responsible standardizing HVS (Health Vulnerability and security) for subsurface applications.


## Requirements for User activity:
1. Your API is using Application Insights, ie. ```builder.Services.AddApplicationInsightsTelemetry(builder.Configuration);``` or somthing that achieves the same.
2. User email is available through either **ClaimTypes.Names** (using System.Security.Claims), alternatively in the "UPN" claim.
3. Grant the role **Log Anatylics Reader** in the application insights resource (for each Environment you have) to the serviceprincipals: SubsurfAppManagement-Server-Dev, SubsurfAppManagement-Server-Staging and SubsurfAppManagement-Server-Prod.
4. Send your Application insihgts URI to us.

## Requirements for Health data:
1. Api versioning needs to be registered with version 1 for example: ```builder.Services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
        });```
2. As this solution is not very optimal, we might change this for future versions
3. See *Steps for healthChecks for the next steps*


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
This will ensure that necessary Telemetry is sent to Application Insights, mainly the data for users authenticated

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

The added healthcontroller exprects Api version to be set. It is therefore necessary to register the service api version.
For example:

```
builder.Services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
        });
```

## Uploading new version of the nuget package

In the github repository, Click Create new release. Create a tag in the fromat `v#.#.#`, where `#` are one or more numbers. Upon publishing the release Github actions will pack and upload a new package with version `v#.#.#`.


#### Preview Release

To make a preview release create a release with a tag in the following format `v#.#.#-preview#.#.#`.

