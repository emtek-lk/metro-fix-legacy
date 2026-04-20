using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase13RequestLifecycleConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceRequestLifecycleTransitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ToStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRequestLifecycleTransitions", x => x.Id);
                    table.UniqueConstraint("AK_ServiceRequestLifecycleTransitions_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ServiceRequestLifecycleTransitions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_ServiceRequestLifecycleTransitions_TenantId_From_To",
                table: "ServiceRequestLifecycleTransitions",
                columns: new[] { "TenantId", "FromStatus", "ToStatus" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceRequestLifecycleTransitions");
        }
    }
}
