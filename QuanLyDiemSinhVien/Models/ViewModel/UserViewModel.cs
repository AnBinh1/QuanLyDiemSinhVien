using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemSinhVien.Models.ViewModel
{
	public class UserViewModel
	{
		public string? Id { get; set; }
		[Required(ErrorMessage = "Yêu cầu nhập tên người dùng")]
		public string UserName { get; set; } = "";
		[Required(ErrorMessage = "Yêu cầu nhập Email người dùng")]
		[EmailAddress(ErrorMessage = "Email không đúng định dạng")]
		public string Email { get; set; } = "";
		[Required(ErrorMessage = "Yêu cầu nhập Mật khẩu người dùng")]
		[DataType(DataType.Password)]
		public string PasswordHash { get; set; }
		[Required(ErrorMessage = "Yêu cầu nhập số điện thoại dùng")]
		public string PhoneNumber { get; set; } = "";
	}
}
