namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;

public sealed class UpdateServiceRequestLifecycleRequest
{
    public IReadOnlyList<UpdateServiceRequestLifecycleStageRequest> Stages { get; set; } = Array.Empty<UpdateServiceRequestLifecycleStageRequest>();

    public IReadOnlyList<UpdateServiceRequestLifecycleTransitionRequest> Items { get; set; } = Array.Empty<UpdateServiceRequestLifecycleTransitionRequest>();
}

public sealed class UpdateServiceRequestLifecycleStageRequest
{
    public string? StageId { get; set; }

    public string? StatusCode { get; set; }

    public string? DisplayName { get; set; }

    public int DisplayOrder { get; set; }
}

public sealed class UpdateServiceRequestLifecycleTransitionRequest
{
    public string? FromStageId { get; set; }

    public string? ToStageId { get; set; }

    public string? FromStatus { get; set; }

    public string? ToStatus { get; set; }
}