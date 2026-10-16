namespace AssetIQ.Business.Interfaces;

public interface IPredictionResultSyncService
{
    Task<int> SyncPersistedEquipmentPredictionsAsync(CancellationToken cancellationToken = default);
}
