using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyDiemSinhVien.Migrations
{
    /// <inheritdoc />
    public partial class AddMajor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MajorId",
                table: "CourseSections",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseSections_MajorId",
                table: "CourseSections",
                column: "MajorId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseSections_Majors_MajorId",
                table: "CourseSections",
                column: "MajorId",
                principalTable: "Majors",
                principalColumn: "MajorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseSections_Majors_MajorId",
                table: "CourseSections");

            migrationBuilder.DropIndex(
                name: "IX_CourseSections_MajorId",
                table: "CourseSections");

            migrationBuilder.DropColumn(
                name: "MajorId",
                table: "CourseSections");
        }
    }
}
