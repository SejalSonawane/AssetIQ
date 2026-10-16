using AssetIQ.Business.Interfaces;
using AssetIQ.Business.Services;
using AssetIQ.Data.Import;
using AssetIQ.Data.Options;
using Microsoft.Extensions.Options;

namespace AssetIQ.Tests;

public class RollingMillCsvIntegrationTests
{
    private static string GetCsvPath(string fileName = "AssetIQ_RollingMill_500Rows.csv")
    {
        var root = FindWorkspaceRoot();
        return Path.Combine(root, "data", "rolling-mill", fileName);
    }

    private static string FindWorkspaceRoot()
    {
        foreach (var startPath in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
        {
            var directory = new DirectoryInfo(startPath);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "AssetIQ.slnx")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not locate AssetIQ.slnx for CSV integration tests.");
    }

    [Fact]
    public async Task CsvReader_LoadsFiveHundredValidRows()
    {
        var path = GetCsvPath();
        Assert.True(File.Exists(path), $"CSV not found at {path}");

        var reader = new RollingMillCsvReader();
        var result = await reader.ReadAsync(path);

        Assert.True(result.FileFound);
        Assert.Equal(500, result.Records.Count);
        Assert.DoesNotContain(result.Issues, i => i.Severity == "Error");
    }

    [Fact]
    public async Task DatasetAccessor_ValidatesAndBuildsQualityReport()
    {
        var path = GetCsvPath();
        var options = Options.Create(new DatasetOptions
        {
            RollingMillCsvPath = path,
            PlantDisplayName = "Test dataset"
        });

        var accessor = new RollingMillDatasetAccessor(new RollingMillCsvReader(), options);
        var snapshot = await accessor.GetSnapshotAsync();

        Assert.True(snapshot.IsValid);
        Assert.Equal(500, snapshot.Equipment.Count);
        Assert.NotNull(snapshot.QualityReport);
        Assert.Equal(476, snapshot.QualityReport!.CriticalRiskCount);
        Assert.Equal(369, snapshot.QualityReport.ZeroHealthScoreCount);
        Assert.Equal(3, snapshot.QualityReport.UniqueDowntimeTierCount);
    }

    [Fact]
    public async Task DashboardService_UsesDatasetCounts()
    {
        var path = GetCsvPath();
        var options = Options.Create(new DatasetOptions { RollingMillCsvPath = path, PlantDisplayName = "Test dataset" });
        var accessor = new RollingMillDatasetAccessor(new RollingMillCsvReader(), options);
        // var service = new DashboardService(accessor);
        // var modelService = new RiskClassificationModelService();
        var modelService = new RiskClassificationModelService(accessor);
        var service = new DashboardService(accessor, modelService);

        var summary = await service.GetSummaryAsync();

        Assert.True(summary.IsDatasetValid);
        Assert.Equal(500, summary.TotalEquipment);
        Assert.Equal(476, summary.CriticalRiskCount);
        Assert.NotEmpty(summary.EnergyTrend);
    }

    [Fact]
    public async Task EquipmentService_QueriesImportedCsvRecords()
    {
        var path = GetCsvPath();
        var options = Options.Create(new DatasetOptions { RollingMillCsvPath = path, PlantDisplayName = "Test dataset" });
        var accessor = new RollingMillDatasetAccessor(new RollingMillCsvReader(), options);
        var service = new EquipmentService(accessor);

        var equipment = await service.GetEquipmentAsync(new EquipmentQuery());
        var criticalEquipment = await service.GetEquipmentAsync(new EquipmentQuery { RiskLevel = "Critical" });
        var record = await service.GetByCodeAsync("RM-1002");

        Assert.Equal(500, equipment.Count);
        Assert.Equal(476, criticalEquipment.Count);
        Assert.NotNull(record);
        Assert.Equal("Finishing", record.Section);
        Assert.Equal(0m, record.HealthScore);
    }

    [Fact]
    public async Task AlertService_MapsImportedCsvRecordsToDatasetAlerts()
    {
        var path = GetCsvPath();
        var options = Options.Create(new DatasetOptions { RollingMillCsvPath = path, PlantDisplayName = "Test dataset" });
        var accessor = new RollingMillDatasetAccessor(new RollingMillCsvReader(), options);
        var service = new AlertService(accessor);

        var alerts = await service.GetAlertsAsync();
        var alert = Assert.Single(alerts.Where(a => a.EquipmentCode == "RM-1002"));

        Assert.Equal(500, alerts.Count);
        Assert.Equal("Critical", alert.Severity);
        Assert.False(alert.IsConfirmedFailure);
        Assert.Contains("not a confirmed failure event", alert.Reason);
    }

    [Fact]
    public async Task DemoCsv_LoadsTwoThousandRowsAndFeedsAllAnalyticsServices()
    {
        var path = GetCsvPath("AssetIQ_RollingMill_2000Rows_Demo.csv");
        var readResult = await new RollingMillCsvReader().ReadAsync(path);

        Assert.True(readResult.FileFound);
        Assert.Equal(2000, readResult.Records.Count);
        Assert.DoesNotContain(readResult.Issues, issue => issue.Severity == "Error");

        var options = Options.Create(new DatasetOptions
        {
            RollingMillCsvPath = path,
            PlantDisplayName = "Rolling Mill demo CSV (500 original rows + 1,500 simulated demonstration rows)"
        });
        var accessor = new RollingMillDatasetAccessor(new RollingMillCsvReader(), options);
        var snapshot = await accessor.GetSnapshotAsync();

        Assert.True(snapshot.IsValid, string.Join("; ", snapshot.ValidationIssues.Select(issue => issue.Message)));
        Assert.Equal(2000, snapshot.Equipment.Count);
        Assert.Equal(701, snapshot.QualityReport!.CriticalRiskCount);
        Assert.Equal(540, snapshot.QualityReport.HighRiskCount);
        Assert.Equal(759, snapshot.QualityReport.MediumRiskCount);

        // var dashboard = await new DashboardService(accessor).GetSummaryAsync();
        // var modelService = new RiskClassificationModelService();
        var modelService = new RiskClassificationModelService(accessor);
        var dashboard = await new DashboardService(accessor, modelService).GetSummaryAsync();
        Assert.Equal(2000, dashboard.TotalEquipment);
        Assert.Equal(701, dashboard.CriticalRiskCount);
        Assert.Equal(540, dashboard.HighRiskCount);
        Assert.Equal(759, dashboard.MediumRiskCount);

        var energy = await new EnergyAnalyticsService(accessor).GetSummaryAsync();
        var expectedIntensity = Math.Round(
            snapshot.Equipment.Sum(e => e.EnergyKwh) / snapshot.Equipment.Sum(e => e.ProductionTons),
            2);
        Assert.Equal(2000, energy.ProductionVsEnergyPoints.Count);
        Assert.Equal(24580185.09m, energy.TotalEnergyKwh);
        Assert.Equal(280839.44m, energy.TotalProductionTons);
        Assert.Equal(expectedIntensity, energy.PlantIntensityKwhPerTon);

        // var predictive = await new PredictiveMaintenanceService(accessor).GetSummaryAsync();
        var predictive = await new PredictiveMaintenanceService(accessor, modelService).GetSummaryAsync();
        Assert.Equal(1241, predictive.FlaggedAssetCount);

        var failures = await new FailureAnalyticsService(accessor).GetSummaryAsync();
        Assert.Equal(4496m, failures.TotalFailureCountSum);
        Assert.Equal(4, failures.ImpactTiers.Count);

        var alerts = await new AlertService(accessor).GetAlertsAsync();
        Assert.Equal(2000, alerts.Count);

        var workOrders = await new MaintenanceWorkOrderService(accessor)
            .GetWorkOrdersAsync(new MaintenanceWorkOrderQuery());
        Assert.Equal(2000, workOrders.Count);
        Assert.Equal(701, workOrders.Count(order => order.Status == "Open — urgent (dataset)"));
        Assert.Equal(540, workOrders.Count(order => order.Status == "Planned (dataset)"));
        Assert.Equal(759, workOrders.Count(order => order.Status == "Monitoring (dataset)"));
    }
}
