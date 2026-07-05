using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDiemSinhVien.Models
{
	public class LecturerModel
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int LecturerId { get; set; } // Khóa chính, tự tăng

		[Required(ErrorMessage = "Yêu cầu nhập mã giảng viên")]
		[StringLength(10, MinimumLength = 5, ErrorMessage = "Mã giảng viên phải từ 5 đến 10 ký tự")]
		[RegularExpression(@"^[0-9]+$", ErrorMessage = "Mã giảng viên chỉ được chứa chữ số")]
		public string LecturerCode { get; set; } // Mã giảng viên, duy nhất

		[Required(ErrorMessage = "Yêu cầu nhập họ và tên giảng viên")]
		[StringLength(100, MinimumLength = 2, ErrorMessage = "Họ và tên phải từ 2 đến 100 ký tự")]
		[RegularExpression(@"^(?!.*\s{2,})[A-Za-zÀ-ỹ\s]+$", ErrorMessage = "Họ và tên chỉ được chứa chữ cái, không có ký tự đặc biệt và không có quá 1 khoảng trắng liên tiếp")]
		public string FullName { get; set; } // Họ tên đầy đủ

		[Required(ErrorMessage = "Yêu cầu chọn giới tính")]
		[RegularExpression(@"^(Nam|Nữ)$", ErrorMessage = "Giới tính chỉ được chọn Nam hoặc Nữ")]
		public string Gender { get; set; } // Giới tính

		[Required(ErrorMessage = "Yêu cầu nhập ngày sinh")]
		[DataType(DataType.Date)]
		[CustomValidation(typeof(LecturerModel), nameof(ValidateAge))]
		public DateTime DateOfBirth { get; set; } // Ngày sinh
		public static ValidationResult? ValidateAge(DateTime dob, ValidationContext context)
		{
			if (dob > DateTime.Now.AddYears(-25)) // Giảng viên phải ít nhất 25 tuổi
				return new ValidationResult("Giảng viên phải ít nhất 25 tuổi");
			return ValidationResult.Success;
		}


		[StringLength(255)]
		public string? PlaceOfBirth { get; set; } // Nơi sinh

		[StringLength(50)]
		public string? Ethnicity { get; set; } // Dân tộc

		[StringLength(50)]
		public string? Religion { get; set; } // Tôn giáo

		[StringLength(50)]
		public string? Nationality { get; set; } // Quốc tịch

		[StringLength(20)]
		[RegularExpression(@"^\d{9,12}$", ErrorMessage = "Số CMND/CCCD phải từ 9 đến 12 chữ số")]
		public string? IdentityNumber { get; set; } // Số CMND/CCCD

		[DataType(DataType.Date)]
		public DateTime? IdentityDate { get; set; } // Ngày cấp CMND/CCCD

		[StringLength(255)]
		public string? IdentityPlace { get; set; } // Nơi cấp CMND/CCCD

		[Required(ErrorMessage = "Yêu cầu nhập địa chỉ")]
		[StringLength(255, MinimumLength = 5, ErrorMessage = "Địa chỉ phải từ 5 đến 255 ký tự")]
		public string Address { get; set; } // Địa chỉ hiện tại

		[Required(ErrorMessage = "Yêu cầu nhập số điện thoại")]
		[RegularExpression(@"^(0\d{9,10})$", ErrorMessage = "Số điện thoại không hợp lệ (phải bắt đầu bằng 0 và có 10–11 số)")]
		public string PhoneNumber { get; set; } // Số điện thoại

		[EmailAddress(ErrorMessage = "Email không hợp lệ")]
		[StringLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự")]
		public string? Email { get; set; } // Email

		[Required(ErrorMessage = "Yêu cầu chọn liên kết tài khoản")]
		[ForeignKey("User")]
		public string? UserId { get; set; }  // Id của IdentityUser
		public IdentityUser? User { get; set; }


		// Công tác
		[Required(ErrorMessage = "Yêu cầu chọn khoa/viện")]
		public int FacultyId { get; set; } // Mã khoa/viện
		[ForeignKey("FacultyId")]
		public FacultyModel? Faculty { get; set; } // Liên kết với bảng Khoa

		[Required(ErrorMessage = "Yêu cầu chọn ngành dạy")]
		public int? MajorId { get; set; } // Nếu giảng viên thuộc bộ môn cụ thể
		[ForeignKey("MajorId")]
		public MajorModel? Major { get; set; } // Liên kết với bảng Ngành/Bộ môn

		[StringLength(50)]
		public string? Position { get; set; } // Chức danh: Trưởng bộ môn, Giảng viên chính,...

		[StringLength(50)]
		public string? AcademicPosition { get; set; } // Học hàm: Giáo sư, Phó giáo sư, TS,...

		[StringLength(50)]
		public string? AcademicDegree { get; set; } // Học vị: Thạc sĩ, Tiến sĩ (tách rõ so với học hàm)

		[DataType(DataType.Date)]
		[CustomValidation(typeof(LecturerModel), nameof(ValidateJoinDate))]
		public DateTime? JoinDate { get; set; } // Ngày vào trường
		public static ValidationResult? ValidateJoinDate(DateTime? date, ValidationContext context)
		{
			if (date.HasValue && date.Value > DateTime.Now)
				return new ValidationResult("Ngày vào trường không được lớn hơn ngày hiện tại");
			return ValidationResult.Success;
		}

		[DataType(DataType.Date)]
		public DateTime? StartTeachingDate { get; set; } // Ngày bắt đầu giảng dạy


		[DataType(DataType.Date)]
		public DateTime? RetireDate { get; set; } // Ngày nghỉ hưu hoặc thôi việc

		[StringLength(50)]
		public string? ContractType { get; set; } // Loại hợp đồng: Dài hạn, Ngắn hạn, Thử việc,...

		[DataType(DataType.Date)]
		public DateTime? ContractStartDate { get; set; }// Ngày bắt đầu hợp đồng

		[DataType(DataType.Date)]
		public DateTime? ContractEndDate { get; set; } // Ngày kết thúc hợp đồng

		[StringLength(50)]
		public string? EmploymentType { get; set; } // Viên chức, hợp đồng, thỉnh giảng,...

		[Required]
		public bool IsActive { get; set; } = true; // Trạng thái đang công tác

		[StringLength(500)]
		public string? Notes { get; set; } // Ghi chú thêm

		[StringLength(255)]
		public string? AvatarUrl { get; set; } // Ảnh đại diện

		[NotMapped] // để EF không tạo cột trong bảng database
		
		public IFormFile? AvatarFile { get; set; }


	}
}
