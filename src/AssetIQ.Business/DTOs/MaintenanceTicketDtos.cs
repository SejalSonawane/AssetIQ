namespace AssetIQ.Business.DTOs;

public sealed class MaintenanceTicketDto
{
    public Guid Id { get; init; }
    public string EquipmentCode { get; init; } = string.Empty;
    public string EquipmentTypeKey { get; init; } = string.Empty;
    public string Section { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string? AssignedTo { get; init; }
    public DateTime? ScheduledAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public string? CompletionNotes { get; init; }
    public string Source { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed record MaintenanceTicketCreateRequest(
    string? EquipmentCode,
    string? Description,
    string? Priority,
    string? AssignedTo,
    DateTime? ScheduledAtUtc);

public sealed class MaintenanceTicketEquipmentOptionDto
{
    public string EquipmentCode { get; init; } = string.Empty;
    public string EquipmentTypeKey { get; init; } = string.Empty;
    public string Section { get; init; } = string.Empty;
}
