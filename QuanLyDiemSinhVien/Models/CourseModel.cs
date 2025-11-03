using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemSinhVien.Models
{
	public class CourseModel
	{
		[Key]
		public int Id { get; set; }
		[Required(ErrorMessage ="yêu cầu nhập mã học phần")]
		public string CourseCode { get; set; } = "";	
		[Required(ErrorMessage = "yêu cầu nhập tên học phần")]
		public string CourseName { get; set; } = "";
		[Required(ErrorMessage = "yêu cầu nhập số tín chỉ")]
		public int Credits { get; set; } 
		[Required(ErrorMessage = "yêu cầu nhập tên khoa")]
		public string Department { get; set; } = "";
	}
}
