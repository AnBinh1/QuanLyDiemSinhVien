using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemSinhVien.Models
{
    public class SemesterModel
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(20)]
        public string AcademicYear { get; set; } = "";   // "2025-2026"

        public int StartYear { get; set; }               // 2025
        public int Term { get; set; }                    // 1 hoặc 2

        // Thuộc tính tính toán, KHÔNG tạo cột trong database
        public string TermName => Term == 1 ? "Học kỳ 1" : "Học kỳ 2";
        public string DisplayName => $"{TermName} - {AcademicYear}";
    }
}
