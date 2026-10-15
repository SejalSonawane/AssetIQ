using System.Globalization;
using AssetIQ.Data.Interfaces;

namespace AssetIQ.Data.Import;

public sealed class RollingMillCsvReader : IRollingMillCsvReader
{
    private static readonly string[] TimestampFormats =
    [
        "yyyy-MM-dd HH:mm",
        "yyyy-MM-dd H:mm",
        "dd-MM-yyyy HH:mm",
        "dd-MM-yyyy H:mm"
    ];

    public async Task<RollingMillCsvReadResult> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return new RollingMillCsvReadResult
            {
                FilePath = filePath,
                FileFound = false,
                Issues =
                [
                    new CsvReadIssue
                    {
                        Message = $"CSV file not found at '{filePath}'.",
                        Severity = "Error"
                    }
                ]
            };
        }

        var issues = new List<CsvReadIssue>();
        var records = new List<RollingMillCsvRecord>();

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream);

        var headerLine = await reader.ReadLineAsync(cancellationToken);
        if (headerLine is null)
        {
            issues.Add(new CsvReadIssue { Message = "CSV file is empty.", Severity = "Error" });
            return BuildResult(filePath, records, issues);
        }

        var headers = SplitCsvLine(headerLine);
        ValidateHeaders(headers, issues);
        if (issues.Any(i => i.Severity == "Error"))
        {
            return BuildResult(filePath, records, issues);
        }

        var headerIndex = headers
            .Select((name, index) => (name, index))
            .ToDictionary(x => x.name, x => x.index, StringComparer.Ordinal);

        string? line;
        var rowNumber = 1;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            rowNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = SplitCsvLine(line);
            if (fields.Length != headers.Length)
            {
                issues.Add(new CsvReadIssue
                {
                    RowNumber = rowNumber,
                    Message = $"Expected {headers.Length} columns but found {fields.Length}.",
                    Severity = "Error"
                });
                continue;
            }

            if (!TryParseRecord(rowNumber, fields, headerIndex, issues, out var record))
            {
                continue;
            }

            records.Add(record);
        }

        return BuildResult(filePath, records, issues);
    }

    private static RollingMillCsvReadResult BuildResult(
        string filePath,
        List<RollingMillCsvRecord> records,
        List<CsvReadIssue> issues)
    {
        return new RollingMillCsvReadResult
        {
            FilePath = filePath,
            FileFound = true,
            Records = records,
            Issues = issues
        };
    }

    private static void ValidateHeaders(IReadOnlyList<string> headers, ICollection<CsvReadIssue> issues)
    {
        if (headers.Count != RollingMillCsvColumnNames.All.Length)
        {
            issues.Add(new CsvReadIssue
            {
                Message = $"Expected {RollingMillCsvColumnNames.All.Length} columns but header row has {headers.Count}.",
                Severity = "Error"
            });
        }

        for (var i = 0; i < RollingMillCsvColumnNames.All.Length; i++)
        {
            if (i >= headers.Count)
            {
                break;
            }

            if (!string.Equals(headers[i], RollingMillCsvColumnNames.All[i], StringComparison.Ordinal))
            {
                issues.Add(new CsvReadIssue
                {
                    Field = RollingMillCsvColumnNames.All[i],
                    Message = $"Header mismatch at column {i + 1}: expected '{RollingMillCsvColumnNames.All[i]}', found '{headers[i]}'.",
                    Severity = "Error"
                });
            }
        }
    }

    private static bool TryParseRecord(
        int rowNumber,
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, int> headerIndex,
        ICollection<CsvReadIssue> issues,
        out RollingMillCsvRecord record)
    {
        record = null!;

        if (!TryGetString(fields, headerIndex, "Timestamp", rowNumber, issues, out var timestampRaw) ||
            !TryGetString(fields, headerIndex, "Equipment_ID", rowNumber, issues, out var equipmentId) ||
            !TryGetString(fields, headerIndex, "Equipment_Type", rowNumber, issues, out var equipmentType) ||
            !TryGetString(fields, headerIndex, "Section", rowNumber, issues, out var section) ||
            !TryGetString(fields, headerIndex, "Risk_Level", rowNumber, issues, out var riskLevel) ||
            !TryGetString(fields, headerIndex, "Maintenance_Flag", rowNumber, issues, out var maintenanceFlag))
        {
            return false;
        }

        if (!DateTime.TryParseExact(
                timestampRaw.Trim(),
                TimestampFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var timestamp))
        {
            issues.Add(new CsvReadIssue
            {
                RowNumber = rowNumber,
                Field = "Timestamp",
                Message = $"Invalid timestamp '{timestampRaw}'.",
                Severity = "Error"
            });
            return false;
        }

        if (string.IsNullOrWhiteSpace(equipmentId))
        {
            issues.Add(new CsvReadIssue
            {
                RowNumber = rowNumber,
                Field = "Equipment_ID",
                Message = "Equipment_ID is required.",
                Severity = "Error"
            });
            return false;
        }

        var numericFields = new (string Name, decimal Value)[0];
        try
        {
            numericFields =
            [
                ("Motor_Temp_C", ParseDecimal(fields, headerIndex, "Motor_Temp_C", rowNumber, issues)),
                ("Bearing_Temp_C", ParseDecimal(fields, headerIndex, "Bearing_Temp_C", rowNumber, issues)),
                ("Vibration_mm_s", ParseDecimal(fields, headerIndex, "Vibration_mm_s", rowNumber, issues)),
                ("Motor_Current_A", ParseDecimal(fields, headerIndex, "Motor_Current_A", rowNumber, issues)),
                ("Power_kW", ParseDecimal(fields, headerIndex, "Power_kW", rowNumber, issues)),
                ("Energy_kWh", ParseDecimal(fields, headerIndex, "Energy_kWh", rowNumber, issues)),
                ("Hydraulic_Pressure_bar", ParseDecimal(fields, headerIndex, "Hydraulic_Pressure_bar", rowNumber, issues)),
                ("Oil_Pressure_bar", ParseDecimal(fields, headerIndex, "Oil_Pressure_bar", rowNumber, issues)),
                ("Roll_Speed_mpm", ParseDecimal(fields, headerIndex, "Roll_Speed_mpm", rowNumber, issues)),
                ("Roll_Force_kN", ParseDecimal(fields, headerIndex, "Roll_Force_kN", rowNumber, issues)),
                ("Operating_Hours", ParseDecimal(fields, headerIndex, "Operating_Hours", rowNumber, issues)),
                ("Production_Tons", ParseDecimal(fields, headerIndex, "Production_Tons", rowNumber, issues)),
                ("Maintenance_Days_Ago", ParseDecimal(fields, headerIndex, "Maintenance_Days_Ago", rowNumber, issues)),
                ("Failure_Count_Last_Year", ParseDecimal(fields, headerIndex, "Failure_Count_Last_Year", rowNumber, issues)),
                ("Energy_Intensity", ParseDecimal(fields, headerIndex, "Energy_Intensity", rowNumber, issues)),
                ("Health_Score", ParseDecimal(fields, headerIndex, "Health_Score", rowNumber, issues)),
                ("Estimated_Downtime_Hours", ParseDecimal(fields, headerIndex, "Estimated_Downtime_Hours", rowNumber, issues)),
                ("Estimated_Cost_Impact_INR", ParseDecimal(fields, headerIndex, "Estimated_Cost_Impact_INR", rowNumber, issues))
            ];
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        if (issues.Any(i => i.RowNumber == rowNumber && i.Severity == "Error"))
        {
            return false;
        }

        record = new RollingMillCsvRecord
        {
            SourceRowNumber = rowNumber,
            TimestampLocal = timestamp,
            EquipmentId = equipmentId.Trim(),
            EquipmentType = equipmentType.Trim(),
            Section = section.Trim(),
            MotorTempC = numericFields[0].Value,
            BearingTempC = numericFields[1].Value,
            MotorCurrentA = numericFields[3].Value,
            VibrationMmS = numericFields[2].Value,
            PowerKw = numericFields[4].Value,
            EnergyKwh = numericFields[5].Value,
            HydraulicPressureBar = numericFields[6].Value,
            OilPressureBar = numericFields[7].Value,
            RollSpeedMpm = numericFields[8].Value,
            RollForceKn = numericFields[9].Value,
            OperatingHours = numericFields[10].Value,
            ProductionTons = numericFields[11].Value,
            MaintenanceDaysAgo = numericFields[12].Value,
            FailureCountLastYear = numericFields[13].Value,
            EnergyIntensity = numericFields[14].Value,
            HealthScore = numericFields[15].Value,
            RiskLevel = riskLevel.Trim(),
            MaintenanceFlag = maintenanceFlag.Trim(),
            EstimatedDowntimeHours = numericFields[16].Value,
            EstimatedCostImpactInr = numericFields[17].Value
        };

        return true;
    }

    private static decimal ParseDecimal(
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, int> headerIndex,
        string fieldName,
        int rowNumber,
        ICollection<CsvReadIssue> issues)
    {
        if (!TryGetString(fields, headerIndex, fieldName, rowNumber, issues, out var raw))
        {
            throw new InvalidOperationException();
        }

        if (!decimal.TryParse(raw.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            issues.Add(new CsvReadIssue
            {
                RowNumber = rowNumber,
                Field = fieldName,
                Message = $"Invalid numeric value '{raw}'.",
                Severity = "Error"
            });
            throw new InvalidOperationException();
        }

        return value;
    }

    private static bool TryGetString(
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, int> headerIndex,
        string fieldName,
        int rowNumber,
        ICollection<CsvReadIssue> issues,
        out string value)
    {
        if (!headerIndex.TryGetValue(fieldName, out var index))
        {
            issues.Add(new CsvReadIssue
            {
                RowNumber = rowNumber,
                Field = fieldName,
                Message = $"Missing column '{fieldName}'.",
                Severity = "Error"
            });
            value = string.Empty;
            return false;
        }

        value = fields[index];
        return true;
    }

    private static string[] SplitCsvLine(string line) =>
        line.Split(',', StringSplitOptions.TrimEntries);
}
