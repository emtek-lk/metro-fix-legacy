using GTEK.FSM.Backend.Domain.Enums;

namespace GTEK.FSM.Backend.Domain.Policies;

/// <summary>
/// Allowed transition policy for service request lifecycle.
/// </summary>
public static class ServiceRequestStateTransitions
{
    private static readonly HashSet<(ServiceRequestStatus From, ServiceRequestStatus To)> DefaultTransitions = new()
    {
        (ServiceRequestStatus.New, ServiceRequestStatus.Assigned),
        (ServiceRequestStatus.New, ServiceRequestStatus.Cancelled),
        (ServiceRequestStatus.Assigned, ServiceRequestStatus.InProgress),
        (ServiceRequestStatus.Assigned, ServiceRequestStatus.OnHold),
        (ServiceRequestStatus.Assigned, ServiceRequestStatus.Cancelled),
        (ServiceRequestStatus.InProgress, ServiceRequestStatus.OnHold),
        (ServiceRequestStatus.InProgress, ServiceRequestStatus.Completed),
        (ServiceRequestStatus.InProgress, ServiceRequestStatus.Cancelled),
        (ServiceRequestStatus.OnHold, ServiceRequestStatus.Assigned),
        (ServiceRequestStatus.OnHold, ServiceRequestStatus.InProgress),
        (ServiceRequestStatus.OnHold, ServiceRequestStatus.Cancelled),
        (ServiceRequestStatus.Completed, ServiceRequestStatus.Completed),
        (ServiceRequestStatus.Cancelled, ServiceRequestStatus.Cancelled),
    };

    public static bool CanTransition(ServiceRequestStatus from, ServiceRequestStatus to)
    {
        return DefaultTransitions.Contains((from, to));
    }

    public static IReadOnlyCollection<(ServiceRequestStatus From, ServiceRequestStatus To)> GetDefaultTransitions()
    {
        return DefaultTransitions;
    }
}
