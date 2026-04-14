namespace GTEK.FSM.Backend.Application.ServiceRequests;

public sealed class ServiceRequestLifecycleDefinitionResult
{
    private ServiceRequestLifecycleDefinitionResult(
        bool isSuccess,
        string message,
        string? errorCode,
        int? statusCode,
        IReadOnlyList<QueriedServiceRequestLifecycleTransition>? payload)
    {
        this.IsSuccess = isSuccess;
        this.Message = message;
        this.ErrorCode = errorCode;
        this.StatusCode = statusCode;
        this.Payload = payload;
    }

    public bool IsSuccess { get; }

    public string Message { get; }

    public string? ErrorCode { get; }

    public int? StatusCode { get; }

    public IReadOnlyList<QueriedServiceRequestLifecycleTransition>? Payload { get; }

    public static ServiceRequestLifecycleDefinitionResult Success(
        IReadOnlyList<QueriedServiceRequestLifecycleTransition> payload,
        string message)
    {
        return new ServiceRequestLifecycleDefinitionResult(true, message, null, null, payload);
    }

    public static ServiceRequestLifecycleDefinitionResult Failure(string message, string errorCode, int statusCode)
    {
        return new ServiceRequestLifecycleDefinitionResult(false, message, errorCode, statusCode, null);
    }
}