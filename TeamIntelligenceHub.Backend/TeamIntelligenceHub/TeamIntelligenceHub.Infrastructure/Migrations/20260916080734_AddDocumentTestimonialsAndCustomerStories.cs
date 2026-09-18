using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamIntelligenceHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentTestimonialsAndCustomerStories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentTestimonialsAndCustomerStories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContributionAttachmentId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    BusinessValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SpeakerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SpeakerRole = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Audience = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Sentiment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTestimonialsAndCustomerStories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentTestimonialsAndCustomerStories_ContributionAttachments_ContributionAttachmentId",
                        column: x => x.ContributionAttachmentId,
                        principalTable: "ContributionAttachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTestimonialsAndCustomerStories_ContributionAttachmentId",
                table: "DocumentTestimonialsAndCustomerStories",
                column: "ContributionAttachmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentTestimonialsAndCustomerStories");
        }
    }
}
