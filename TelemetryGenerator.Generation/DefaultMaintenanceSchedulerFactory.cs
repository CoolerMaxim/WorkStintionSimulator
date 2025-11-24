using TelemetryGenerator.Core.Services;

namespace TelemetryGenerator.Generation;

public sealed class DefaultMaintenanceSchedulerFactory : IMaintenanceSchedulerFactory
{
    public MaintenanceScheduler Create()
    {
        return new MaintenanceScheduler();
    }
}
