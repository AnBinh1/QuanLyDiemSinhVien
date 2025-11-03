using QuanLyDiemSinhVien.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDiemSinhVien.Models
{
	public class MajorModel
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int MajorId { get; set; }

		[Required(ErrorMessage = "Yêu cầu nhập mã ngành học")]
		[NotEmptyOrWhiteSpace(ErrorMessage = "Mã Ngành học không được để trống hoặc toàn khoảng trắng")]
		[StringLength(10, MinimumLength = 2, ErrorMessage = "Mã Ngành học từ 2 đến 10 ký tự")]
		[RegularExpression(@"^[A-Za-z0-9_]+$", ErrorMessage = "Mã Ngành học chỉ được chứa chữ, số và dấu gạch dưới, không có khoảng trắng")]
		public string? MajorCode { get; set; } = "";

		[Required(ErrorMessage = "Yêu cầu nhập tên ngành học")]
		[NotEmptyOrWhiteSpace(ErrorMessage = "Tên Ngành học không được để trống hoặc toàn khoảng trắng")]
		[StringLength(255, MinimumLength = 2, ErrorMessage = "Tên ngành học từ 2 đến 255 ký tự")]
		[RegularExpression(@"^(?!.*\s{2,})[a-zA-ZÀ-ỹ\s]+$", ErrorMessage = "Tên khoa-viện chỉ được chứa chữ cái, không có ký tự đặc biệt và không có quá 1 khoảng trắng liên tiếp")]

		public string? MajorName { get; set; } = "";


		[Required(ErrorMessage = "Yêu cầu nhập mô tả ngành học")]
		[NotEmptyOrWhiteSpace(ErrorMessage = "Mô tả không được để trống hoặc toàn khoảng trắng")]
		[StringLength(500, MinimumLength = 5, ErrorMessage = "Mô tả phải từ 5 đến 500 ký tự")]
		[RegularExpression(@"^(?!.*\s{2,}).+$", ErrorMessage = "Mô tả không được có 2 khoảng trắng liên tiếp.")]
		public string? MajorDescription { get; set; }

		[Required(ErrorMessage = "Yêu cầu chọn khoa/viện")]
		public int? FacultyId { get; set; }

		[ForeignKey("FacultyId")]
		public FacultyModel? Faculty { get; set; }

		public ICollection<ClassModel>? Classes { get; set; }

		public void TrimFields()
		{
			MajorCode = MajorCode.Trim();
			MajorName = MajorName.Trim();
			MajorDescription = MajorDescription.Trim();
			Faculty.FacultyName = Faculty.FacultyName.Trim();
		}
	}
}
