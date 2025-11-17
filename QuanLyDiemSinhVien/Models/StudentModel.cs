using QuanLyDiemSinhVien.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDiemSinhVien.Models
{
	public class StudentModel
	{
		[Key]
		public int StudentId { get; set; }

		[Required(ErrorMessage = "Yêu cầu nhập mã sinh viên")]
		[NotEmptyOrWhiteSpace(ErrorMessage = "Mã sinh viên không được để trống hoặc toàn khoảng trắng")]
		[StringLength(10, MinimumLength = 5, ErrorMessage = "Mã sinh viên phải từ 5 đến 10 ký tự")]
		[RegularExpression(@"^[0-9]+$", ErrorMessage = "Mã sinh viên chỉ được chứa chữ số")]

		public string StudentCode { get; set; }

		[Required(ErrorMessage = "Yêu cầu nhập họ và tên sinh viên")]
		[NotEmptyOrWhiteSpace(ErrorMessage = "Họ và tên không được để trống hoặc toàn khoảng trắng")]
		[StringLength(100, MinimumLength = 2, ErrorMessage = "Họ và tên phải từ 2 đến 100 ký tự")]
		[RegularExpression(@"^(?!.*\s{2,})[A-Za-zÀ-ỹ\s]+$", ErrorMessage = "Họ và tên chỉ được chứa chữ cái, không có ký tự đặc biệt và không có quá 1 khoảng trắng liên tiếp")]
		public string FullName { get; set; }

		[Required(ErrorMessage = "Yêu cầu chọn giới tính")]
		[RegularExpression(@"^(Nam|Nữ)$", ErrorMessage = "Giới tính chỉ được chọn Nam hoặc Nữ")]
		public string Gender { get; set; }

		[Required(ErrorMessage = "Yêu cầu nhập ngày sinh")]
		[DataType(DataType.Date)]
		[CustomValidation(typeof(StudentModel), nameof(ValidateAge))]
		public DateTime DateOfBirth { get; set; }
		public static ValidationResult? ValidateAge(DateTime dob, ValidationContext context)
		{
			if (dob > DateTime.Now.AddYears(-17))
				return new ValidationResult("Sinh viên phải ít nhất 17 tuổi");
			return ValidationResult.Success;
		}

		[Required(ErrorMessage = "Yêu cầu nhập địa chỉ sinh viên")]
		[NotEmptyOrWhiteSpace(ErrorMessage = "Địa chỉ không được để trống hoặc toàn khoảng trắng")]
		[StringLength(255, MinimumLength = 5, ErrorMessage = "Địa chỉ phải từ 5 đến 255 ký tự")]
		public string Address { get; set; }

		[Required(ErrorMessage = "Yêu cầu nhập số điện thoại")]
		[RegularExpression(@"^(0\d{9,10})$", ErrorMessage = "Số điện thoại không hợp lệ (phải bắt đầu bằng 0 và có 10–11 số)")]
		public string PhoneNumber { get; set; }

		[Required(ErrorMessage = "Yêu cầu nhập địa chỉ Email")]
		[EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ")]
		[StringLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự")]
		public string? Email { get; set; }

		[Required(ErrorMessage = "Yêu cầu nhập khoá đào tạo")]
		[RegularExpression(@"^\d{4}-\d{4}$", ErrorMessage = "Năm học phải theo định dạng yyyy-yyyy")]
		[StringLength(9, ErrorMessage = "Năm học phải theo dạng yyyy-yyyy, tối đa 9 ký tự")]
		public string YearOfStudy { get; set; }


		[Required(ErrorMessage = "Yêu cầu nhập ngày nhập học")]
		[DataType(DataType.Date)]
		[CustomValidation(typeof(StudentModel), nameof(ValidateEnrollmentDate))]
		public DateTime EnrollmentDate { get; set; }

		public static ValidationResult? ValidateEnrollmentDate(DateTime date, ValidationContext context)
		{
			if (date > DateTime.Now)
				return new ValidationResult("Ngày nhập học không được lớn hơn ngày hiện tại");
			return ValidationResult.Success;
		}


		[Required(ErrorMessage = "Yêu cầu nhập ngày nhập học")]
		public bool IsActive { get; set; } = true;

		[Required(ErrorMessage = "Yêu cầu chọn lớp học")]
		public int ClassId { get; set; }

		[ForeignKey("ClassId")]
		public ClassModel? Class { get; set; }

		[NotMapped]
		public int? MajorId { get; set; }

		[NotMapped]
		public int? FacultyId { get; set; }
	}
}
