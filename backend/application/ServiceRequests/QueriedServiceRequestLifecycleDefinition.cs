namespace GTEK.FSM.Backend.Application.ServiceRequests;

public sealed record QueriedServiceRequestLifecycleDefinition(
    IReadOnlyList<QueriedServiceRequestLifecycleStage> Stages,
    IReadOnlyList<QueriedServiceRequestLifecycleTransition> Transitions);
