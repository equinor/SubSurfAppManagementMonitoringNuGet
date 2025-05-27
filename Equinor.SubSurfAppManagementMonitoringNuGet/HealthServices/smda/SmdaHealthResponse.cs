using System.Text.Json.Serialization;

namespace Equinor.SubSurfAppManagementMonitoringNuGet.HealthServices.smda;

internal record SmdaHealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;
}

internal static class SmdaResponseStatusHealhtyIndication
{
    public static IEnumerable<string> HealthyStatuses { get; } = new List<string>
    {
        "UP",
    };
}
