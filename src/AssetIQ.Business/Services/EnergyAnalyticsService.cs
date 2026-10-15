using AssetIQ.Business.DTOs;
using AssetIQ.Business.Interfaces;

namespace AssetIQ.Business.Services;

public sealed class EnergyAnalyticsService : IEnergyAnalyticsService
{
    private readonly IRollingMillDatasetAccessor _datasetAccessor;

    public EnergyAnalyticsService(IRollingMillDatasetAccessor datasetAccessor)
    {
        _datasetAccessor = datasetAccessor;
    }

    public async Task<EnergyAnalyticsSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        if (!snapshot.IsValid || snapshot.Equipment.Count == 0)
        {
            return new EnergyAnalyticsSummaryDto
            {
                IsDatasetValid = snapshot.IsValid,
                DataSourceLabel = snapshot.SourceLabel
            };
        }

        var equipment = snapshot.Equipment;
        var totalEnergy = equipment.Sum(e => e.EnergyKwh);
        var totalProduction = equipment.Sum(e => e.ProductionTons);
        decimal? plantIntensity = totalProduction > 0 ? totalEnergy / totalProduction : null;

        var energyBySection = equipment
            .GroupBy(e => e.Section, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Sum(x => x.EnergyKwh))
            .Select(g => new GroupMetricDto
            {
                Label = g.Key,
                Value = Math.Round(g.Sum(x => x.EnergyKwh), 2),
                Unit = "kWh (sum per section)",
                Count = g.Count()
            })
            .ToList();

        var energyByType = equipment
            .GroupBy(e => e.EquipmentTypeKey, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Sum(x => x.EnergyKwh))
            .Select(g => new GroupMetricDto
            {
                Label = g.Key,
                Value = Math.Round(g.Sum(x => x.EnergyKwh), 2),
                Unit = "kWh (sum per type)",
                Count = g.Count()
            })
            .ToList();

        var intensityBySection = equipment
            .GroupBy(e => e.Section, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var energy = g.Sum(x => x.EnergyKwh);
                var production = g.Sum(x => x.ProductionTons);
                var intensity = production > 0 ? energy / production : 0;
                return new GroupMetricDto
                {
                    Label = g.Key,
                    Value = Math.Round(intensity, 2),
                    Unit = "kWh/ton (section total kWh ÷ total tons)",
                    Count = g.Count()
                };
            })
            .OrderByDescending(x => x.Value)
            .ToList();

        var scatter = equipment
            .Select(e => new ProductionEnergyPointDto
            {
                EquipmentCode = e.EquipmentCode,
                ProductionTons = e.ProductionTons,
                EnergyKwh = e.EnergyKwh,
                Section = e.Section
            })
            .ToList();

        var topConsumers = equipment
            .OrderByDescending(e => e.EnergyKwh)
            .Take(20)
            .Select(e => new EquipmentEnergyRowDto
            {
                EquipmentCode = e.EquipmentCode,
                EquipmentTypeKey = e.EquipmentTypeKey,
                Section = e.Section,
                EnergyKwh = e.EnergyKwh,
                ProductionTons = e.ProductionTons,
                EnergyIntensityKwhPerTon = e.EnergyIntensityKwhPerTon,
                PowerKw = e.PowerKw
            })
            .ToList();

        return new EnergyAnalyticsSummaryDto
        {
            IsDatasetValid = true,
            DataSourceLabel = snapshot.SourceLabel,
            TotalEnergyKwh = totalEnergy,
            TotalProductionTons = totalProduction,
            PlantIntensityKwhPerTon = plantIntensity.HasValue ? Math.Round(plantIntensity.Value, 2) : null,
            EnergyKwhBySection = energyBySection,
            EnergyKwhByEquipmentType = energyByType,
            AvgIntensityBySection = intensityBySection,
            ProductionVsEnergyPoints = scatter,
            TopEnergyConsumers = topConsumers
        };
    }
}
