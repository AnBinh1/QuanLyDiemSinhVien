using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDiemSinhVien.Models
{
	public class CourseModel
	{
		[Key]
		public int CourseId { get; set; }

		[Required(ErrorMessage ="yêu cầu nhập mã học phần")]
		[StringLength(20)]
		public string CourseCode { get; set; } = "";

		[Required(ErrorMessage = "yêu cầu nhập tên học phần")]
		[StringLength(200)]
		public string CourseName { get; set; } = "";

		[Required(ErrorMessage = "yêu cầu nhập số tín chỉ")]
		public int Credits { get; set; }

		[Required(ErrorMessage = "yêu cầu nhập mô tả học phần")]
		[StringLength(500)]
		public string Description { get; set; } = "";

		[Required(ErrorMessage = "Yêu cầu chọn loại học phần")]
		[StringLength(100)]
		public string CourseType { get; set; } = "";
		//bắt buộc/ Tự chọn
		public int? PrerequisiteCourseId { get; set; } = null;
		// Học phần tiên quyết (0 = không có)

		// Quan hệ tự tham chiếu (tiên quyết)
		[ForeignKey("PrerequisiteCourseId")]
		public CourseModel? PrerequisiteCourse { get; set; }

		
	}
}
