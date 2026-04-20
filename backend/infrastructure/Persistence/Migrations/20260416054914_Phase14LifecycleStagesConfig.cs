using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase14LifecycleStagesConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceRequestLifecycleStages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatusCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRequestLifecycleStages", x => x.Id);
                    table.UniqueConstraint("AK_ServiceRequestLifecycleStages_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ServiceRequestLifecycleStages_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRequestLifecycleStageTransitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRequestLifecycleStageTransitions", x => x.Id);
                    table.UniqueConstraint("AK_ServiceRequestLifecycleStageTransitions_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ServiceRequestLifecycleStageTransitions_Stages_From",
                        columns: x => new { x.TenantId, x.FromStageId },
                        principalTable: "ServiceRequestLifecycleStages",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceRequestLifecycleStageTransitions_Stages_To",
                        columns: x => new { x.TenantId, x.ToStageId },
                        principalTable: "ServiceRequestLifecycleStages",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceRequestLifecycleStageTransitions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequestLifecycleStages_TenantId_DisplayOrder",
                table: "ServiceRequestLifecycleStages",
                columns: new[] { "TenantId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "UQ_ServiceRequestLifecycleStages_TenantId_DisplayName",
                table: "ServiceRequestLifecycleStages",
                columns: new[] { "TenantId", "DisplayName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequestLifecycleStageTransitions_TenantId_ToStageId",
                table: "ServiceRequestLifecycleStageTransitions",
                columns: new[] { "TenantId", "ToStageId" });

            migrationBuilder.CreateIndex(
                name: "UQ_ServiceRequestLifecycleStageTransitions_TenantId_From_To",
                table: "ServiceRequestLifecycleStageTransitions",
                columns: new[] { "TenantId", "FromStageId", "ToStageId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceRequestLifecycleStageTransitions");

            migrationBuilder.DropTable(
                name: "ServiceRequestLifecycleStages");
        }
    }
}
