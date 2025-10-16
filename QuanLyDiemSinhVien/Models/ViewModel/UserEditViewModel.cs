using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemSinhVien.Models.ViewModel
{
	public class UserEditViewModel
	{
		public string? Id { get; set; }
		[Required(ErrorMessage = "Yêu cầu nhập tên người dùng")]
		public string UserName { get; set; } = "";
		[Required(ErrorMessage = "Yêu cầu nhập Email người dùng")]
		[EmailAddress(ErrorMessage = "Email không đúng định dạng")]
		public string Email { get; set; } = "";
		[Required(ErrorMessage = "Yêu cầu nhập mật khẩu mới(nếu muốn đổi mật khẩu)")]
		[DataType(DataType.Password)]
		public string NewPassword { get; set; }
		[Required(ErrorMessage = "Yêu cầu nhập số điện thoại dùng")]
		public string PhoneNumber { get; set; } = "";


	}
}
