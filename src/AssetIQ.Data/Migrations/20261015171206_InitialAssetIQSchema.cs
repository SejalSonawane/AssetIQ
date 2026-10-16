using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetIQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialAssetIQSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "equipment_types",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "measurement_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PhysicalDimension = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CanonicalUnit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_measurement_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "plants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "equipment_type_measurements",
                columns: table => new
                {
                    EquipmentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeasurementDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_type_measurements", x => new { x.EquipmentTypeId, x.MeasurementDefinitionId });
                    table.ForeignKey(
                        name: "FK_equipment_type_measurements_equipment_types_EquipmentTypeId",
                        column: x => x.EquipmentTypeId,
                        principalTable: "equipment_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_equipment_type_measurements_measurement_definitions_Measure~",
                        column: x => x.MeasurementDefinitionId,
                        principalTable: "measurement_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "csv_import_batches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    SourceFileSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MappingProfileKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    MappingProfileVersion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    TotalRowCount = table.Column<int>(type: "integer", nullable: false),
                    AcceptedRowCount = table.Column<int>(type: "integer", nullable: false),
                    RejectedRowCount = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_csv_import_batches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_csv_import_batches_plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "csv_import_batch_rows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceRowNumber = table.Column<int>(type: "integer", nullable: false),
                    Classification = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ValidationMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_csv_import_batch_rows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_csv_import_batch_rows_csv_import_batches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "csv_import_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EquipmentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Section = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Criticality = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    OperatingStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    HealthScore = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: true),
                    RiskLevel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    SourceRowId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment", x => x.Id);
                    table.UniqueConstraint("AK_equipment_Id_EquipmentTypeId", x => new { x.Id, x.EquipmentTypeId });
                    table.ForeignKey(
                        name: "FK_equipment_csv_import_batch_rows_SourceRowId",
                        column: x => x.SourceRowId,
                        principalTable: "csv_import_batch_rows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_equipment_types_EquipmentTypeId",
                        column: x => x.EquipmentTypeId,
                        principalTable: "equipment_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Severity = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RecommendedNextStep = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RaisedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceRowId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_alerts_csv_import_batch_rows_SourceRowId",
                        column: x => x.SourceRowId,
                        principalTable: "csv_import_batch_rows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_alerts_equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "energy_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PowerKw = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    EnergyKwh = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProductionOutput = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    ProductionUnit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    SourceRowId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_energy_records", x => x.Id);
                    table.CheckConstraint("ck_energy_records_period_order", "\"PeriodEndUtc\" > \"PeriodStartUtc\"");
                    table.ForeignKey(
                        name: "FK_energy_records_csv_import_batch_rows_SourceRowId",
                        column: x => x.SourceRowId,
                        principalTable: "csv_import_batch_rows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_energy_records_equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "failure_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FailureMode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DowntimeHours = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    EstimatedCostImpact = table.Column<decimal>(type: "numeric(20,2)", precision: 20, scale: 2, nullable: true),
                    SourceRowId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_failure_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_failure_records_csv_import_batch_rows_SourceRowId",
                        column: x => x.SourceRowId,
                        principalTable: "csv_import_batch_rows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_failure_records_equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Priority = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AssignedTo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ScheduledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SourceRowId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_maintenance_records_csv_import_batch_rows_SourceRowId",
                        column: x => x.SourceRowId,
                        principalTable: "csv_import_batch_rows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maintenance_records_equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prediction_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelIdentifier = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ModelVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TargetKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PredictedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PredictionHorizonSeconds = table.Column<int>(type: "integer", nullable: true),
                    PredictedClass = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    PredictedValue = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    Score = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: true),
                    Probability = table.Column<decimal>(type: "numeric(9,8)", precision: 9, scale: 8, nullable: true),
                    InputDataMetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    ExplanationMetadataJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prediction_results", x => x.Id);
                    table.CheckConstraint("ck_prediction_results_has_output", "\"PredictedClass\" IS NOT NULL OR \"PredictedValue\" IS NOT NULL");
                    table.CheckConstraint("ck_prediction_results_nonnegative_horizon", "\"PredictionHorizonSeconds\" IS NULL OR \"PredictionHorizonSeconds\" >= 0");
                    table.CheckConstraint("ck_prediction_results_probability_range", "\"Probability\" IS NULL OR (\"Probability\" >= 0 AND \"Probability\" <= 1)");
                    table.ForeignKey(
                        name: "FK_prediction_results_equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recommended_actions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Priority = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceRowId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recommended_actions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recommended_actions_csv_import_batch_rows_SourceRowId",
                        column: x => x.SourceRowId,
                        principalTable: "csv_import_batch_rows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recommended_actions_equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sensor_readings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeasurementDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Unit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceRowId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sensor_readings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sensor_readings_csv_import_batch_rows_SourceRowId",
                        column: x => x.SourceRowId,
                        principalTable: "csv_import_batch_rows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sensor_readings_equipment_EquipmentId_EquipmentTypeId",
                        columns: x => new { x.EquipmentId, x.EquipmentTypeId },
                        principalTable: "equipment",
                        principalColumns: new[] { "Id", "EquipmentTypeId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sensor_readings_equipment_type_measurements_EquipmentTypeId~",
                        columns: x => new { x.EquipmentTypeId, x.MeasurementDefinitionId },
                        principalTable: "equipment_type_measurements",
                        principalColumns: new[] { "EquipmentTypeId", "MeasurementDefinitionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sensor_readings_measurement_definitions_MeasurementDefiniti~",
                        column: x => x.MeasurementDefinitionId,
                        principalTable: "measurement_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alerts_EquipmentId_RaisedAtUtc",
                table: "alerts",
                columns: new[] { "EquipmentId", "RaisedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_alerts_SourceRowId",
                table: "alerts",
                column: "SourceRowId");

            migrationBuilder.CreateIndex(
                name: "IX_csv_import_batch_rows_ImportBatchId_Classification",
                table: "csv_import_batch_rows",
                columns: new[] { "ImportBatchId", "Classification" });

            migrationBuilder.CreateIndex(
                name: "IX_csv_import_batch_rows_ImportBatchId_SourceRowNumber",
                table: "csv_import_batch_rows",
                columns: new[] { "ImportBatchId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_csv_import_batches_PlantId_SourceFileSha256_MappingProfileK~",
                table: "csv_import_batches",
                columns: new[] { "PlantId", "SourceFileSha256", "MappingProfileKey", "MappingProfileVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_energy_records_EquipmentId_PeriodStartUtc_PeriodEndUtc",
                table: "energy_records",
                columns: new[] { "EquipmentId", "PeriodStartUtc", "PeriodEndUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_energy_records_SourceRowId",
                table: "energy_records",
                column: "SourceRowId",
                unique: true,
                filter: "\"SourceRowId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_EquipmentTypeId_PlantId",
                table: "equipment",
                columns: new[] { "EquipmentTypeId", "PlantId" });

            migrationBuilder.CreateIndex(
                name: "IX_equipment_PlantId_EquipmentCode",
                table: "equipment",
                columns: new[] { "PlantId", "EquipmentCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_equipment_SourceRowId",
                table: "equipment",
                column: "SourceRowId",
                unique: true,
                filter: "\"SourceRowId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_type_measurements_EquipmentTypeId_DisplayOrder",
                table: "equipment_type_measurements",
                columns: new[] { "EquipmentTypeId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_equipment_type_measurements_MeasurementDefinitionId",
                table: "equipment_type_measurements",
                column: "MeasurementDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_types_Key",
                table: "equipment_types",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_failure_records_EquipmentId_OccurredAtUtc",
                table: "failure_records",
                columns: new[] { "EquipmentId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_failure_records_SourceRowId",
                table: "failure_records",
                column: "SourceRowId");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_records_EquipmentId_Status",
                table: "maintenance_records",
                columns: new[] { "EquipmentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_records_SourceRowId",
                table: "maintenance_records",
                column: "SourceRowId");

            migrationBuilder.CreateIndex(
                name: "IX_measurement_definitions_Key",
                table: "measurement_definitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plants_PlantCode",
                table: "plants",
                column: "PlantCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prediction_results_EquipmentId_TargetKey_PredictedAtUtc",
                table: "prediction_results",
                columns: new[] { "EquipmentId", "TargetKey", "PredictedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_prediction_results_ModelIdentifier_ModelVersion_PredictedAt~",
                table: "prediction_results",
                columns: new[] { "ModelIdentifier", "ModelVersion", "PredictedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_recommended_actions_EquipmentId_CreatedAtUtc",
                table: "recommended_actions",
                columns: new[] { "EquipmentId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_recommended_actions_SourceRowId",
                table: "recommended_actions",
                column: "SourceRowId");

            migrationBuilder.CreateIndex(
                name: "IX_sensor_readings_EquipmentId_EquipmentTypeId",
                table: "sensor_readings",
                columns: new[] { "EquipmentId", "EquipmentTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_sensor_readings_EquipmentId_MeasurementDefinitionId_Recorde~",
                table: "sensor_readings",
                columns: new[] { "EquipmentId", "MeasurementDefinitionId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_sensor_readings_EquipmentTypeId_MeasurementDefinitionId",
                table: "sensor_readings",
                columns: new[] { "EquipmentTypeId", "MeasurementDefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_sensor_readings_MeasurementDefinitionId",
                table: "sensor_readings",
                column: "MeasurementDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_sensor_readings_SourceRowId_MeasurementDefinitionId",
                table: "sensor_readings",
                columns: new[] { "SourceRowId", "MeasurementDefinitionId" },
                unique: true,
                filter: "\"SourceRowId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerts");

            migrationBuilder.DropTable(
                name: "energy_records");

            migrationBuilder.DropTable(
                name: "failure_records");

            migrationBuilder.DropTable(
                name: "maintenance_records");

            migrationBuilder.DropTable(
                name: "prediction_results");

            migrationBuilder.DropTable(
                name: "recommended_actions");

            migrationBuilder.DropTable(
                name: "sensor_readings");

            migrationBuilder.DropTable(
                name: "equipment");

            migrationBuilder.DropTable(
                name: "equipment_type_measurements");

            migrationBuilder.DropTable(
                name: "csv_import_batch_rows");

            migrationBuilder.DropTable(
                name: "equipment_types");

            migrationBuilder.DropTable(
                name: "measurement_definitions");

            migrationBuilder.DropTable(
                name: "csv_import_batches");

            migrationBuilder.DropTable(
                name: "plants");
        }
    }
}
