using AssetIQ.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetIQ.Data.Configurations;

public sealed class SensorReadingConfiguration : IEntityTypeConfiguration<SensorReading>
{
    public void Configure(EntityTypeBuilder<SensorReading> builder)
    {
        builder.ToTable("sensor_readings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Value).HasPrecision(20, 6).IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(40);
        builder.Property(x => x.RecordedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.HasOne(x => x.Equipment).WithMany()
            .HasForeignKey(x => new { x.EquipmentId, x.EquipmentTypeId })
            .HasPrincipalKey(x => new { x.Id, x.EquipmentTypeId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.MeasurementDefinition).WithMany(x => x.SensorReadings)
            .HasForeignKey(x => x.MeasurementDefinitionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EquipmentTypeMeasurement>().WithMany()
            .HasForeignKey(x => new { x.EquipmentTypeId, x.MeasurementDefinitionId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SourceRow).WithMany()
            .HasForeignKey(x => x.SourceRowId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EquipmentId, x.MeasurementDefinitionId, x.RecordedAtUtc });
        builder.HasIndex(x => new { x.SourceRowId, x.MeasurementDefinitionId }).IsUnique()
            .HasFilter("\"SourceRowId\" IS NOT NULL");
    }
}

public sealed class EnergyRecordConfiguration : IEntityTypeConfiguration<EnergyRecord>
{
    public void Configure(EntityTypeBuilder<EnergyRecord> builder)
    {
        builder.ToTable("energy_records", table =>
            table.HasCheckConstraint("ck_energy_records_period_order", "\"PeriodEndUtc\" > \"PeriodStartUtc\""));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.PowerKw).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.EnergyKwh).HasPrecision(20, 6).IsRequired();
        builder.Property(x => x.PeriodStartUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.PeriodEndUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ProductionOutput).HasPrecision(20, 6);
        builder.Property(x => x.ProductionUnit).HasMaxLength(40);
        builder.HasOne(x => x.Equipment).WithMany()
            .HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SourceRow).WithMany()
            .HasForeignKey(x => x.SourceRowId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EquipmentId, x.PeriodStartUtc, x.PeriodEndUtc });
        builder.HasIndex(x => x.SourceRowId).IsUnique()
            .HasFilter("\"SourceRowId\" IS NOT NULL");
    }
}

public sealed class FailureRecordConfiguration : IEntityTypeConfiguration<FailureRecord>
{
    public void Configure(EntityTypeBuilder<FailureRecord> builder)
    {
        builder.ToTable("failure_records");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.FailureMode).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OccurredAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.DowntimeHours).HasPrecision(18, 4);
        builder.Property(x => x.EstimatedCostImpact).HasPrecision(20, 2);
        builder.HasOne(x => x.Equipment).WithMany()
            .HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SourceRow).WithMany()
            .HasForeignKey(x => x.SourceRowId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EquipmentId, x.OccurredAtUtc });
    }
}

public sealed class MaintenanceRecordConfiguration : IEntityTypeConfiguration<MaintenanceRecord>
{
    public void Configure(EntityTypeBuilder<MaintenanceRecord> builder)
    {
        builder.ToTable("maintenance_records");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Priority).HasMaxLength(40).IsRequired();
        builder.Property(x => x.AssignedTo).HasMaxLength(200);
        builder.Property(x => x.CompletionNotes).HasMaxLength(1000);
        builder.Property(x => x.Source).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ScheduledAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CompletedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.HasOne(x => x.Equipment).WithMany()
            .HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SourceRow).WithMany()
            .HasForeignKey(x => x.SourceRowId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EquipmentId, x.Status });
    }
}

public sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("alerts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Severity).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.RecommendedNextStep).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
        builder.Property(x => x.RaisedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.HasOne(x => x.Equipment).WithMany()
            .HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SourceRow).WithMany()
            .HasForeignKey(x => x.SourceRowId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EquipmentId, x.RaisedAtUtc });
        builder.HasIndex(x => x.SourceRowId);
    }
}

public sealed class RecommendedActionConfiguration : IEntityTypeConfiguration<RecommendedAction>
{
    public void Configure(EntityTypeBuilder<RecommendedAction> builder)
    {
        builder.ToTable("recommended_actions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.ActionType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Priority).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.HasOne(x => x.Equipment).WithMany()
            .HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SourceRow).WithMany()
            .HasForeignKey(x => x.SourceRowId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EquipmentId, x.CreatedAtUtc });
    }
}

public sealed class PredictionResultConfiguration : IEntityTypeConfiguration<PredictionResult>
{
    public void Configure(EntityTypeBuilder<PredictionResult> builder)
    {
        builder.ToTable("prediction_results", table =>
        {
            table.HasCheckConstraint(
                "ck_prediction_results_has_output",
                "\"PredictedClass\" IS NOT NULL OR \"PredictedValue\" IS NOT NULL");
            table.HasCheckConstraint(
                "ck_prediction_results_probability_range",
                "\"Probability\" IS NULL OR (\"Probability\" >= 0 AND \"Probability\" <= 1)");
            table.HasCheckConstraint(
                "ck_prediction_results_nonnegative_horizon",
                "\"PredictionHorizonSeconds\" IS NULL OR \"PredictionHorizonSeconds\" >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.ModelIdentifier).HasMaxLength(160).IsRequired();
        builder.Property(x => x.ModelVersion).HasMaxLength(80).IsRequired();
        builder.Property(x => x.TargetKey).HasMaxLength(120).IsRequired();
        builder.Property(x => x.PredictedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.PredictedClass).HasMaxLength(120);
        builder.Property(x => x.PredictedValue).HasPrecision(20, 6);
        builder.Property(x => x.Score).HasPrecision(20, 8);
        builder.Property(x => x.Probability).HasPrecision(9, 8);
        builder.Property(x => x.InputDataMetadataJson).HasColumnType("jsonb");
        builder.Property(x => x.ExplanationMetadataJson).HasColumnType("jsonb");
        builder.HasOne(x => x.Equipment).WithMany()
            .HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EquipmentId, x.TargetKey, x.PredictedAtUtc });
        builder.HasIndex(x => new { x.ModelIdentifier, x.ModelVersion, x.PredictedAtUtc });
    }
}