using AssetIQ.Business.DTOs;
using AssetIQ.Business.Services;
using AssetIQ.Data;
using AssetIQ.Data.Entities;
using AssetIQ.Data.Import;
using AssetIQ.Data.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AssetIQ.Tests;

public sealed class MaintenanceTicketServiceTests
{
    private const string WorkspaceCsvPath = "data/rolling-mill/AssetIQ_RollingMill_500Rows.csv";
    private static readonly DateTime ScheduledAt = new(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc);

    private static string GetCsvPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AssetIQ.slnx")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory!.FullName, WorkspaceCsvPath);
    }

    private static RollingMillDatasetAccessor CreateAccessor() =>
        new(
            new RollingMillCsvReader(),
            Options.Create(new DatasetOptions
            {
                RollingMillCsvPath = GetCsvPath(),
                PlantDisplayName = "Test dataset"
            }));

    private static AssetIQDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AssetIQDbContext>()
            .UseInMemoryDatabase($"maintenance_ticket_tests_{Guid.NewGuid():N}")
            .Options;
        return new AssetIQDbContext(options);
    }

    private static async Task<Equipment> CreateEquipmentAsync(AssetIQDbContext context)
    {
        var plant = new Plant { Id = Guid.NewGuid(), PlantCode = "P1", Name = "Test Plant", TimeZoneId = "UTC", CreatedAtUtc = DateTime.UtcNow };
        var type = new EquipmentType { Id = Guid.NewGuid(), Key = "Coiler", Name = "Coiler" };
        var equipment = new Equipment
        {
            Id = Guid.NewGuid(),
            PlantId = plant.Id,
            EquipmentTypeId = type.Id,
            EquipmentCode = "RM-1002",
            Name = "RM-1002",
            Section = "Finishing",
            Criticality = "High",
            OperatingStatus = "Online",
            CreatedAtUtc = DateTime.UtcNow,
            Plant = plant,
            EquipmentType = type
        };
        context.AddRange(plant, type, equipment);
        await context.SaveChangesAsync();
        return equipment;
    }

    [Fact]
    public async Task CreateTicketPersistsMaintenanceRecord()
    {
        await using var context = CreateContext();
        await CreateEquipmentAsync(context);
        var service = new MaintenanceTicketService(context, CreateAccessor());

        var ticket = await service.CreateTicketAsync(
            new MaintenanceTicketCreateRequest("RM-1002", "Inspect bearing", "High", "maintenance@assetiq.local", ScheduledAt),
            "maintenance@assetiq.local");

        var persisted = await context.MaintenanceRecords.SingleAsync();
        Assert.Equal(ticket.Id, persisted.Id);
        Assert.Equal("RM-1002", ticket.EquipmentCode);
        Assert.Equal("High", ticket.Priority);
        Assert.Equal("maintenance@assetiq.local", ticket.AssignedTo);
        Assert.Equal(ScheduledAt, ticket.ScheduledAtUtc);
        Assert.Equal("Application", ticket.Source);
        Assert.Equal("Open", ticket.Status);
        Assert.NotEqual(default, ticket.CreatedAtUtc);
    }

    [Fact]
    public async Task UpdateStatusCompletesTicketWithNotes()
    {
        await using var context = CreateContext();
        await CreateEquipmentAsync(context);
        var service = new MaintenanceTicketService(context, CreateAccessor());
        var ticket = await service.CreateTicketAsync(
            new MaintenanceTicketCreateRequest("RM-1002", "Inspect bearing", "High", null, null),
            "maintenance@assetiq.local");

        // var updated = await service.UpdateStatusAsync(ticket.Id, "Completed", "Bearing replaced.");
        var updated = await service.UpdateStatusAsync(
        ticket.Id,
        "Completed",
        "Bearing replaced.",
        "maintenance@assetiq.local");

        Assert.True(updated);
        var persisted = await context.MaintenanceRecords.SingleAsync();
        Assert.Equal("Completed", persisted.Status);
        Assert.Equal("Bearing replaced.", persisted.CompletionNotes);
        Assert.NotNull(persisted.CompletedAtUtc);
        Assert.NotNull(persisted.UpdatedAtUtc);
    }

    [Fact]
    public async Task CreateTicketRejectsMissingDescription()
    {
        await using var context = CreateContext();
        await CreateEquipmentAsync(context);
        var service = new MaintenanceTicketService(context, CreateAccessor());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateTicketAsync(
                new MaintenanceTicketCreateRequest("RM-1002", string.Empty, "High", null, null),
                "maintenance@assetiq.local"));
        Assert.Empty(context.MaintenanceRecords);
    }

    [Fact]
    public async Task CreateTicketRejectsUnpersistedEquipment()
    {
        await using var context = CreateContext();
        var service = new MaintenanceTicketService(context, CreateAccessor());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateTicketAsync(
                new MaintenanceTicketCreateRequest("RM-1002", "Inspect bearing", "High", null, null),
                "maintenance@assetiq.local"));
        Assert.Empty(context.MaintenanceRecords);
    }
}
