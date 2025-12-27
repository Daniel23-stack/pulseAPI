using PulseAPI.Core.Entities;

namespace PulseAPI.Core.Interfaces;

public interface IAlertService
{
    Task EvaluateAlertsAsync(int apiId, HealthCheck healthCheck, CancellationToken cancellationToken = default);
    Task EvaluateCollectionAlertsAsync(int collectionId, CancellationToken cancellationToken = default);
}



