namespace GTEK.FSM.Backend.Application.ServiceRequests;

public sealed record QueriedServiceRequestLifecycleStage(
    Guid StageId,
    string StatusCode,
    string DisplayName,
    int DisplayOrder);
