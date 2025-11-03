using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemSinhVien.Models.ViewModel
{
	// Custom validation: không cho phép null, rỗng hoặc toàn khoảng trắng
	public class NotEmptyOrWhiteSpaceAttribute : ValidationAttribute
	{
		protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
		{
			if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
				return new ValidationResult(ErrorMessage ?? "Trường không được để trống hoặc toàn khoảng trắng");
			return ValidationResult.Success;
		}
	}

	public class LoginViewModel
	{
		[Required(ErrorMessage = "Yêu cầu nhập tên người dùng")]
		[NotEmptyOrWhiteSpace(ErrorMessage = "Tên người dùng không được để trống hoặc toàn khoảng trắng")]
		[StringLength(50, MinimumLength = 3, ErrorMessage = "Tên người dùng phải từ 3 đến 50 ký tự")]
		[RegularExpression(@"^[A-Za-z0-9_]+$", ErrorMessage = "Tên người dùng chỉ được chứa chữ, số và dấu gạch dưới (không có khoảng trắng)")]
		public string UserName { get; set; } = string.Empty;

		[Required(ErrorMessage = "Yêu cầu nhập mật khẩu người dùng")]
		[NotEmptyOrWhiteSpace(ErrorMessage = "Mật khẩu không được để trống hoặc toàn khoảng trắng")]
		[StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 100 ký tự")]
		[DataType(DataType.Password)]
		public string Password { get; set; } = string.Empty;

		public bool RememberMe { get; set; } = false;
	}
}
