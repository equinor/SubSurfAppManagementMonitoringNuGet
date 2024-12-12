namespace Equinor.SubSurfAppManagementMonitoringNuget.Models;

/// <summary>
/// Represents the application for which the health data is retrieved
/// </summary>
public class ApplicationHealth
{
    /// <summary>
    /// Gets or sets the name of the application.
    /// </summary>
    public string ApplicationName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the date when the health data was requested.
    /// </summary>
    public DateTime RequestDate { get; set; }
    /// <summary>
    /// Gets or sets the list of resources associated with the application's health data.
    /// If no resources are available, this property may be null.
    /// </summary>
    public List<Resources>? Resources { get; set; }

    /// <summary>
    /// Sets the resources for all heathdata
    /// </summary>
    /// <param name="resources"></param>
    public void SetResouces(List<Resources> resources)
    {
        Resources = resources;
    }
}