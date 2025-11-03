using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemSinhVien.Attributes
{
	public class NotEmptyOrWhiteSpaceAttribute : ValidationAttribute
	{
		protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
		{
			if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
			{
				return new ValidationResult(ErrorMessage ?? "Trường không được để trống hoặc toàn khoảng trắng");
			}

			return ValidationResult.Success;
		}
	}
}
