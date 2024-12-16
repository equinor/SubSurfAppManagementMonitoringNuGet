using Microsoft.Extensions.Hosting;

namespace Equinor.SubSurfAppManagementMonitoringNuGet.Helpers;

/// <summary>
/// Represents an interface for accessing environment-specific properties such as 
/// environment name and application name.
/// </summary>
public interface IEnvironment
{
    /// <summary>
    /// Gets the name of the current environment (e.g., Development, Staging, Production).
    /// </summary>
    string EnvironmentName { get; }

    /// <summary>
    /// Gets the name of the application as defined in the hosting environment.
    /// </summary>
    string ApplicationName { get; }
}

/// <summary>
/// A wrapper class for <see cref="IHostEnvironment"/> that provides environment-specific properties.
/// </summary>
public class EnvironmentWrapper : IEnvironment
{
    private readonly IHostEnvironment _hostEnvironment;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentWrapper"/> class.
    /// </summary>
    /// <param name="hostEnvironment">The <see cref="IHostEnvironment"/> instance to wrap.</param>
    public EnvironmentWrapper(IHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
    }

    /// <summary>
    /// Gets the name of the current environment (e.g., Development, Staging, Production).
    /// </summary>
    public string EnvironmentName => _hostEnvironment.EnvironmentName;

    /// <summary>
    /// Gets the name of the application as defined in the hosting environment.
    /// </summary>
    public string ApplicationName => _hostEnvironment.ApplicationName;
}

