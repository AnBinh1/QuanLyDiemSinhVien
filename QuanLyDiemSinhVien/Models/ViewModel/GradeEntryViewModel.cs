using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemSinhVien.Models.ViewModel
{
    public class GradeEntryViewModel
    {
        
            // =========================
            // THÔNG TIN LỚP HỌC PHẦN
            // =========================

            public int SectionId { get; set; }

            public string? SectionCode { get; set; }

            public string? CourseName { get; set; }

            // Số sinh viên đăng ký học phần
            public int StudentCount { get; set; }

            // Danh sách sinh viên cần nhập điểm
            public List<GradeEntryItemViewModel> Students { get; set; } = new();
        }


        // Một dòng sinh viên trong bảng nhập điểm
        public class GradeEntryItemViewModel
        {
            // =========================
            // THÔNG TIN ĐĂNG KÝ
            // =========================

            // Khóa chính của Enrollment
            public int EnrollmentId { get; set; }

            // =========================
            // THÔNG TIN SINH VIÊN
            // =========================

            public string? StudentCode { get; set; }

            public string? FullName { get; set; }

            // =========================
            // ĐIỂM
            // =========================

            [Range(0, 10, ErrorMessage = "Điểm quá trình phải từ 0 đến 10")]
            public decimal ContinuousAssessment { get; set; }

            [Range(0, 10, ErrorMessage = "Điểm cuối kỳ phải từ 0 đến 10")]
            public decimal FinalExam { get; set; }

            // =========================
            // GHI CHÚ
            // =========================

            public string? Notes { get; set; }


            // =========================
            // ĐIỂM TỰ TÍNH
            // =========================

            public decimal TotalScore =>
                Math.Round(
                    (ContinuousAssessment + FinalExam) / 2,
                    2
                );

        public string LetterGrade
        {
            get
            {
                if (TotalScore >= 9.5m)
                    return "A+";

                if (TotalScore >= 8.5m)
                    return "A";

                if (TotalScore >= 8.0m)
                    return "B+";

                if (TotalScore >= 7.0m)
                    return "B";

                if (TotalScore >= 6.5m)
                    return "C+";

                if (TotalScore >= 5.5m)
                    return "C";

                if (TotalScore >= 5.0m)
                    return "D+";

                if (TotalScore >= 4.0m)
                    return "D";

                return "F";
            }
        }
    }
    
}
