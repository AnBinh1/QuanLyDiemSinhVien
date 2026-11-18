using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDiemSinhVien.Models
{
    [Index(nameof(StudentId), nameof(SemesterId), IsUnique = true)]
    public class ConductScoreModel
    {
        [Key]
        public int Id { get; set; }

        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public StudentModel? Student { get; set; }

        public int SemesterId { get; set; }
        [ForeignKey("SemesterId")]
        public SemesterModel? Semester { get; set; }

        [Range(0, 100)]
        public int Score { get; set; }

        [StringLength(1000)]
        public string? Feedback { get; set; }          // phản hồi của sinh viên

        [StringLength(1000)]
        public string? AdminReply { get; set; }        // admin trả lời
        [StringLength(100)]
        public string? RepliedBy { get; set; }
        public DateTime? RepliedAt { get; set; }

        [Required, StringLength(100)]
        public string CreatedBy { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required, StringLength(100)]
        public string UpdatedBy { get; set; } = "";
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}