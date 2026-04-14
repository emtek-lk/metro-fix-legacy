using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;
using GTEK.FSM.Shared.Contracts.Results;

namespace GTEK.FSM.WebPortal.Services.Management;

public interface IManagementRequestLifecycleApiClient
{
    Task<IReadOnlyList<ServiceRequestLifecycleTransitionResponse>> GetAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceRequestLifecycleTransitionResponse>> UpdateAsync(UpdateServiceRequestLifecycleRequest request, CancellationToken cancellationToken = default);
}

public sealed class ManagementRequestLifecycleApiClient : IManagementRequestLifecycleApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient httpClient;

    public ManagementRequestLifecycleApiClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<IReadOnlyList<ServiceRequestLifecycleTransitionResponse>> GetAsync(CancellationToken cancellationToken = default)
    {
        using var response = await this.httpClient.GetAsync("api/v1/management/request-lifecycle", cancellationToken);
        var envelope = await ReadSuccessEnvelopeAsync<GetServiceRequestLifecycleResponse>(response, cancellationToken);
        return envelope.Data?.Items ?? [];
    }

    public async Task<IReadOnlyList<ServiceRequestLifecycleTransitionResponse>> UpdateAsync(
        UpdateServiceRequestLifecycleRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await this.httpClient.PutAsJsonAsync("api/v1/management/request-lifecycle", request, cancellationToken);
        var envelope = await ReadSuccessEnvelopeAsync<GetServiceRequestLifecycleResponse>(response, cancellationToken);
        return envelope.Data?.Items ?? [];
    }

    private static async Task<ApiResponse<T>> ReadSuccessEnvelopeAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var envelope = JsonSerializer.Deserialize<ApiResponse<T>>(content, SerializerOptions);
            if ((envelope is null) || !envelope.Success)
            {
                throw new ManagementRequestLifecycleApiException(response.StatusCode, "API_RESPONSE_INVALID", "The API response was not in the expected success format.");
            }

            return envelope;
        }

        var errorEnvelope = JsonSerializer.Deserialize<ApiResponse<object>>(content, SerializerOptions);
        throw new ManagementRequestLifecycleApiException(
            response.StatusCode,
            errorEnvelope?.ErrorCode,
            errorEnvelope?.Message ?? $"The request failed with status code {(int)response.StatusCode}.");
    }
}

public sealed class ManagementRequestLifecycleApiException : Exception
{
    public ManagementRequestLifecycleApiException(HttpStatusCode statusCode, string? errorCode, string message)
        : base(message)
    {
        this.StatusCode = statusCode;
        this.ErrorCode = errorCode;
    }

    public HttpStatusCode StatusCode { get; }

    public string? ErrorCode { get; }
}