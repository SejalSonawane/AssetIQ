using AssetIQ.Business.DTOs;
using AssetIQ.Data.Import;

namespace AssetIQ.Business.Mapping;

public static class RollingMillRecordMapper
{
    public static EquipmentRecordDto ToEquipmentRecord(RollingMillCsvRecord record, string plantDisplayName) =>
        new()
        {
            SourceRowNumber = record.SourceRowNumber,
            EquipmentCode = record.EquipmentId,
            EquipmentTypeKey = record.EquipmentType,
            Section = record.Section,
            PlantName = plantDisplayName,
            ObservedAtLocal = record.TimestampLocal,
            HealthScore = record.HealthScore,
            RiskLevel = record.RiskLevel,
            MaintenanceRecommendation = record.MaintenanceFlag,
            MotorTempC = record.MotorTempC,
            BearingTempC = record.BearingTempC,
            VibrationMmS = record.VibrationMmS,
            MotorCurrentA = record.MotorCurrentA,
            PowerKw = record.PowerKw,
            EnergyKwh = record.EnergyKwh,
            HydraulicPressureBar = record.HydraulicPressureBar,
            OilPressureBar = record.OilPressureBar,
            RollSpeedMpm = record.RollSpeedMpm,
            RollForceKn = record.RollForceKn,
            OperatingHours = record.OperatingHours,
            ProductionTons = record.ProductionTons,
            MaintenanceDaysAgo = record.MaintenanceDaysAgo,
            FailureCountLastYear = record.FailureCountLastYear,
            EnergyIntensityKwhPerTon = record.EnergyIntensity,
            EstimatedDowntimeHours = record.EstimatedDowntimeHours,
            EstimatedCostImpactInr = record.EstimatedCostImpactInr
        };
}
