using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDiemSinhVien.Models
{
	public class AttendanceSessionModel
	{
		[Key]
		public int AttendanceSessionId { get; set; }

		public int SectionId { get; set; }
		[ForeignKey("SectionId")]
		public CourseSectionModel? CourseSection { get; set; }

		public DateTime SessionDate { get; set; }


		public string Week { get; set; } // Tuần, ví dụ: Tuần 1, Tuần 2

		public string Topic { get; set; }

		public string Note { get; set; }
	}
}
