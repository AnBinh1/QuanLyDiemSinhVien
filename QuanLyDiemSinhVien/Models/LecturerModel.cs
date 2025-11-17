using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDiemSinhVien.Models
{
	public class LecturerModel
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int LecturerId { get; set; } // Khóa chính, tự tăng

		[Required, StringLength(10)]
		public string LecturerCode { get; set; } // Mã giảng viên, duy nhất

		[Required, StringLength(100)]
		public string FullName { get; set; } // Họ tên đầy đủ

		[Required]
		[RegularExpression(@"^(Nam|Nữ)$", ErrorMessage = "Giới tính chỉ được chọn Nam hoặc Nữ")]
		public string Gender { get; set; } // Giới tính

		[Required, DataType(DataType.Date)]
		public DateTime DateOfBirth { get; set; } // Ngày sinh

		[StringLength(255)]
		public string? PlaceOfBirth { get; set; } // Nơi sinh

		[StringLength(50)]
		public string? Ethnicity { get; set; } // Dân tộc

		[StringLength(50)]
		public string? Religion { get; set; } // Tôn giáo

		[StringLength(50)]
		public string? Nationality { get; set; } // Quốc tịch

		[StringLength(20)]
		public string? IdentityNumber { get; set; } // Số CMND/CCCD

		[DataType(DataType.Date)]
		public DateTime? IdentityDate { get; set; } // Ngày cấp CMND/CCCD

		[StringLength(255)]
		public string? IdentityPlace { get; set; } // Nơi cấp CMND/CCCD

		[StringLength(255)]
		public string Address { get; set; } // Địa chỉ hiện tại

		[StringLength(15)]
		public string PhoneNumber { get; set; } // Số điện thoại

		[EmailAddress, StringLength(255)]
		public string? Email { get; set; } // Email


		[ForeignKey("User")]
		public string? UserId { get; set; }  // Id của IdentityUser
		public IdentityUser? User { get; set; }


	// Công tác
		[Required]
		public int FacultyId { get; set; } // Mã khoa/viện
		[ForeignKey("FacultyId")]
		public FacultyModel? Faculty { get; set; } // Liên kết với bảng Khoa

		public int? MajorId { get; set; } // Nếu giảng viên thuộc bộ môn cụ thể
		[ForeignKey("MajorId")]
		public MajorModel? Major { get; set; } // Liên kết với bảng Ngành/Bộ môn

		[StringLength(50)]
		public string? Position { get; set; } // Chức danh: Trưởng bộ môn, Giảng viên chính,...

		[StringLength(50)]
		public string? AcademicPosition { get; set; } // Học hàm: Giáo sư, Phó giáo sư, TS,...

		[StringLength(50)]
		public string? AcademicDegree { get; set; } // Học vị: Thạc sĩ, Tiến sĩ (tách rõ so với học hàm)

		[DataType(DataType.Date)]
		public DateTime? JoinDate { get; set; } // Ngày vào trường

		[DataType(DataType.Date)]
		public DateTime? StartTeachingDate { get; set; } // Ngày bắt đầu giảng dạy


		[DataType(DataType.Date)]
		public DateTime? RetireDate { get; set; } // Ngày nghỉ hưu hoặc thôi việc

		[StringLength(50)]
		public string? ContractType { get; set; } // Loại hợp đồng: Dài hạn, Ngắn hạn, Thử việc,...

		[DataType(DataType.Date)]
		public DateTime? ContractStartDate { get; set; }// Ngày bắt đầu hợp đồng

		[DataType(DataType.Date)]
		public DateTime? ContractEndDate { get; set; } // Ngày kết thúc hợp đồng

		[StringLength(50)]
		public string? EmploymentType { get; set; } // Viên chức, hợp đồng, thỉnh giảng,...

		[Required]
		public bool IsActive { get; set; } = true; // Trạng thái đang công tác

		[StringLength(500)]
		public string? Notes { get; set; } // Ghi chú thêm

		[StringLength(255)]
		public string? AvatarUrl { get; set; } // Ảnh đại diện

		[NotMapped] // để EF không tạo cột trong bảng database
		public IFormFile? AvatarFile { get; set; }


	}
}
