using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamIntelligenceHub.Infrastructure.Migrations
{
    /// <summary>
    /// Splits Status into LifecycleStage/Health/Status, replaces Visibility with Segment
    /// (an unrelated concept — not a rename), adds ImpactedRoles and ChangeImpact, and
    /// tightens BusinessArea/InitiativeType from free text to closed enums.
    /// </summary>
    /// <remarks>
    /// The scaffolded migration got two things wrong that would have broken every
    /// existing row on the next read: it treated Visibility→Segment as a rename (they are
    /// different concepts — a Visibility value is not a valid Segment), and it narrowed
    /// BusinessArea/InitiativeType without converting the old free-text values into the
    /// new enum names. This version fixes both, following the same
    /// backfill-before-narrow pattern as 20260818114851_InitiativeEnumColumns.cs.
    ///
    /// Backfilling Visibility's original per-row values into anything is not attempted on
    /// the way down — Segment only has one member (Enterprise) today, so there is nothing
    /// meaningful to preserve going forward either. Down() restores a valid Visibility
    /// column but not each row's original choice.
    /// </remarks>
    public partial class InitiativeAudienceAndLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Health/LifecycleStage first, with a safe blanket default — refined below
            // from each row's *current* Status value before Status itself is remapped.
            migrationBuilder.AddColumn<string>(
                name: "Health",
                table: "Initiatives",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "OnTrack");

            migrationBuilder.AddColumn<string>(
                name: "LifecycleStage",
                table: "Initiatives",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Assess");

            // Derived from the old 7-value Status while it is still readable — AtRisk
            // becomes both a Health flag and an Activate-stage Initiative; every other
            // status maps onto a plausible stage without inventing history that was
            // never captured.
            migrationBuilder.Sql(@"
                UPDATE [Initiatives]
                SET [Health] = CASE WHEN [Status] = 'AtRisk' THEN 'AtRisk' ELSE 'OnTrack' END,
                    [LifecycleStage] = CASE [Status]
                        WHEN 'Draft' THEN 'Assess'
                        WHEN 'Planning' THEN 'Plan'
                        WHEN 'OnTrack' THEN 'Activate'
                        WHEN 'AtRisk' THEN 'Activate'
                        WHEN 'OnHold' THEN 'Activate'
                        WHEN 'Completed' THEN 'Sustain'
                        WHEN 'Cancelled' THEN 'Assess'
                        ELSE 'Assess'
                    END;");

            // Now collapse Status onto the new 4-value set. Draft/Planning/OnTrack/AtRisk
            // all become Active — the distinction those four used to carry now lives in
            // LifecycleStage/Health, which were just populated above.
            migrationBuilder.Sql(@"
                UPDATE [Initiatives]
                SET [Status] = CASE
                        WHEN [Status] IN ('Draft', 'Planning', 'OnTrack', 'AtRisk') THEN 'Active'
                        WHEN [Status] IN ('OnHold', 'Cancelled', 'Completed') THEN [Status]
                        ELSE 'Active'
                    END;");

            // No historical signal for this one — it did not exist before.
            migrationBuilder.AddColumn<string>(
                name: "ChangeImpact",
                table: "Initiatives",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Medium");

            // Visibility and Segment are unrelated concepts — drop and add, not rename.
            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Initiatives");

            migrationBuilder.AddColumn<string>(
                name: "Segment",
                table: "Initiatives",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Enterprise");

            // Best-effort mapping from the old suggestion-chip strings (the only values
            // that could ever have been stored — the old UI had no free-text entry) onto
            // the new closed set. Widened columns are narrowed only after this runs.
            migrationBuilder.Sql(@"
                UPDATE [Initiatives]
                SET [BusinessArea] = CASE [BusinessArea]
                        WHEN 'Adoption' THEN 'ChangeManagementAndAdoption'
                        WHEN 'Change Management' THEN 'ChangeManagementAndAdoption'
                        WHEN 'Storytelling' THEN 'StorytellingAndEvidence'
                        WHEN 'Insights' THEN 'InsightsAndMeasurement'
                        ELSE 'Other'
                    END;");

            migrationBuilder.AlterColumn<string>(
                name: "BusinessArea",
                table: "Initiatives",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.Sql(@"
                UPDATE [Initiatives]
                SET [InitiativeType] = CASE [InitiativeType]
                        WHEN 'Enablement' THEN 'ContentDevelopment'
                        WHEN 'Communications' THEN 'CommunityOrEngagementMotion'
                        WHEN 'Analytics' THEN 'ReportingOrAnalytics'
                        WHEN 'Customer Zero' THEN 'Campaign'
                        WHEN 'Operational Improvement' THEN 'OperationalImprovement'
                        ELSE 'Motion'
                    END;");

            migrationBuilder.AlterColumn<string>(
                name: "InitiativeType",
                table: "Initiatives",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            // One JSON column, not a child table — see ContributionConfiguration's
            // Types/Tags/ReuseTargets for the same pattern. "[]" is a valid empty list.
            migrationBuilder.AddColumn<string>(
                name: "ImpactedRoles",
                table: "Initiatives",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImpactedRoles",
                table: "Initiatives");

            migrationBuilder.AlterColumn<string>(
                name: "InitiativeType",
                table: "Initiatives",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "BusinessArea",
                table: "Initiatives",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            // BusinessArea/InitiativeType keep the new enum-style values (e.g.
            // "ChangeManagementAndAdoption") even after the column widens back — mapping
            // them back to the old suggestion-chip strings is not attempted, the same way
            // 20260818114851_InitiativeEnumColumns.cs did not restore pre-enum free text.

            migrationBuilder.DropColumn(
                name: "Segment",
                table: "Initiatives");

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                table: "Initiatives",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "InitiativeMembers");

            migrationBuilder.DropColumn(
                name: "ChangeImpact",
                table: "Initiatives");

            // Status keeps the collapsed 4-value set on the way down too — the original
            // Draft/Planning/OnTrack/AtRisk distinction was moved into LifecycleStage/
            // Health, which are dropped here, so there is nothing left to restore it from.
            migrationBuilder.DropColumn(
                name: "LifecycleStage",
                table: "Initiatives");

            migrationBuilder.DropColumn(
                name: "Health",
                table: "Initiatives");
        }
    }
}
