namespace QuanLyDiemSinhVien.Models.ViewModel
{
    public class ConductScoreRowViewModel
    {
        public int RowNumber { get; set; }
        public int ScoreId { get; set; }
        public string AcademicYear { get; set; } = "";
        public string TermName { get; set; } = "";
        public string ScoreType { get; set; } = "Chính thức";
        public int Score { get; set; }
        public string Rank { get; set; } = "";
        public int? YearScore { get; set; }
        public string? YearRank { get; set; }
        public int CumulativeScore { get; set; }
        public string CumulativeRank { get; set; } = "";
        public string? Feedback { get; set; }
        public string? Reply { get; set; }
        public string? RepliedBy { get; set; }
        public DateTime? RepliedAt { get; set; }
    }
}