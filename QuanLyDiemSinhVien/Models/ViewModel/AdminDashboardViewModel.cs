namespace QuanLyDiemSinhVien.ViewModels
{
    public class AdminDashboardViewModel
    {
        public string AdminName { get; set; } = "";

        // Số liệu theo từng mục menu
        public int TotalUsers { get; set; }
        public int TotalFaculties { get; set; }
        public int TotalMajors { get; set; }
        public int TotalClasses { get; set; }
        public int TotalStudents { get; set; }
        public int ActiveStudents { get; set; }
        public int TotalLecturers { get; set; }
        public int ActiveLecturers { get; set; }
        public int TotalCourses { get; set; }
        public int TotalSections { get; set; }
        public int TotalNews { get; set; }

        // Biểu đồ
        public List<ChartItem> StudentsByFaculty { get; set; } = new();
        public List<ChartItem> StudentsByGender { get; set; } = new();

        // Việc cần xử lý
        public List<TodoItem> Todos { get; set; } = new();

        // Hạn khóa điểm
        public List<SectionDeadlineItem> UpcomingDeadlines { get; set; } = new();
        public List<SectionDeadlineItem> OverdueDeadlines { get; set; } = new();

        //public List<NewsItem> LatestNews { get; set; } = new();
    }

    public class ChartItem
    {
        public string Label { get; set; } = "";
        public int Value { get; set; }
    }

    public class TodoItem
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public int Count { get; set; }
        public string Icon { get; set; } = "fa-tasks";
        public string Color { get; set; } = "warning"; // warning | danger | info
        public string Controller { get; set; } = "";
        public string Action { get; set; } = "Index";
    }

    public class SectionDeadlineItem
    {
        public int SectionId { get; set; }
        public string SectionCode { get; set; } = "";
        public string CourseName { get; set; } = "";
        public string LecturerName { get; set; } = "";
        public DateTime EndDate { get; set; }
    }

    //public class NewsItem
    //{
    //    public int Id { get; set; }
    //    public string Title { get; set; } = "";
    //    public string Category { get; set; } = "";
    //    public DateTime PublishedDate { get; set; }
    //    public bool IsPublished { get; set; }
    //}
}