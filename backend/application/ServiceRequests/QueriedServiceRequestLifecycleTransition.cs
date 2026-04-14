namespace GTEK.FSM.Backend.Application.ServiceRequests;

public sealed record QueriedServiceRequestLifecycleTransition(
    string FromStatus,
    string ToStatus,
    bool IsEnabled);