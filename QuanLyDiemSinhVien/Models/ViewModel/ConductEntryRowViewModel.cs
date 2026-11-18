using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemSinhVien.Models.ViewModel
{
    public class ConductEntryRowViewModel
    {
        public int StudentId { get; set; }

        // Chỉ để hiển thị, không gửi lại từ form nên phải là nullable
        public string? StudentCode { get; set; }
        public string? StudentName { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [Range(0, 100, ErrorMessage = "Điểm từ 0 đến 100")]
        public int? Score { get; set; }              // null = chưa nhập
    }
}