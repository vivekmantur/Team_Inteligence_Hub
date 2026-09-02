using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamIntelligenceHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceActivityId",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Activity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InitiativeId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ActivityMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AutoCreateTaskEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activity_Initiatives_InitiativeId",
                        column: x => x.InitiativeId,
                        principalTable: "Initiatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Activity_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityMentions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    MentionedUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityMentions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityMentions_Activity_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityMentions_Users_MentionedUserId",
                        column: x => x.MentionedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_SourceActivityId",
                table: "Tasks",
                column: "SourceActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_Activity_InitiativeId_CreatedAt",
                table: "Activity",
                columns: new[] { "InitiativeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Activity_UserId",
                table: "Activity",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityMentions_ActivityId_MentionedUserId",
                table: "ActivityMentions",
                columns: new[] { "ActivityId", "MentionedUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityMentions_MentionedUserId",
                table: "ActivityMentions",
                column: "MentionedUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Activity_SourceActivityId",
                table: "Tasks",
                column: "SourceActivityId",
                principalTable: "Activity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Activity_SourceActivityId",
                table: "Tasks");

            migrationBuilder.DropTable(
                name: "ActivityMentions");

            migrationBuilder.DropTable(
                name: "Activity");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_SourceActivityId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "SourceActivityId",
                table: "Tasks");
        }
    }
}
