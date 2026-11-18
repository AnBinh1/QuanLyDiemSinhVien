using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemSinhVien.Models.ViewModel
{
    public class ConductReplyViewModel
    {
        public int ScoreId { get; set; }

        // Chỉ để hiển thị (nullable để không bị coi là bắt buộc khi gửi form)
        public string? StudentCode { get; set; }
        public string? StudentName { get; set; }
        public string? SemesterName { get; set; }
        public int Score { get; set; }
        public string? Feedback { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung trả lời")]
        [StringLength(1000, ErrorMessage = "Tối đa 1000 ký tự")]
        [Display(Name = "Nội dung trả lời")]
        public string? Reply { get; set; }

        // Để quay lại đúng bộ lọc sau khi lưu
        public int? ReturnClassId { get; set; }
        public int? ReturnSemesterId { get; set; }
    }
}