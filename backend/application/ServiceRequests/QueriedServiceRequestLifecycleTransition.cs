namespace GTEK.FSM.Backend.Application.ServiceRequests;

public sealed record QueriedServiceRequestLifecycleTransition(
    Guid FromStageId,
    Guid ToStageId,
    string FromStatus,
    string ToStatus,
    bool IsEnabled);