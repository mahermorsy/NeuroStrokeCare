using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroStrokeCare.infrastructure.Migrations
{
    /// <inheritdoc />
    // PHASE 11 (area 1): adds the new HospitalNumber identifier to Patients - a nullable,
    // explicitly length-bounded (nvarchar(30), matching [StringLength(30)] on the DTOs)
    // column plus a filtered unique index (NULL allowed multiple times - only a real,
    // assigned value must be unique). Written by hand, same reasoning as
    // 20261002070000_AddStaffProfileFields: no "dotnet ef migrations add" tooling was
    // reachable in this environment. Unlike that migration, this one also adds an index, so
    // double-check "dotnet ef migrations has-pending-model-changes" particularly carefully
    // before trusting it - an index definition is easier to get subtly wrong by hand than a
    // plain AddColumn.
    public partial class AddPatientHospitalNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HospitalNumber",
                table: "Patients",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_HospitalNumber",
                table: "Patients",
                column: "HospitalNumber",
                unique: true,
                filter: "[HospitalNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_HospitalNumber",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "HospitalNumber",
                table: "Patients");
        }
    }
}
