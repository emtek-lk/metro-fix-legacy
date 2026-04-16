namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;

public sealed class GetServiceRequestLifecycleResponse
{
    public IReadOnlyList<ServiceRequestLifecycleStageResponse> Stages { get; set; } = Array.Empty<ServiceRequestLifecycleStageResponse>();

    public IReadOnlyList<ServiceRequestLifecycleTransitionResponse> Items { get; set; } = Array.Empty<ServiceRequestLifecycleTransitionResponse>();
}

public sealed class ServiceRequestLifecycleStageResponse
{
    public string? StageId { get; set; }

    public string? StatusCode { get; set; }

    public string? DisplayName { get; set; }

    public int DisplayOrder { get; set; }
}

public sealed class ServiceRequestLifecycleTransitionResponse
{
    public string? FromStageId { get; set; }

    public string? ToStageId { get; set; }

    public string? FromStatus { get; set; }

    public string? ToStatus { get; set; }

    public bool IsEnabled { get; set; }
}