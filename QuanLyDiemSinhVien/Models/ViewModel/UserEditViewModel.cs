using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemSinhVien.Models.ViewModel
{
	public class UserEditViewModel
	{
		public string? Id { get; set; }

		public string UserName { get; set; } = "";

		public string Email { get; set; } = "";
		
		public string NewPassword { get; set; }
		
		public string PhoneNumber { get; set; } = "";


	}
}
