using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroStrokeCare.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NationalId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRootSuperAdmin = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Wards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalBeds = table.Column<int>(type: "int", nullable: false),
                    CurrentState = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NationalId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChiefComplaint = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentState = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Patients_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Beds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WardId = table.Column<int>(type: "int", nullable: false),
                    BedNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WardId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentState = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Beds_Wards_WardId1",
                        column: x => x.WardId1,
                        principalTable: "Wards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Admissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StrokeType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StrokeTypeSetAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BedId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DischargeTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DischargeNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdmittedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StrokeTypeSetById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CTDone = table.Column<bool>(type: "bit", nullable: false),
                    MRIDone = table.Column<bool>(type: "bit", nullable: false),
                    CTADone = table.Column<bool>(type: "bit", nullable: false),
                    CTFindings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MRIFindings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CTAFindings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentState = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Admissions_AspNetUsers_AdmittedById",
                        column: x => x.AdmittedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Admissions_AspNetUsers_StrokeTypeSetById",
                        column: x => x.StrokeTypeSetById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Admissions_Beds_BedId",
                        column: x => x.BedId,
                        principalTable: "Beds",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Admissions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ASPECTSAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    C = table.Column<int>(type: "int", nullable: false),
                    P = table.Column<int>(type: "int", nullable: false),
                    IC = table.Column<int>(type: "int", nullable: false),
                    I = table.Column<int>(type: "int", nullable: false),
                    M1 = table.Column<int>(type: "int", nullable: false),
                    M2 = table.Column<int>(type: "int", nullable: false),
                    M3 = table.Column<int>(type: "int", nullable: false),
                    M4 = table.Column<int>(type: "int", nullable: false),
                    M5 = table.Column<int>(type: "int", nullable: false),
                    M6 = table.Column<int>(type: "int", nullable: false),
                    CurrentState = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASPECTSAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ASPECTSAssessments_Admissions_AdmissionId",
                        column: x => x.AdmissionId,
                        principalTable: "Admissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ASPECTSAssessments_AspNetUsers_AssessedById",
                        column: x => x.AssessedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CanadianTIAAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FirstTIAInLifetime = table.Column<bool>(type: "bit", nullable: false),
                    SymptomsOver10Min = table.Column<bool>(type: "bit", nullable: false),
                    HistoryCarotidStenosis = table.Column<bool>(type: "bit", nullable: false),
                    OnAntiplateletTherapy = table.Column<bool>(type: "bit", nullable: false),
                    GaitDisturbance = table.Column<bool>(type: "bit", nullable: false),
                    UnilateralWeakness = table.Column<bool>(type: "bit", nullable: false),
                    HistoryOfVertigo = table.Column<bool>(type: "bit", nullable: false),
                    DiastolicBPOver110 = table.Column<bool>(type: "bit", nullable: false),
                    DysarthriaOrAphasia = table.Column<bool>(type: "bit", nullable: false),
                    AtrialFibrillationOnECG = table.Column<bool>(type: "bit", nullable: false),
                    InfarctionOnCT = table.Column<bool>(type: "bit", nullable: false),
                    PlateletOver400 = table.Column<bool>(type: "bit", nullable: false),
                    GlucoseOver15 = table.Column<bool>(type: "bit", nullable: false),
                    CurrentState = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanadianTIAAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CanadianTIAAssessments_Admissions_AdmissionId",
                        column: x => x.AdmissionId,
                        principalTable: "Admissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CanadianTIAAssessments_AspNetUsers_AssessedById",
                        column: x => x.AssessedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DoorTimings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Er_StrokeArrival = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DoorToCT = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DoorToNeedle = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DoorToGroin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CTRecordedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NeedleRecordedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GroinRecordedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentState = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoorTimings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoorTimings_Admissions_AdmissionId",
                        column: x => x.AdmissionId,
                        principalTable: "Admissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ICHAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GCSScore = table.Column<int>(type: "int", nullable: false),
                    ICHVolumeMl = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InfratentorialOrigin = table.Column<bool>(type: "bit", nullable: false),
                    IVHPresent = table.Column<bool>(type: "bit", nullable: false),
                    AgeScore = table.Column<int>(type: "int", nullable: false),
                    CurrentState = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ICHAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ICHAssessments_Admissions_AdmissionId",
                        column: x => x.AdmissionId,
                        principalTable: "Admissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ICHAssessments_AspNetUsers_AssessedById",
                        column: x => x.AssessedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LabResultsSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GlucoseMmol = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    INR = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PT = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Platelets = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Sodium = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Potassium = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Creatinine = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Hemoglobin = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LDL = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    HbA1c = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    aPTT = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ALT = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AST = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ECGAtrialFibrillation = table.Column<bool>(type: "bit", nullable: true),
                    CurrentState = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabResultsSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabResultsSet_Admissions_AdmissionId",
                        column: x => x.AdmissionId,
                        principalTable: "Admissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabResultsSet_AspNetUsers_RecordedById",
                        column: x => x.RecordedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NIHSSAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LOC = table.Column<int>(type: "int", nullable: false),
                    LOCQuestions = table.Column<int>(type: "int", nullable: false),
                    LOCCommands = table.Column<int>(type: "int", nullable: false),
                    BestGaze = table.Column<int>(type: "int", nullable: false),
                    Visual = table.Column<int>(type: "int", nullable: false),
                    FacialPalsy = table.Column<int>(type: "int", nullable: false),
                    MotorArmLeft = table.Column<int>(type: "int", nullable: false),
                    MotorArmRight = table.Column<int>(type: "int", nullable: false),
                    MotorLegLeft = table.Column<int>(type: "int", nullable: false),
                    MotorLegRight = table.Column<int>(type: "int", nullable: false),
                    LimbAtaxia = table.Column<int>(type: "int", nullable: false),
                    Sensory = table.Column<int>(type: "int", nullable: false),
                    BestLanguage = table.Column<int>(type: "int", nullable: false),
                    Dysarthria = table.Column<int>(type: "int", nullable: false),
                    Extinction = table.Column<int>(type: "int", nullable: false),
                    CurrentState = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NIHSSAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NIHSSAssessments_Admissions_AdmissionId",
                        column: x => x.AdmissionId,
                        principalTable: "Admissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NIHSSAssessments_AspNetUsers_AssessedById",
                        column: x => x.AssessedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_AdmittedById",
                table: "Admissions",
                column: "AdmittedById");

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_BedId",
                table: "Admissions",
                column: "BedId",
                unique: true,
                filter: "[BedId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_PatientId",
                table: "Admissions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_StrokeTypeSetById",
                table: "Admissions",
                column: "StrokeTypeSetById");

            migrationBuilder.CreateIndex(
                name: "IX_ASPECTSAssessments_AdmissionId",
                table: "ASPECTSAssessments",
                column: "AdmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ASPECTSAssessments_AssessedById",
                table: "ASPECTSAssessments",
                column: "AssessedById");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Beds_BedNumber",
                table: "Beds",
                column: "BedNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Beds_WardId1",
                table: "Beds",
                column: "WardId1");

            migrationBuilder.CreateIndex(
                name: "IX_CanadianTIAAssessments_AdmissionId",
                table: "CanadianTIAAssessments",
                column: "AdmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CanadianTIAAssessments_AssessedById",
                table: "CanadianTIAAssessments",
                column: "AssessedById");

            migrationBuilder.CreateIndex(
                name: "IX_DoorTimings_AdmissionId",
                table: "DoorTimings",
                column: "AdmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ICHAssessments_AdmissionId",
                table: "ICHAssessments",
                column: "AdmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ICHAssessments_AssessedById",
                table: "ICHAssessments",
                column: "AssessedById");

            migrationBuilder.CreateIndex(
                name: "IX_LabResultsSet_AdmissionId",
                table: "LabResultsSet",
                column: "AdmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabResultsSet_RecordedById",
                table: "LabResultsSet",
                column: "RecordedById");

            migrationBuilder.CreateIndex(
                name: "IX_NIHSSAssessments_AdmissionId",
                table: "NIHSSAssessments",
                column: "AdmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NIHSSAssessments_AssessedById",
                table: "NIHSSAssessments",
                column: "AssessedById");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_CreatedBy",
                table: "Patients",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_NationalId",
                table: "Patients",
                column: "NationalId");

            migrationBuilder.CreateIndex(
                name: "IX_Wards_Code",
                table: "Wards",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ASPECTSAssessments");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CanadianTIAAssessments");

            migrationBuilder.DropTable(
                name: "DoorTimings");

            migrationBuilder.DropTable(
                name: "ICHAssessments");

            migrationBuilder.DropTable(
                name: "LabResultsSet");

            migrationBuilder.DropTable(
                name: "NIHSSAssessments");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Admissions");

            migrationBuilder.DropTable(
                name: "Beds");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "Wards");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
