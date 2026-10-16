using AssetIQ.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AssetIQ.Data;

public sealed class AssetIQDbContext(DbContextOptions<AssetIQDbContext> options)
    : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Plant> Plants => Set<Plant>();
    public DbSet<EquipmentType> EquipmentTypes => Set<EquipmentType>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<MeasurementDefinition> MeasurementDefinitions => Set<MeasurementDefinition>();
    public DbSet<EquipmentTypeMeasurement> EquipmentTypeMeasurements => Set<EquipmentTypeMeasurement>();
    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();
    public DbSet<EnergyRecord> EnergyRecords => Set<EnergyRecord>();
    public DbSet<FailureRecord> FailureRecords => Set<FailureRecord>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<RecommendedAction> RecommendedActions => Set<RecommendedAction>();
    public DbSet<PredictionResult> PredictionResults => Set<PredictionResult>();
    public DbSet<CsvImportBatch> CsvImportBatches => Set<CsvImportBatch>();
    public DbSet<CsvImportBatchRow> CsvImportBatchRows => Set<CsvImportBatchRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssetIQDbContext).Assembly);
    }
}