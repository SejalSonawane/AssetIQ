using AssetIQ.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetIQ.Data.Configurations;

public sealed class CsvImportBatchConfiguration : IEntityTypeConfiguration<CsvImportBatch>
{
    public void Configure(EntityTypeBuilder<CsvImportBatch> builder)
    {
        builder.ToTable("csv_import_batches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.SourceFileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.SourceFileSha256).HasMaxLength(64).IsRequired();
        builder.Property(x => x.MappingProfileKey).HasMaxLength(120).IsRequired();
        builder.Property(x => x.MappingProfileVersion).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
        builder.Property(x => x.StartedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CompletedAtUtc).HasColumnType("timestamp with time zone");
        builder.HasOne(x => x.Plant).WithMany(x => x.ImportBatches)
            .HasForeignKey(x => x.PlantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.PlantId, x.SourceFileSha256, x.MappingProfileKey, x.MappingProfileVersion })
            .IsUnique();
    }
}

public sealed class CsvImportBatchRowConfiguration : IEntityTypeConfiguration<CsvImportBatchRow>
{
    public void Configure(EntityTypeBuilder<CsvImportBatchRow> builder)
    {
        builder.ToTable("csv_import_batch_rows");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Classification).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
        builder.Property(x => x.ValidationMessage).HasMaxLength(2000);
        builder.HasOne(x => x.ImportBatch).WithMany(x => x.Rows)
            .HasForeignKey(x => x.ImportBatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ImportBatchId, x.SourceRowNumber }).IsUnique();
        builder.HasIndex(x => new { x.ImportBatchId, x.Classification });
    }
}