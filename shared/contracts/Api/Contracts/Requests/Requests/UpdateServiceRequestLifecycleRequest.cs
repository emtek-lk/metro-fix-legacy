namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;

public sealed class UpdateServiceRequestLifecycleRequest
{
    public IReadOnlyList<UpdateServiceRequestLifecycleTransitionRequest> Items { get; set; } = Array.Empty<UpdateServiceRequestLifecycleTransitionRequest>();
}

public sealed class UpdateServiceRequestLifecycleTransitionRequest
{
    public string? FromStatus { get; set; }

    public string? ToStatus { get; set; }
}