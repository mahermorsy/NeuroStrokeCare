using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroStrokeCare.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingAssessmentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BradenAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SensoryPerception = table.Column<int>(type: "int", nullable: false),
                    Moisture = table.Column<int>(type: "int", nullable: false),
                    Activity = table.Column<int>(type: "int", nullable: false),
                    Mobility = table.Column<int>(type: "int", nullable: false),
                    Nutrition = table.Column<int>(type: "int", nullable: false),
                    FrictionShear = table.Column<int>(type: "int", nullable: false),
                    CurrentState = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BradenAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BradenAssessments_Admissions_AdmissionId",
                        column: x => x.AdmissionId,
                        principalTable: "Admissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BradenAssessments_AspNetUsers_AssessedById",
                        column: x => x.AssessedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GCSAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EyeResponse = table.Column<int>(type: "int", nullable: false),
                    VerbalResponse = table.Column<int>(type: "int", nullable: false),
                    MotorResponse = table.Column<int>(type: "int", nullable: false),
                    CurrentState = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GCSAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GCSAssessments_Admissions_AdmissionId",
                        column: x => x.AdmissionId,
                        principalTable: "Admissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GCSAssessments_AspNetUsers_AssessedById",
                        column: x => x.AssessedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GUSSAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Vigilance = table.Column<bool>(type: "bit", nullable: false),
                    VoluntaryCough = table.Column<bool>(type: "bit", nullable: false),
                    SalivaSwallowSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    NoDrooling = table.Column<bool>(type: "bit", nullable: false),
                    NoVoiceChange = table.Column<bool>(type: "bit", nullable: false),
                    SemisolidScore = table.Column<int>(type: "int", nullable: true),
                    LiquidScore = table.Column<int>(type: "int", nullable: true),
                    SolidScore = table.Column<int>(type: "int", nullable: true),
                    CurrentState = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GUSSAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GUSSAssessments_Admissions_AdmissionId",
                        column: x => x.AdmissionId,
                        principalTable: "Admissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GUSSAssessments_AspNetUsers_AssessedById",
                        column: x => x.AssessedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MorseAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HistoryOfFalling = table.Column<bool>(type: "bit", nullable: false),
                    SecondaryDiagnosis = table.Column<bool>(type: "bit", nullable: false),
                    AmbulatoryAid = table.Column<int>(type: "int", nullable: false),
                    IVOrHeparinLock = table.Column<bool>(type: "bit", nullable: false),
                    Gait = table.Column<int>(type: "int", nullable: false),
                    MentalStatus = table.Column<bool>(type: "bit", nullable: false),
                    CurrentState = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MorseAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MorseAssessments_Admissions_AdmissionId",
                        column: x => x.AdmissionId,
                        principalTable: "Admissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MorseAssessments_AspNetUsers_AssessedById",
                        column: x => x.AssessedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BradenAssessments_AdmissionId",
                table: "BradenAssessments",
                column: "AdmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BradenAssessments_AssessedById",
                table: "BradenAssessments",
                column: "AssessedById");

            migrationBuilder.CreateIndex(
                name: "IX_GCSAssessments_AdmissionId",
                table: "GCSAssessments",
                column: "AdmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GCSAssessments_AssessedById",
                table: "GCSAssessments",
                column: "AssessedById");

            migrationBuilder.CreateIndex(
                name: "IX_GUSSAssessments_AdmissionId",
                table: "GUSSAssessments",
                column: "AdmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GUSSAssessments_AssessedById",
                table: "GUSSAssessments",
                column: "AssessedById");

            migrationBuilder.CreateIndex(
                name: "IX_MorseAssessments_AdmissionId",
                table: "MorseAssessments",
                column: "AdmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MorseAssessments_AssessedById",
                table: "MorseAssessments",
                column: "AssessedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BradenAssessments");

            migrationBuilder.DropTable(
                name: "GCSAssessments");

            migrationBuilder.DropTable(
                name: "GUSSAssessments");

            migrationBuilder.DropTable(
                name: "MorseAssessments");
        }
    }
}
