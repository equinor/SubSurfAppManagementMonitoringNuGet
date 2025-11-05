using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Equinor.SubSurfAppManagementMonitoringNuGet.Config;

public class DefaultTelemetryProcessor : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;
    private readonly TelemetrySuccessResponseCodes _successCodes;

    public DefaultTelemetryProcessor(ITelemetryProcessor next, TelemetrySuccessResponseCodes successCodes)
    {
        _next = next;
        _successCodes = successCodes;
    }

    public void Process(ITelemetry item)
    {
        if (item is RequestTelemetry request)
        {
            if (_successCodes.ContainsResponseCode(request.ResponseCode))
            {
                request.Success = true;
            }
        }

        _next.Process(item);
    }
}