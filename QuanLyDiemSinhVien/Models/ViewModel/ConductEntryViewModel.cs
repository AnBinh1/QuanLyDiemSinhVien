namespace QuanLyDiemSinhVien.Models.ViewModel
{
    public class ConductEntryViewModel
    {
        public int? ClassId { get; set; }
        public int? SemesterId { get; set; }
        public string? ClassName { get; set; }       // hiển thị
        public string? SemesterName { get; set; }    // hiển thị
        public List<ConductEntryRowViewModel> Rows { get; set; } = new();
    }
}