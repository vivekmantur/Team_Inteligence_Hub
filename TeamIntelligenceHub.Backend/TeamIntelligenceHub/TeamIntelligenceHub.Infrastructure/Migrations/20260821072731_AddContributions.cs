using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamIntelligenceHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContributions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contributions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InitiativeId = table.Column<int>(type: "int", nullable: false),
                    SubmittedByUserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    KeyTakeaway = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Types = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReuseTargets = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contributions_Initiatives_InitiativeId",
                        column: x => x.InitiativeId,
                        principalTable: "Initiatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contributions_Users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContributionAiPractices",
                columns: table => new
                {
                    ContributionId = table.Column<int>(type: "int", nullable: false),
                    Tool = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UseCase = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Prompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeSavedHoursPerWeek = table.Column<decimal>(type: "decimal(9,2)", nullable: true),
                    Recommendation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionAiPractices", x => x.ContributionId);
                    table.ForeignKey(
                        name: "FK_ContributionAiPractices_Contributions_ContributionId",
                        column: x => x.ContributionId,
                        principalTable: "Contributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContributionAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContributionId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BlobName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContributionAttachments_Contributions_ContributionId",
                        column: x => x.ContributionId,
                        principalTable: "Contributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContributionContributors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContributionId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ResponsibilityArea = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionContributors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContributionContributors_Contributions_ContributionId",
                        column: x => x.ContributionId,
                        principalTable: "Contributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContributionContributors_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContributionCustomerStories",
                columns: table => new
                {
                    ContributionId = table.Column<int>(type: "int", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Quote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BusinessValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionCustomerStories", x => x.ContributionId);
                    table.ForeignKey(
                        name: "FK_ContributionCustomerStories_Contributions_ContributionId",
                        column: x => x.ContributionId,
                        principalTable: "Contributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContributionLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContributionId = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContributionLinks_Contributions_ContributionId",
                        column: x => x.ContributionId,
                        principalTable: "Contributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContributionMetrics",
                columns: table => new
                {
                    ContributionId = table.Column<int>(type: "int", nullable: false),
                    MetricName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PreviousValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CurrentValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ReportingPeriod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionMetrics", x => x.ContributionId);
                    table.ForeignKey(
                        name: "FK_ContributionMetrics_Contributions_ContributionId",
                        column: x => x.ContributionId,
                        principalTable: "Contributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContributionRisks",
                columns: table => new
                {
                    ContributionId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BusinessImpact = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Mitigation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SupportNeeded = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OwnerUserId = table.Column<int>(type: "int", nullable: true),
                    TargetResolutionDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionRisks", x => x.ContributionId);
                    table.ForeignKey(
                        name: "FK_ContributionRisks_Contributions_ContributionId",
                        column: x => x.ContributionId,
                        principalTable: "Contributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContributionRisks_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContributionAttachments_ContributionId",
                table: "ContributionAttachments",
                column: "ContributionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContributionContributors_ContributionId_UserId",
                table: "ContributionContributors",
                columns: new[] { "ContributionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContributionContributors_UserId",
                table: "ContributionContributors",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContributionLinks_ContributionId",
                table: "ContributionLinks",
                column: "ContributionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContributionRisks_OwnerUserId",
                table: "ContributionRisks",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContributionRisks_Severity",
                table: "ContributionRisks",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_InitiativeId_CreatedAt",
                table: "Contributions",
                columns: new[] { "InitiativeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_SubmittedByUserId",
                table: "Contributions",
                column: "SubmittedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContributionAiPractices");

            migrationBuilder.DropTable(
                name: "ContributionAttachments");

            migrationBuilder.DropTable(
                name: "ContributionContributors");

            migrationBuilder.DropTable(
                name: "ContributionCustomerStories");

            migrationBuilder.DropTable(
                name: "ContributionLinks");

            migrationBuilder.DropTable(
                name: "ContributionMetrics");

            migrationBuilder.DropTable(
                name: "ContributionRisks");

            migrationBuilder.DropTable(
                name: "Contributions");
        }
    }
}
