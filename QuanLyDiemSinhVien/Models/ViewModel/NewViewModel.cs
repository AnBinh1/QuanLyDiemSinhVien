namespace QuanLyDiemSinhVien.Models.ViewModel
{
    public class NewViewModel
    {
        public News? FeaturedNews { get; set; }
        public List<News> LatestNews { get; set; } = new();
        public List<News> Announcements { get; set; } = new();
    }
}
