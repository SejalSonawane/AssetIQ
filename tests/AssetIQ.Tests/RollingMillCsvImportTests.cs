using System.Text;
using AssetIQ.Business.Services;
using AssetIQ.Data;
using AssetIQ.Data.Entities;
using AssetIQ.Data.Import;
using AssetIQ.Data.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using AssetIQ.Business.Ml;

namespace AssetIQ.Tests;

public sealed class RollingMillCsvImportTests
{
    private static string GetCsvPath(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AssetIQ.slnx")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory!.FullName, "data", "rolling-mill", fileName);
    }

    private static AssetIQDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AssetIQDbContext>()
            .UseInMemoryDatabase($"csv_import_tests_{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AssetIQDbContext(options);
    }

    private static RollingMillDatasetAccessor CreateAccessor(string fileName, string plantDisplayName)
    {
        var options = Options.Create(new DatasetOptions
        {
            RollingMillCsvPath = GetCsvPath(fileName),
            PlantDisplayName = plantDisplayName
        });
        return new RollingMillDatasetAccessor(new RollingMillCsvReader(), options);
    }

    [Fact]
    public async Task ImportConfiguredCsvPersistsEquipmentAndBatchMetadata()
    {
        await using var context = CreateContext();
        var accessor = CreateAccessor("AssetIQ_RollingMill_500Rows.csv", "Test Rolling Mill");
        var service = new RollingMillCsvImportService(context, accessor, Options.Create(new DatasetOptions { RollingMillCsvPath = GetCsvPath("AssetIQ_RollingMill_500Rows.csv"), PlantDisplayName = "Test Rolling Mill" }));

        var result = await service.ImportConfiguredCsvAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(500, result.TotalRowCount);
        Assert.Equal(500, result.AcceptedRowCount);
        Assert.Equal(500, await context.Equipment.CountAsync());
        Assert.Equal(500, await context.CsvImportBatchRows.CountAsync());
        Assert.Single(await context.CsvImportBatches.ToListAsync());
        var plant = await context.Plants.SingleAsync();
        var equipment = await context.Equipment.Include(e => e.EquipmentType).SingleAsync(e => e.EquipmentCode == "RM-1002");
        Assert.Equal("Test Rolling Mill", plant.PlantCode);
        Assert.Equal("Coiler", equipment.EquipmentType!.Key);
        Assert.Equal("Unknown", equipment.Criticality);
        Assert.Equal("Unknown", equipment.OperatingStatus);
        Assert.NotNull(equipment.SourceRowId);
    }

    [Fact]
    public async Task RepeatImportIsIdempotentAndPreservesEquipmentIds()
    {
        await using var context = CreateContext();
        var accessor = CreateAccessor("AssetIQ_RollingMill_500Rows.csv", "Test Rolling Mill");
        var options = Options.Create(new DatasetOptions { RollingMillCsvPath = GetCsvPath("AssetIQ_RollingMill_500Rows.csv"), PlantDisplayName = "Test Rolling Mill" });
        var service = new RollingMillCsvImportService(context, accessor, options);

        Assert.True((await service.ImportConfiguredCsvAsync()).Succeeded);
        var firstEquipment = await context.Equipment.SingleAsync(e => e.EquipmentCode == "RM-1002");
        var firstId = firstEquipment.Id;

        var second = await service.ImportConfiguredCsvAsync();

        Assert.True(second.Succeeded);
        Assert.Contains("already been imported", second.Message);
        Assert.Equal(1, await context.CsvImportBatches.CountAsync());
        Assert.Equal(500, await context.Equipment.CountAsync());
        Assert.Equal(firstId, (await context.Equipment.SingleAsync(e => e.EquipmentCode == "RM-1002")).Id);
    }

    [Fact]
    public async Task InvalidConfiguredCsvRejectsImportWithoutDatabaseChanges()
    {
        await using var context = CreateContext();
        var tempPath = Path.Combine(Path.GetTempPath(), $"assetiq-invalid-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(tempPath, "Timestamp,Equipment_ID\n");
        var options = Options.Create(new DatasetOptions { RollingMillCsvPath = tempPath, PlantDisplayName = "Invalid dataset" });
        var accessor = new RollingMillDatasetAccessor(new RollingMillCsvReader(), options);
        var service = new RollingMillCsvImportService(context, accessor, options);

        var result = await service.ImportConfiguredCsvAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(0, await context.Equipment.CountAsync());
        Assert.Equal(0, await context.CsvImportBatches.CountAsync());
        Assert.Equal(0, await context.CsvImportBatchRows.CountAsync());
        File.Delete(tempPath);
    }

    [Fact]
    public async Task ImportedEquipmentCanSupportMaintenanceTicketCreation()
    {
        await using var context = CreateContext();
        var accessor = CreateAccessor("AssetIQ_RollingMill_500Rows.csv", "Test Rolling Mill");
        var options = Options.Create(new DatasetOptions { RollingMillCsvPath = GetCsvPath("AssetIQ_RollingMill_500Rows.csv"), PlantDisplayName = "Test Rolling Mill" });
        var importService = new RollingMillCsvImportService(context, accessor, options);

        Assert.True((await importService.ImportConfiguredCsvAsync()).Succeeded);
        var ticketService = new MaintenanceTicketService(context, accessor);
        var ticket = await ticketService.CreateTicketAsync(
            new AssetIQ.Business.DTOs.MaintenanceTicketCreateRequest("RM-1002", "Inspect bearing", "High", "maintenance@assetiq.local", null),
            "maintenance@assetiq.local");

        var persistedTicket = await context.MaintenanceRecords.SingleAsync();
        var equipment = await context.Equipment.SingleAsync(e => e.EquipmentCode == "RM-1002");
        Assert.Equal(equipment.Id, persistedTicket.EquipmentId);
        Assert.Equal(equipment.Id, ticket.Id != Guid.Empty ? persistedTicket.EquipmentId : Guid.Empty);
    }
}
