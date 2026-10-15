using AssetIQ.Business.Interfaces;
using AssetIQ.Business.Services;
using AssetIQ.Data.Import;
using AssetIQ.Data.Interfaces;
using AssetIQ.Data.Options;
using AssetIQ.Data.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssetIQ.Business.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAssetIQBusiness(
        this IServiceCollection services,
        IConfiguration configuration,
        string rollingMillCsvAbsolutePath)
    {
        services.Configure<DatasetOptions>(options =>
        {
            configuration.GetSection(DatasetOptions.SectionName).Bind(options);
            options.RollingMillCsvPath = rollingMillCsvAbsolutePath;
        });

        services.AddSingleton<IRollingMillCsvReader, RollingMillCsvReader>();
        services.AddSingleton<IRollingMillDatasetAccessor, RollingMillDatasetAccessor>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IEquipmentService, EquipmentService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IEnergyAnalyticsService, EnergyAnalyticsService>();
        services.AddScoped<IPredictiveMaintenanceService, PredictiveMaintenanceService>();
        services.AddScoped<IFailureAnalyticsService, FailureAnalyticsService>();
        services.AddScoped<IMaintenanceWorkOrderService, MaintenanceWorkOrderService>();
        services.AddSingleton<IMaintenanceActionService, MaintenanceActionService>();
        services.AddSingleton<IEquipmentRepository, InMemoryEquipmentRepository>();

        return services;
    }
}
