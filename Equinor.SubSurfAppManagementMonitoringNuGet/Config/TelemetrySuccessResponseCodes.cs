namespace Equinor.SubSurfAppManagementMonitoringNuGet.Config;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

public class TelemetrySuccessResponseCodes
{
    private readonly IReadOnlyCollection<string> _responseCodes;

    public TelemetrySuccessResponseCodes(IEnumerable<string>? additionalCodes = null)
    {
        var defaultCodes = new[] { "401", "403", "404" };
        var allCodes = additionalCodes != null
            ? defaultCodes.Concat(additionalCodes).Distinct().ToArray()
            : defaultCodes;
        _responseCodes = new ReadOnlyCollection<string>(allCodes.ToArray());
    }
    public bool ContainsResponseCode(string responseCode) => _responseCodes.Contains(responseCode);
}