using Microsoft.AspNetCore.Identity;

namespace QuanLyDiemSinhVien.Models.ViewModel
{
	public class UserWithRoleViewModel
	{
		public IdentityUser User { get; set; }
		public List<string> Roles { get; set; }
	}
}
