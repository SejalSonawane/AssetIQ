namespace AssetIQ.Data.Import;

/// <summary>
/// Expected headers for the Rolling Mill proof-of-concept CSV. Plant-specific import schema only.
/// </summary>
public static class RollingMillCsvColumnNames
{
    public static readonly string[] All =
    [
        "Timestamp",
        "Equipment_ID",
        "Equipment_Type",
        "Section",
        "Motor_Temp_C",
        "Bearing_Temp_C",
        "Vibration_mm_s",
        "Motor_Current_A",
        "Power_kW",
        "Energy_kWh",
        "Hydraulic_Pressure_bar",
        "Oil_Pressure_bar",
        "Roll_Speed_mpm",
        "Roll_Force_kN",
        "Operating_Hours",
        "Production_Tons",
        "Maintenance_Days_Ago",
        "Failure_Count_Last_Year",
        "Energy_Intensity",
        "Health_Score",
        "Risk_Level",
        "Maintenance_Flag",
        "Estimated_Downtime_Hours",
        "Estimated_Cost_Impact_INR"
    ];
}
