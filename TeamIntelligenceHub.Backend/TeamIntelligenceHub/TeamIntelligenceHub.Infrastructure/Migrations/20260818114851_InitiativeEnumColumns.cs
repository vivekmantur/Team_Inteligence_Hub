using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamIntelligenceHub.Infrastructure.Migrations
{
    /// <summary>
    /// Converts Priority, Status and Visibility from free-text columns to enums stored
    /// as their names, and makes all three NOT NULL.
    /// </summary>
    /// <remarks>
    /// The columns keep their nvarchar(50) type, so this is a data migration first and a
    /// schema migration second. Existing rows hold display strings ("On Track") and nulls,
    /// neither of which parses as an enum, so they are normalised before the columns are
    /// tightened. Rolling back restores the display strings.
    /// </remarks>
    public partial class InitiativeEnumColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Normalise first — the ALTERs below fail on nulls, and any surviving
            // display string would throw on read once the property is an enum.
            migrationBuilder.Sql(@"
                UPDATE [Initiatives]
                SET [Priority] = CASE
                        WHEN [Priority] IS NULL OR LTRIM(RTRIM([Priority])) = '' THEN 'Medium'
                        ELSE REPLACE([Priority], ' ', '')
                    END;");

            migrationBuilder.Sql(@"
                UPDATE [Initiatives]
                SET [Status] = CASE
                        WHEN [Status] IS NULL OR LTRIM(RTRIM([Status])) = '' THEN 'Planning'
                        ELSE REPLACE([Status], ' ', '')
                    END;");

            migrationBuilder.Sql(@"
                UPDATE [Initiatives]
                SET [Visibility] = CASE
                        WHEN [Visibility] IS NULL OR LTRIM(RTRIM([Visibility])) = ''
                            THEN 'InitiativeMembers'
                        ELSE REPLACE([Visibility], ' ', '')
                    END;");

            // Anything still outside the enum would break every read of the table, so
            // park it on a safe value rather than leaving a row that cannot be loaded.
            migrationBuilder.Sql(@"
                UPDATE [Initiatives] SET [Priority] = 'Medium'
                WHERE [Priority] NOT IN ('High', 'Medium', 'Low');");

            migrationBuilder.Sql(@"
                UPDATE [Initiatives] SET [Status] = 'Planning'
                WHERE [Status] NOT IN
                    ('Draft', 'Planning', 'OnTrack', 'AtRisk', 'OnHold', 'Completed', 'Cancelled');");

            migrationBuilder.Sql(@"
                UPDATE [Initiatives] SET [Visibility] = 'InitiativeMembers'
                WHERE [Visibility] NOT IN
                    ('InitiativeMembers', 'Leadership', 'Organization', 'Private');");

            migrationBuilder.AlterColumn<string>(
                name: "Visibility",
                table: "Initiatives",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Initiatives",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Priority",
                table: "Initiatives",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Visibility",
                table: "Initiatives",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Initiatives",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Priority",
                table: "Initiatives",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            // Put the display strings back so the data reads the way it did before.
            migrationBuilder.Sql(@"
                UPDATE [Initiatives] SET [Status] = 'On Track' WHERE [Status] = 'OnTrack';
                UPDATE [Initiatives] SET [Status] = 'At Risk'  WHERE [Status] = 'AtRisk';
                UPDATE [Initiatives] SET [Status] = 'On Hold'  WHERE [Status] = 'OnHold';
                UPDATE [Initiatives] SET [Visibility] = 'Initiative Members'
                    WHERE [Visibility] = 'InitiativeMembers';");
        }
    }
}
