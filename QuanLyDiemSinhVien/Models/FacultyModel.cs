using QuanLyDiemSinhVien.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDiemSinhVien.Models
{
	public class FacultyModel
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required(ErrorMessage = "Yêu cầu nhập mã khoa-viện")]
		[NotEmptyOrWhiteSpace(ErrorMessage = "Mã khoa-viện không được để trống hoặc toàn khoảng trắng")]
		[StringLength(10, MinimumLength = 2, ErrorMessage = "Mã khoa-viện từ 2 đến 10 ký tự")]
		[RegularExpression(@"^[A-Za-z0-9_]+$", ErrorMessage = "Mã khoa-viện chỉ được chứa chữ, số và dấu gạch dưới, không có khoảng trắng")]
		public string FacultyCode { get; set; } = "";

		[Required(ErrorMessage = "Yêu cầu nhập tên khoa-viện")]
		[NotEmptyOrWhiteSpace(ErrorMessage = "Tên khoa-viện không được để trống hoặc toàn khoảng trắng")]
		[StringLength(255, MinimumLength = 2, ErrorMessage = "Tên khoa-viện từ 2 đến 255 ký tự")]
		[RegularExpression(@"^(?!.*\s{2,})[a-zA-ZÀ-ỹ\s]+$", ErrorMessage = "Tên khoa-viện chỉ được chứa chữ cái, không có ký tự đặc biệt và không có quá 1 khoảng trắng liên tiếp")]
		public string FacultyName { get; set; } = "";


		[Required(ErrorMessage = "Yêu cầu nhập mô tả khoa-viện")]
		[NotEmptyOrWhiteSpace(ErrorMessage = "Mô tả không được để trống hoặc toàn khoảng trắng")]
		[StringLength(500, MinimumLength = 5, ErrorMessage = "Mô tả phải từ 5 đến 500 ký tự")]
		[RegularExpression(@"^(?!.*\s{2,}).+$", ErrorMessage = "Mô tả không được có 2 khoảng trắng liên tiếp.")]
		public string FacultyDescription { get; set; } = "";

		public ICollection<MajorModel>? Majors { get; set; }

		public void TrimFields()
		{
			FacultyCode = FacultyCode.Trim();
			FacultyName = FacultyName.Trim();
			FacultyDescription = FacultyDescription.Trim();
		}
	}
}
