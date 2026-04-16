using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase14RequestCurrentStageRuntime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentStageId",
                table: "ServiceRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_TenantId_CurrentStageId",
                table: "ServiceRequests",
                columns: new[] { "TenantId", "CurrentStageId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceRequests_ServiceRequestLifecycleStages_TenantId_CurrentStageId",
                table: "ServiceRequests",
                columns: new[] { "TenantId", "CurrentStageId" },
                principalTable: "ServiceRequestLifecycleStages",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceRequests_ServiceRequestLifecycleStages_TenantId_CurrentStageId",
                table: "ServiceRequests");

            migrationBuilder.DropIndex(
                name: "IX_ServiceRequests_TenantId_CurrentStageId",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "CurrentStageId",
                table: "ServiceRequests");
        }
    }
}
