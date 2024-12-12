using Microsoft.Extensions.Hosting;

namespace Equinor.SubSurfAppManagementMonitoringNuget.Helpers;

public interface IEnvironment
{
    string EnvironmentName { get; }
    string ApplicationName { get; }
}

public class EnvironmentWrapper : IEnvironment
{
    private readonly IHostEnvironment _hostEnvironment;

    public EnvironmentWrapper(IHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
    }

    public string EnvironmentName => _hostEnvironment.EnvironmentName;
    public string ApplicationName => _hostEnvironment.ApplicationName;
}
