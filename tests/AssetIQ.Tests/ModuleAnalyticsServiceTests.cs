using AssetIQ.Business.Interfaces;
using AssetIQ.Business.Services;
using AssetIQ.Data.Import;
using AssetIQ.Data.Options;
using Microsoft.Extensions.Options;

namespace AssetIQ.Tests;

public class ModuleAnalyticsServiceTests
{
    private static RollingMillDatasetAccessor CreateAccessor()
    {
        var root = FindWorkspaceRoot();
        var path = Path.Combine(root, "data", "rolling-mill", "AssetIQ_RollingMill_500Rows.csv");
        var options = Options.Create(new DatasetOptions { RollingMillCsvPath = path, PlantDisplayName = "Test dataset" });
        return new RollingMillDatasetAccessor(new RollingMillCsvReader(), options);
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

        throw new DirectoryNotFoundException("Could not locate AssetIQ.slnx for CSV analytics tests.");
    }

    [Fact]
    public async Task EnergyAnalyticsService_AggregatesEnergyBySection()
    {
        var accessor = CreateAccessor();
        var service = new EnergyAnalyticsService(accessor);
        var summary = await service.GetSummaryAsync();

        Assert.True(summary.IsDatasetValid);
        Assert.Equal(500, summary.ProductionVsEnergyPoints.Count);
        Assert.Equal(5, summary.EnergyKwhBySection.Count);
        Assert.True(summary.TotalEnergyKwh > 0);
        Assert.NotNull(summary.PlantIntensityKwhPerTon);
    }

    [Fact]
    public async Task PredictiveMaintenanceService_FlagsNonMonitorAssets()
    {
        var accessor = CreateAccessor();
        var ml = new RiskClassificationModelService(accessor);
        var service = new PredictiveMaintenanceService(accessor, ml);
        var summary = await service.GetSummaryAsync();

        Assert.True(summary.IsDatasetValid);
        Assert.Equal(491, summary.FlaggedAssetCount);
        Assert.Equal(500, summary.TotalAssets);
        Assert.All(summary.FlaggedAssets, a => Assert.NotEqual("Monitor", a.Equipment.MaintenanceRecommendation));
        Assert.Contains(summary.FlaggedAssets, a => a.Equipment.EquipmentCode == "RM-1002");
        Assert.True(summary.ModelEvaluation.IsAvailable);
        Assert.Equal(500, summary.MachineLearningAgreementCount + summary.MachineLearningDisagreementCount);
    }

    [Fact]
    public async Task RiskClassificationModelService_TrainsAndEvaluatesOn500RowCsv()
    {
        var accessor = CreateAccessor();
        var service = new RiskClassificationModelService(accessor);
        var evaluation = await service.GetEvaluationAsync();

        Assert.True(evaluation.IsAvailable);
        Assert.True(evaluation.TrainingRowCount > 0);
        Assert.True(evaluation.HoldoutRowCount > 0);
        Assert.InRange(evaluation.MacroAccuracy, 0, 1);
        Assert.NotEmpty(evaluation.FeatureKeys);
    }

    [Fact]
    public async Task FailureAnalyticsService_ReportsThreeImpactTiers()
    {
        var accessor = CreateAccessor();
        var service = new FailureAnalyticsService(accessor);
        var summary = await service.GetSummaryAsync();

        Assert.True(summary.IsDatasetValid);
        Assert.Equal(3, summary.ImpactTiers.Count);
        Assert.Equal(10, summary.FailureCountSumByEquipmentType.Count);
        Assert.True(summary.TotalFailureCountSum > 0);
    }

    [Fact]
    public async Task MaintenanceWorkOrderService_MapsStatusesAndFilters()
    {
        var accessor = CreateAccessor();
        var service = new MaintenanceWorkOrderService(accessor);

        var all = await service.GetWorkOrdersAsync(new MaintenanceWorkOrderQuery());
        var monitoring = await service.GetWorkOrdersAsync(new MaintenanceWorkOrderQuery { Status = "Monitoring (dataset)" });

        Assert.Equal(500, all.Count);
        Assert.Equal(9, monitoring.Count);
        Assert.Equal("Open — urgent (dataset)", MaintenanceWorkOrderService.MapStatus("Immediate Maintenance"));
        Assert.All(all, wo => Assert.StartsWith("WO-", wo.WorkOrderId));
    }

    [Fact]
    public async Task MaintenanceActionService_CreatesAndCompletesApplicationAction()
    {
        var service = new MaintenanceActionService(CreateAccessor());

        var action = await service.CreateActionAsync("RM-1002", "Inspect bearing assembly");
        var storedActions = await service.GetActionsAsync();
        var completed = await service.CompleteActionAsync(action.Id);
        var completedAction = Assert.Single(await service.GetActionsAsync());

        Assert.Equal("RM-1002", action.EquipmentCode);
        Assert.Equal("Open", action.Status);
        Assert.Equal("Inspect bearing assembly", action.Description);
        Assert.Single(storedActions);
        Assert.True(completed);
        Assert.Equal("Completed", completedAction.Status);
        Assert.NotNull(completedAction.CompletedAtLocal);
        Assert.False(await service.CompleteActionAsync(action.Id));
    }

    [Fact]
    public async Task MaintenanceActionService_RejectsUnknownEquipment()
    {
        var service = new MaintenanceActionService(CreateAccessor());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateActionAsync("UNKNOWN", "Inspect equipment"));
        Assert.Empty(await service.GetActionsAsync());
    }
}
