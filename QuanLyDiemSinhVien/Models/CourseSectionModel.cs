
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDiemSinhVien.Models
{
	public class CourseSectionModel
	{
		[Key]
		public int SectionId { get; set; }

		[Required]
		public int CourseId { get; set; }

		[ForeignKey("CourseId")]
		public CourseModel? Course { get; set; }

		// Mã lớp học phần, ví dụ: CNTT101-01
		[Required]
		[StringLength(50)]
		public string SectionCode { get; set; } = "";

		// Học kỳ, ví dụ: HK1, HK2
		[Required]
		[StringLength(20)]
		public string Semester { get; set; } = "";

		// Năm học, ví dụ: 2025-2026
		[Required]
		[StringLength(20)]
		public string AcademicYear { get; set; } = "";

		// Số lượng sinh viên tối đa
		[Required]
		public int MaxStudents { get; set; }

		// Số lượng sinh viên đã đăng ký
		public int RegisteredStudents { get; set; } = 0;

		// Giảng viên phụ trách
		public int? LecturerId { get; set; }
		[ForeignKey("LecturerId")]
		public LecturerModel? Lecturer { get; set; }

		public int? MajorId { get; set; }

		[ForeignKey("MajorId")]
		public MajorModel? Major { get; set; }


		// Số giờ thực tế
		public int LectureHours { get; set; } = 0;//lý thuyết
		public int PracticeHours { get; set; } = 0;//thực hành
		public int SelfStudyHours { get; set; } = 0;//tự học

		// Ngày bắt đầu và kết thúc lớp đăng ký
		public DateTime? StartDate { get; set; }
		public DateTime? EndDate { get; set; }

		// Tuần bắt đầu – kết thúc (ví dụ: 1–15)
		public int? StartWeek { get; set; }

		public int? EndWeek { get; set; }
		// Thứ trong tuần: 2–7 (2=Thứ Hai, 7=Chủ Nhật)
		public int DayOfWeek { get; set; }

		// Tiết học, ví dụ: từ tiết 3 đến tiết 5
		public int? EndPeriod { get; set; }

		public int? StartPeriod { get; set; }
		// Phòng học
		[StringLength(100)]
		public string? Room { get; set; }

		// Trạng thái lớp học (ví dụ: Mở / Đóng / Hoãn)
		[StringLength(50)]
		public string Status { get; set; } = "Closed";

		// Ghi chú thêm
		[StringLength(500)]
		public string? Notes { get; set; }
	}
}
