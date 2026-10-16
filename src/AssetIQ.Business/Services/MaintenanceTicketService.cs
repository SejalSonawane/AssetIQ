using AssetIQ.Business.DTOs;
using AssetIQ.Business.Interfaces;
using AssetIQ.Data;
using AssetIQ.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetIQ.Business.Services;

public sealed class MaintenanceTicketService : IMaintenanceTicketService
{
    private const int MaxDescriptionLength = 300;
    private const int MaxAssigneeLength = 200;
    private static readonly string[] AllowedStatuses = ["Open", "In Progress", "Completed"];

    private readonly AssetIQDbContext _dbContext;
    private readonly IRollingMillDatasetAccessor _datasetAccessor;

    public MaintenanceTicketService(
        AssetIQDbContext dbContext,
        IRollingMillDatasetAccessor datasetAccessor)
    {
        _dbContext = dbContext;
        _datasetAccessor = datasetAccessor;
    }

    public async Task<MaintenanceTicketDto> CreateTicketAsync(
        MaintenanceTicketCreateRequest request,
        string createdByEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.EquipmentCode))
        {
            throw new ArgumentException("Select equipment for this ticket.", nameof(request.EquipmentCode));
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ArgumentException("Enter a ticket description.", nameof(request.Description));
        }

        if (string.IsNullOrWhiteSpace(request.Priority))
        {
            throw new ArgumentException("Select a priority.", nameof(request.Priority));
        }

        var normalizedDescription = request.Description.Trim();
        if (normalizedDescription.Length > MaxDescriptionLength)
        {
            throw new ArgumentException($"Ticket descriptions must be {MaxDescriptionLength} characters or fewer.", nameof(request.Description));
        }

        var normalizedAssignee = request.AssignedTo?.Trim();
        if (normalizedAssignee?.Length > MaxAssigneeLength)
        {
            throw new ArgumentException($"Assignee must be {MaxAssigneeLength} characters or fewer.", nameof(request.AssignedTo));
        }

        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        if (!snapshot.IsValid)
        {
            throw new InvalidOperationException("Tickets require a valid imported equipment dataset.");
        }

        var equipment = snapshot.Equipment.FirstOrDefault(e =>
            e.EquipmentCode.Equals(request.EquipmentCode.Trim(), StringComparison.OrdinalIgnoreCase));
        if (equipment is null)
        {
            throw new ArgumentException("Select equipment present in the imported dataset.", nameof(request.EquipmentCode));
        }

        var normalizedPriority = request.Priority.Trim();
        if (normalizedPriority.Length is 0 or > 40)
        {
            throw new ArgumentException("Priority must be between 1 and 40 characters.", nameof(request.Priority));
        }

        var normalizedCreatedBy = createdByEmail.Trim();
        if (normalizedCreatedBy.Length == 0)
        {
            throw new ArgumentException("A signed-in user email is required.", nameof(createdByEmail));
        }

        var now = DateTime.UtcNow;
        var normalizedEquipmentCode = request.EquipmentCode.Trim();
        var persistedEquipment = await _dbContext.Equipment
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EquipmentCode == normalizedEquipmentCode, cancellationToken);
        if (persistedEquipment is null)
        {
            throw new InvalidOperationException("Selected equipment has not been persisted to AssetIQ yet.");
        }

        var entity = new MaintenanceRecord
        {
            Id = Guid.NewGuid(),
            EquipmentId = persistedEquipment.Id,
            Title = normalizedDescription,
            Status = "Open",
            Priority = normalizedPriority,
            AssignedTo = normalizedAssignee,
            ScheduledAtUtc = request.ScheduledAtUtc,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Source = "Application"
        };

        _dbContext.MaintenanceRecords.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new MaintenanceTicketDto
        {
            Id = entity.Id,
            EquipmentCode = normalizedEquipmentCode,
            EquipmentTypeKey = equipment.EquipmentTypeKey,
            Section = equipment.Section,
            Title = entity.Title,
            Status = entity.Status,
            Priority = entity.Priority,
            AssignedTo = entity.AssignedTo,
            ScheduledAtUtc = entity.ScheduledAtUtc,
            CompletedAtUtc = entity.CompletedAtUtc,
            CompletionNotes = entity.CompletionNotes,
            Source = entity.Source,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }

    public async Task<IReadOnlyList<MaintenanceTicketEquipmentOptionDto>> GetPersistedEquipmentOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Equipment
            .AsNoTracking()
            .Include(e => e.EquipmentType)
            .OrderBy(e => e.EquipmentCode)
            .Select(e => new MaintenanceTicketEquipmentOptionDto
            {
                EquipmentCode = e.EquipmentCode,
                EquipmentTypeKey = e.EquipmentType!.Key,
                Section = e.Section
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MaintenanceTicketDto>> GetTicketsAsync(CancellationToken cancellationToken = default)
    {
        return await (
            from record in _dbContext.MaintenanceRecords.AsNoTracking()
            join equipment in _dbContext.Equipment.AsNoTracking() on record.EquipmentId equals equipment.Id
            join equipmentType in _dbContext.EquipmentTypes.AsNoTracking() on equipment.EquipmentTypeId equals equipmentType.Id
            orderby record.CreatedAtUtc descending
            select new MaintenanceTicketDto
            {
                Id = record.Id,
                EquipmentCode = equipment.EquipmentCode,
                EquipmentTypeKey = equipmentType.Key,
                Section = equipment.Section,
                Title = record.Title,
                Status = record.Status,
                Priority = record.Priority,
                AssignedTo = record.AssignedTo,
                ScheduledAtUtc = record.ScheduledAtUtc,
                CompletedAtUtc = record.CompletedAtUtc,
                CompletionNotes = record.CompletionNotes,
                Source = record.Source,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            }).ToListAsync(cancellationToken);
    }

    public async Task<bool> UpdateStatusAsync(
        Guid id,
        string status,
        string? completionNotes,
        string? assignedTo,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedStatuses.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        var entity = await _dbContext.MaintenanceRecords
            .FirstOrDefaultAsync(record => record.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        var normalizedStatus = status.Trim();
        entity.Status = normalizedStatus;
        entity.CompletionNotes = normalizedStatus == "Completed" ? completionNotes?.Trim() : null;
        entity.CompletedAtUtc = normalizedStatus == "Completed" ? DateTime.UtcNow : null;
        if (!string.IsNullOrWhiteSpace(assignedTo))
        {
            var normalizedAssignee = assignedTo.Trim();
            if (normalizedAssignee.Length > MaxAssigneeLength)
            {
                return false;
            }

            entity.AssignedTo = normalizedAssignee;
        }

        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static MaintenanceTicketDto Map(MaintenanceRecord entity)
    {
        return new MaintenanceTicketDto
        {
            Id = entity.Id,
            EquipmentCode = entity.Equipment!.EquipmentCode,
            EquipmentTypeKey = entity.Equipment.EquipmentType!.Key,
            Section = entity.Equipment.Section,
            Title = entity.Title,
            Status = entity.Status,
            Priority = entity.Priority,
            AssignedTo = entity.AssignedTo,
            ScheduledAtUtc = entity.ScheduledAtUtc,
            CompletedAtUtc = entity.CompletedAtUtc,
            CompletionNotes = entity.CompletionNotes,
            Source = entity.Source,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }
}
