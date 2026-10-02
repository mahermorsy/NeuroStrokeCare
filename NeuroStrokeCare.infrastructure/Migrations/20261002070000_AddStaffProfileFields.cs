using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroStrokeCare.infrastructure.Migrations
{
    /// <inheritdoc />
    // ADDITIONAL CLINICAL UX & STAFF PROFILE TASKS (area 2): four new optional staff-profile
    // columns on AspNetUsers, written by hand following the exact AddColumn<string> pattern
    // used by the "FixDecimalPrecision" migration for EmployeeId/ProfilePhotoUrl (same table,
    // same nvarchar(max)/nullable shape). No "dotnet ef migrations add" tooling was reachable
    // in this environment (see PHASE9_REPORT.md) - this migration was written manually and
    // its corresponding Designer.cs/ModelSnapshot.cs updated to match. Run
    // "dotnet ef migrations has-pending-model-changes" (or just "dotnet build" then
    // "dotnet ef database update") locally before trusting this against a real database.
    public partial class AddStaffProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Profession",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcademicDegree",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Profession",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AcademicDegree",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "AspNetUsers");
        }
    }
}
