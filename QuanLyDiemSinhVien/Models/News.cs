using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemSinhVien.Models
{
    public class News
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [StringLength(300)]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tóm tắt")]
        [StringLength(500)]
        [Display(Name = "Tóm tắt")]
        public string Summary { get; set; } = string.Empty;

        [Display(Name = "Nội dung")]
        public string? Content { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập chuyên mục")]
        [StringLength(50)]
        [Display(Name = "Chuyên mục")]
        public string Category { get; set; } = "THÔNG BÁO";

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [Display(Name = "Ngày đăng")]
        [DataType(DataType.Date)]
        public DateTime PublishedDate { get; set; } = DateTime.Today;

        [Display(Name = "Tin nổi bật")]
        public bool IsFeatured { get; set; }

        [Display(Name = "Hiển thị")]
        public bool IsPublished { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}