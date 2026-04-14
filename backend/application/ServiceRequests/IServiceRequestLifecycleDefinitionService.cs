using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;

namespace GTEK.FSM.Backend.Application.ServiceRequests;

public interface IServiceRequestLifecycleDefinitionService
{
    Task<ServiceRequestLifecycleDefinitionResult> GetAsync(
        AuthenticatedPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<ServiceRequestLifecycleDefinitionResult> ReplaceAsync(
        AuthenticatedPrincipal principal,
        UpdateServiceRequestLifecycleRequest request,
        CancellationToken cancellationToken = default);
}