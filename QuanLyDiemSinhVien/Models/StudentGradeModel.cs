using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDiemSinhVien.Models
{
	public class StudentGradeModel
	{
		[Key]
		public int GradeId { get; set; }

		public int EnrollmentId { get; set; }
		[ForeignKey("EnrollmentId")]
		public EnrollmentModel? Enrollment { get; set; }

		// Điểm quá trình (0-10)
		[Range(0, 10, ErrorMessage = "Điểm quá trình phải từ 0 đến 10")]
		[Column(TypeName = "decimal(5,2)")]
		public decimal ContinuousAssessment { get; set; }

		// Điểm cuối kỳ (0-10)
		[Range(0, 10, ErrorMessage = "Điểm cuối kỳ phải từ 0 đến 10")]
		[Column(TypeName = "decimal(5,2)")]
		public decimal FinalExam { get; set; }
		// Điểm tổng kết = (Quá trình + Cuối kỳ) / 2
		[NotMapped]
		public decimal TotalScore => Math.Round((ContinuousAssessment + FinalExam) / 2, 2);

		// Điểm chữ tự động
		[NotMapped]
		public string LetterGrade
		{
			get
			{
				if (TotalScore >= 8.5m) return "A";
				if (TotalScore >= 7.0m) return "B";
				if (TotalScore >= 5.5m) return "C";
				if (TotalScore >= 4.0m) return "D";
				return "F";
			}
		}

		// Tham gia xét tốt nghiệp (true = tham gia, false = không tham gia)
		public bool IsGraduationEligible { get; set; } = true;

		// Ghi chú nếu cần
		[StringLength(500)]
		public string? Notes { get; set; }
		//ngày nhập điểm
		public DateTime UpdatedAt { get; set; } = DateTime.Now;

		[ForeignKey("User")]
		public string? UserId { get; set; }  // Id của IdentityUser
		public IdentityUser? User { get; set; }
	}
}
