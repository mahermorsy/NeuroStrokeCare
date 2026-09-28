using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroStrokeCare.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Beds_Wards_WardId1",
                table: "Beds");

            migrationBuilder.DropIndex(
                name: "IX_Beds_WardId1",
                table: "Beds");

            migrationBuilder.DropColumn(
                name: "WardId1",
                table: "Beds");

            // مش عارفين نستخدم AlterColumn هنا لأن SQL Server مش بيعرف يحول int لـ uniqueidentifier
            // مباشرة (مفيش تحويل ضمني ولا CAST بينهم أصلًا) - فبنمسح العمود القديم (int) ونعمله
            // من جديد بالنوع الصح (uniqueidentifier). آمن هنا لأن مفيش بيانات في الجدول أصلًا.
            migrationBuilder.DropColumn(
                name: "WardId",
                table: "Beds");

            migrationBuilder.AddColumn<Guid>(
                name: "WardId",
                table: "Beds",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.CreateIndex(
                name: "IX_Beds_WardId",
                table: "Beds",
                column: "WardId");

            migrationBuilder.AddForeignKey(
                name: "FK_Beds_Wards_WardId",
                table: "Beds",
                column: "WardId",
                principalTable: "Wards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Beds_Wards_WardId",
                table: "Beds");

            migrationBuilder.DropIndex(
                name: "IX_Beds_WardId",
                table: "Beds");

            migrationBuilder.DropColumn(
                name: "WardId",
                table: "Beds");

            migrationBuilder.AddColumn<int>(
                name: "WardId",
                table: "Beds",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "WardId1",
                table: "Beds",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Beds_WardId1",
                table: "Beds",
                column: "WardId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Beds_Wards_WardId1",
                table: "Beds",
                column: "WardId1",
                principalTable: "Wards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
