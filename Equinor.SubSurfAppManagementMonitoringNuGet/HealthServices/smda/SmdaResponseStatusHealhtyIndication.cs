
namespace Equinor.SubSurfAppManagementMonitoringNuGet.HealthServices.smda;

internal static class SmdaResponseStatusHealhtyIndication
{
    public static IEnumerable<string> HealthyStatuses { get; } = new List<string>
    {
        "Healthy",
    };
}
