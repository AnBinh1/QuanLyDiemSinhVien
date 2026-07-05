using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Models;

namespace QuanLyDiemSinhVien.Repository
{
	public class DataContext : IdentityDbContext<IdentityUser>
	{
		public DataContext(DbContextOptions<DataContext> options) : base(options)
		{

		}
		public DbSet<CourseModel> Courses { get; set; }
		public DbSet<FacultyModel> Faculties { get; set; }
		public DbSet<MajorModel> Majors { get; set; }
		public DbSet<ClassModel> Classes { get; set; }
		public DbSet<StudentModel> Students { get; set; }
		public DbSet<LecturerModel> Lecturers { get; set; }
		public DbSet<CourseSectionModel> CourseSections { get; set; }

		public DbSet<EnrollmentModel> Enrollments { get; set; }

		public DbSet<AttendanceSessionModel> AttendanceSessions { get; set; }

		public DbSet<AttendanceModel> Attendances { get; set; }

		public DbSet<StudentGradeModel> Grades { get; set; }
	}
}
