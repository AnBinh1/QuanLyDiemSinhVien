using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemSinhVien.Models
{
	public class RoleSelection
	{
		[Required(ErrorMessage ="Yêu cầu nhập tên vai trò")]
		public string RoleName { get; set; } = "";
		public bool IsSelected { get; set; }
	}
}
