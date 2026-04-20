namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;

/// <summary>
/// Request payload for transitioning a service request lifecycle status.
/// </summary>
public sealed class TransitionServiceRequestStatusRequest
{
    public string? NextStageId { get; set; }

    public string? NextStatus { get; set; }

    public string? RowVersion { get; set; }
}
