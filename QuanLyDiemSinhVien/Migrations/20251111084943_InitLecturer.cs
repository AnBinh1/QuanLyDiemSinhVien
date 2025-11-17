using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyDiemSinhVien.Migrations
{
    /// <inheritdoc />
    public partial class InitLecturer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            

            migrationBuilder.CreateTable(
                name: "Lecturers",
                columns: table => new
                {
                    LecturerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LecturerCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlaceOfBirth = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Ethnicity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Religion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Nationality = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IdentityNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IdentityDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdentityPlace = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FacultyId = table.Column<int>(type: "int", nullable: false),
                    MajorId = table.Column<int>(type: "int", nullable: true),
                    Position = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AcademicPosition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AcademicDegree = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    JoinDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartTeachingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContractType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ContractStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContractEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmploymentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lecturers", x => x.LecturerId);
                    table.ForeignKey(
                        name: "FK_Lecturers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Lecturers_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "Faculties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Lecturers_Majors_MajorId",
                        column: x => x.MajorId,
                        principalTable: "Majors",
                        principalColumn: "MajorId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lecturers_FacultyId",
                table: "Lecturers",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_Lecturers_MajorId",
                table: "Lecturers",
                column: "MajorId");

            migrationBuilder.CreateIndex(
                name: "IX_Lecturers_UserId",
                table: "Lecturers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lecturers");
        }
    }
}
