using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDiemSinhVien.Models
{
	public class AttendanceModel
	{
		[Key]
		public int AttendanceId { get; set; }
		public int AttendanceSessionId { get; set; }
		[ForeignKey("AttendanceSessionId")]
		public AttendanceSessionModel? AttendanceSession { get; set; }
		public int StudentId { get; set; }
		[ForeignKey("StudentId")]
		public StudentModel? Student { get; set; }
		public int AttendanceStatus { get; set; }

		public string? Note { get; set; }

	}
}
