namespace QuanLyDiemSinhVien.Models.ViewModel
{
    public class ConductClassRowViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = "";
        public string MajorName { get; set; } = "";
        public string FacultyName { get; set; } = "";
        public int Total { get; set; }          // sĩ số
        public int Scored { get; set; }         // số sinh viên đã có điểm trong học kỳ
        public double? Average { get; set; }    // điểm trung bình của những người đã có điểm

        public int Percent => Total == 0 ? 0 : (int)Math.Round(Scored * 100.0 / Total);
    }

    public class ConductClassListViewModel
    {
        public int? SemesterId { get; set; }
        public string SemesterName { get; set; } = "";
        public string? Keyword { get; set; }
        public bool OnlyIncomplete { get; set; }

        public int ClassCount { get; set; }      // số lớp có sinh viên
        public int CompleteCount { get; set; }   // số lớp đã nhập đủ

        public List<ConductClassRowViewModel> Rows { get; set; } = new();
    }
}