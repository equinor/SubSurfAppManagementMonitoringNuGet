using Ardalis.GuardClauses;
using Equinor.SubSurfAppManagementMonitoringNuGet.Authentication;
using Equinor.SubSurfAppManagementMonitoringNuGet.Constants;
using Equinor.SubSurfAppManagementMonitoringNuGet.HttpClients;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Equinor.SubSurfAppManagementMonitoringNuGet.HealthServices.smda;

public class SmdaHealthCheck : IHealthCheck
{
    private readonly ISmdaClient _client;
    
    private readonly IAccessTokenService _tokenService;

    private readonly ILogger<SmdaHealthCheck> _logger;
    
    private readonly string? _requestPath;

    private readonly string? _resourceId;

    public SmdaHealthCheck(ISmdaClient client, IAccessTokenService tokenService, string requestPath, string resourceId, ILogger<SmdaHealthCheck> logger)
    {
        _client = client;
        _tokenService = tokenService;
        _requestPath = Guard.Against.NullOrWhiteSpace(requestPath, nameof(requestPath), "RequestPath is required");
        _resourceId = Guard.Against.NullOrWhiteSpace(resourceId, nameof(resourceId), "ResourceId is required");
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_requestPath is not null && _resourceId is not null)
            {
                _logger.LogInformation($"Get Health Information for SMDA request path: {_requestPath} ");
                
                var token = await _tokenService.GetAccessTokenOnBehalfOfAsync(_resourceId).ConfigureAwait(false);
                var res = await _client.GetSmdaDataAsync(_requestPath, token, HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);
                
                if (res.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy(
                        description: HealthCheckDescriptions.Descriptions[HealthCheckNames.SMDA] ??
                                     "SMDA service is operational.");
                }

                return HealthCheckResult.Unhealthy(
                    description: HealthCheckDescriptions.Descriptions[HealthCheckNames.SMDA] ??
                                 "SMDA service is not operational.",
                    exception: null);
            }
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }

        return HealthCheckResult.Unhealthy(
            description: HealthCheckDescriptions.Descriptions[HealthCheckNames.SMDA] ??
                         "SMDA service encountered an exception.");
    }
}