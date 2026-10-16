using AssetIQ.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetIQ.Data.Configurations;

public sealed class PlantConfiguration : IEntityTypeConfiguration<Plant>
{
    public void Configure(EntityTypeBuilder<Plant> builder)
    {
        builder.ToTable("plants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.PlantCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.HasIndex(x => x.PlantCode).IsUnique();
    }
}

public sealed class EquipmentTypeConfiguration : IEntityTypeConfiguration<EquipmentType>
{
    public void Configure(EntityTypeBuilder<EquipmentType> builder)
    {
        builder.ToTable("equipment_types");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Key).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasIndex(x => x.Key).IsUnique();
    }
}

public sealed class MeasurementDefinitionConfiguration : IEntityTypeConfiguration<MeasurementDefinition>
{
    public void Configure(EntityTypeBuilder<MeasurementDefinition> builder)
    {
        builder.ToTable("measurement_definitions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Key).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PhysicalDimension).HasMaxLength(80).IsRequired();
        builder.Property(x => x.CanonicalUnit).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasIndex(x => x.Key).IsUnique();
    }
}

public sealed class EquipmentTypeMeasurementConfiguration : IEntityTypeConfiguration<EquipmentTypeMeasurement>
{
    public void Configure(EntityTypeBuilder<EquipmentTypeMeasurement> builder)
    {
        builder.ToTable("equipment_type_measurements");
        builder.HasKey(x => new { x.EquipmentTypeId, x.MeasurementDefinitionId });
        builder.HasOne(x => x.EquipmentType)
            .WithMany(x => x.Measurements)
            .HasForeignKey(x => x.EquipmentTypeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.MeasurementDefinition)
            .WithMany(x => x.EquipmentTypeAssociations)
            .HasForeignKey(x => x.MeasurementDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EquipmentTypeId, x.DisplayOrder });
        builder.Property(x => x.DisplayOrder).IsRequired();
    }
}

public sealed class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.ToTable("equipment");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.EquipmentCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Section).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Criticality).HasMaxLength(40).IsRequired();
        builder.Property(x => x.OperatingStatus).HasMaxLength(40).IsRequired();
        builder.Property(x => x.RiskLevel).HasMaxLength(40);
        builder.Property(x => x.HealthScore).HasPrecision(8, 3);
        builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.HasOne(x => x.Plant).WithMany(x => x.Equipment)
            .HasForeignKey(x => x.PlantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.EquipmentType).WithMany(x => x.Equipment)
            .HasForeignKey(x => x.EquipmentTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SourceRow).WithMany()
            .HasForeignKey(x => x.SourceRowId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(x => new { x.Id, x.EquipmentTypeId });
        builder.HasIndex(x => new { x.PlantId, x.EquipmentCode }).IsUnique();
        builder.HasIndex(x => new { x.EquipmentTypeId, x.PlantId });
        builder.HasIndex(x => x.SourceRowId).IsUnique()
            .HasFilter("\"SourceRowId\" IS NOT NULL");
    }
}