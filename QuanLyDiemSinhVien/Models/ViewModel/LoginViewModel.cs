using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemSinhVien.Models.ViewModel
{
	public class LoginViewModel
	{
		[Required(ErrorMessage = "Vui lòng nhập UserName")]
		public string UserName { get; set; } = string.Empty;
		[Required(ErrorMessage = "Mật khẩu không được để trống")]
		[DataType(DataType.Password)]
		public string Password { get; set; } = string.Empty;
		[Display(Name = "Nhớ đăng nhập")]
		public bool RememberMe { get; set; }
	}
}
