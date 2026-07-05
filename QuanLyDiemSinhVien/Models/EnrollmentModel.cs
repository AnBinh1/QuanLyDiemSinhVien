using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDiemSinhVien.Models
{
	public class EnrollmentModel
	{
		[Key]
		public int EnrollmentId { get; set; }

		public int StudentId { get; set; }
		[ForeignKey("StudentId")]
		public StudentModel? Student { get; set; }

		public int SectionId { get; set; }
		[ForeignKey("SectionId")]
		public CourseSectionModel? CourseSection { get; set; }
	}
}
