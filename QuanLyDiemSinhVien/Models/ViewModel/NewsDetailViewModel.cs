namespace QuanLyDiemSinhVien.Models.ViewModel
{
    public class NewsDetailViewModel
    {
        public News Article { get; set; } = null!;
        public List<News> SameCategory { get; set; } = new();   // cùng chuyên mục
        public List<News> Related { get; set; } = new();
    }
}
