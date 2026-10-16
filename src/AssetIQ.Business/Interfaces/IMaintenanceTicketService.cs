using AssetIQ.Business.DTOs;

namespace AssetIQ.Business.Interfaces;

public interface IMaintenanceTicketService
{
    Task<MaintenanceTicketDto> CreateTicketAsync(
        MaintenanceTicketCreateRequest request,
        string createdByEmail,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceTicketDto>> GetTicketsAsync(CancellationToken cancellationToken = default);

    Task<bool> UpdateStatusAsync(
        Guid id,
        string status,
        string? completionNotes,
        string? assignedTo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceTicketEquipmentOptionDto>> GetPersistedEquipmentOptionsAsync(
        CancellationToken cancellationToken = default);
}
