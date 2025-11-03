using QuanLyDiemSinhVien.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDiemSinhVien.Models
{
	public class ClassModel
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int ClassId { get; set; }

		[Required(ErrorMessage = "Yêu cầu nhập tên lớp học")]
		[NotEmptyOrWhiteSpace(ErrorMessage = "Tên lớp học không được để trống hoặc toàn khoảng trắng")]
		[StringLength(500, MinimumLength = 2, ErrorMessage = "Tên Lớp học từ 2 đến 500 ký tự")]
		[RegularExpression(@"^[A-Za-z0-9_]+$", ErrorMessage = "Tên Lớp học chỉ được chứa chữ, số và dấu gạch dưới, không có khoảng trắng")]
		public string? ClassName { get; set; }

		[Required(ErrorMessage = "Yêu cầu nhập mô tả lớp học")]
		[NotEmptyOrWhiteSpace(ErrorMessage = "Mô tả không được để trống hoặc toàn khoảng trắng")]
		[StringLength(500, MinimumLength = 5, ErrorMessage = "Mô tả phải từ 5 đến 500 ký tự")]
		[RegularExpression(@"^(?!.*\s{2,}).+$", ErrorMessage = "Mô tả không được có 2 khoảng trắng liên tiếp.")]
		public string? ClassDescription { get; set; }

		[Required(ErrorMessage = "Vui lòng chọn Ngành học!")]
		public int MajorId { get; set; }

		[ForeignKey("MajorId")]
		public MajorModel? Major { get; set; }

		public ICollection<StudentModel>? Students { get; set; }

		public void TrimFields()
		{
			ClassName = ClassName.Trim();
			ClassDescription = ClassDescription.Trim();
			Major.MajorName = Major.MajorName.Trim();
			Major.Faculty.FacultyName = Major.Faculty.FacultyName.Trim();
		}
	}
}
