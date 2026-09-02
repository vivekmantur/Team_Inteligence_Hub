using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamIntelligenceHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInitiativeMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InitiativeMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InitiativeId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ResponsibilityArea = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Allocation = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InitiativeMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InitiativeMembers_Initiatives_InitiativeId",
                        column: x => x.InitiativeId,
                        principalTable: "Initiatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InitiativeMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeMembers_InitiativeId_UserId",
                table: "InitiativeMembers",
                columns: new[] { "InitiativeId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeMembers_UserId",
                table: "InitiativeMembers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InitiativeMembers");
        }
    }
}
