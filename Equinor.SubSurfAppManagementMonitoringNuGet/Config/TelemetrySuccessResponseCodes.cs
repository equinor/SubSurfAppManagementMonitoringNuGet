namespace Equinor.SubSurfAppManagementMonitoringNuGet.Config;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Options;

public class TelemetrySuccessResponseCodes
{
    private readonly IReadOnlyCollection<string> _responseCodes;

    public TelemetrySuccessResponseCodes(IOptions<TelemetrySuccessResponseCodesOptions> options)
    {
        var defaultCodes = new[] { "401", "403", "404" };
        var additionalCodes = options.Value.AdditionalCodes ?? new List<string>();
        var allCodes = defaultCodes.Concat(additionalCodes).Distinct().ToArray();
        _responseCodes = new ReadOnlyCollection<string>(allCodes);
    }
    public bool ContainsResponseCode(string responseCode) => _responseCodes.Contains(responseCode);
}