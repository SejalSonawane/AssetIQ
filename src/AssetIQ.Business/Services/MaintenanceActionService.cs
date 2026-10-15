using System.Collections.Concurrent;
using AssetIQ.Business.DTOs;
using AssetIQ.Business.Interfaces;

namespace AssetIQ.Business.Services;

public sealed class MaintenanceActionService : IMaintenanceActionService
{
    private const int MaxDescriptionLength = 300;
    private readonly IRollingMillDatasetAccessor _datasetAccessor;
    private readonly ConcurrentDictionary<Guid, MaintenanceApplicationActionDto> _actions = new();

    public MaintenanceActionService(IRollingMillDatasetAccessor datasetAccessor)
    {
        _datasetAccessor = datasetAccessor;
    }

    public async Task<MaintenanceApplicationActionDto> CreateActionAsync(
        string? equipmentCode,
        string? description,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(equipmentCode))
        {
            throw new ArgumentException("Select equipment for this action.", nameof(equipmentCode));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Enter an action description.", nameof(description));
        }

        var normalizedDescription = description.Trim();
        if (normalizedDescription.Length > MaxDescriptionLength)
        {
            throw new ArgumentException($"Action descriptions must be {MaxDescriptionLength} characters or fewer.", nameof(description));
        }

        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        if (!snapshot.IsValid)
        {
            throw new InvalidOperationException("Actions require a valid imported equipment dataset.");
        }

        var equipment = snapshot.Equipment.FirstOrDefault(e =>
            e.EquipmentCode.Equals(equipmentCode.Trim(), StringComparison.OrdinalIgnoreCase));
        if (equipment is null)
        {
            throw new ArgumentException("Select equipment present in the imported dataset.", nameof(equipmentCode));
        }

        var action = new MaintenanceApplicationActionDto
        {
            Id = Guid.NewGuid(),
            EquipmentCode = equipment.EquipmentCode,
            EquipmentTypeKey = equipment.EquipmentTypeKey,
            Section = equipment.Section,
            Description = normalizedDescription,
            Status = "Open",
            CreatedAtLocal = DateTime.Now
        };

        _actions[action.Id] = action;
        return action;
    }

    public Task<IReadOnlyList<MaintenanceApplicationActionDto>> GetActionsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<MaintenanceApplicationActionDto> actions = _actions.Values
            .OrderByDescending(action => action.CreatedAtLocal)
            .ToList();
        return Task.FromResult(actions);
    }

    public Task<bool> CompleteActionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        while (_actions.TryGetValue(id, out var action))
        {
            if (action.Status == "Completed")
            {
                return Task.FromResult(false);
            }

            var completed = action with { Status = "Completed", CompletedAtLocal = DateTime.Now };
            if (_actions.TryUpdate(id, completed, action))
            {
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }
}