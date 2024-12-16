namespace Equinor.SubSurfAppManagementMonitoringNuGet.Controllers;

/// <summary>
/// Represents options that can be passed to the health controller.
/// </summary>
public class HealthControllerOptions
{
    /// <summary>
    /// A custom parameter to configure the health controller.
    /// </summary>
    public string AppName { get; set; } = string.Empty;
}