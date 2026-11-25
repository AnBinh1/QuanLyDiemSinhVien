namespace QuanLyDiemSinhVien.Models.ViewModel
{
    public class GradeRecord
    {
        public string CourseCode { get; set; } = "";
        public int Credits { get; set; }
        public string Semester { get; set; } = "";      // "HK 1", "HK 2.1", "HK 2.2"
        public string AcademicYear { get; set; } = "";  // "2025-2026"
        public double TotalScore { get; set; }
        public bool IncludeInGraduation { get; set; } = true;
    }

    public class SemesterSummaryRow
    {
        public int Index { get; set; }
        public string SemesterName { get; set; } = "";

        // Điểm TBC (tích lũy)
        public double CumGpa4 { get; set; }
        public double CumGpa10 { get; set; }

        // Điểm TBC (học kỳ)
        public double SemGpa4 { get; set; }
        public double SemGpa10 { get; set; }

        public int AccumulatedCredits { get; set; } // TC tích lũy
        public int DebtCredits { get; set; }        // TC nợ
        public int RegisteredCredits { get; set; }  // TC đăng ký
        public int NotCountedCredits { get; set; }  // Chưa tính
    }

    public class TranscriptSummaryViewModel
    {
        public string StudentName { get; set; } = "";
        public string StudentCode { get; set; } = "";
        public List<SemesterSummaryRow> Rows { get; set; } = new();
    }
}