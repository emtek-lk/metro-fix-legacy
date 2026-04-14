namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;

public sealed class GetServiceRequestLifecycleResponse
{
    public IReadOnlyList<ServiceRequestLifecycleTransitionResponse> Items { get; set; } = Array.Empty<ServiceRequestLifecycleTransitionResponse>();
}

public sealed class ServiceRequestLifecycleTransitionResponse
{
    public string? FromStatus { get; set; }

    public string? ToStatus { get; set; }

    public bool IsEnabled { get; set; }
}