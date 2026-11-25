namespace QuanLyDiemSinhVien.Models
{
    public class LecturerDashboardViewModel
    {
        // Hồ sơ cá nhân
        public string LecturerCode { get; set; } = "";
        public string FullName { get; set; } = "";
        public string FacultyName { get; set; } = "";
        public string Position { get; set; } = "";
        public string? AvatarUrl { get; set; }

        // Giảng dạy
        public List<TeachingScheduleItem> TodaySchedule { get; set; } = new();
        public List<SectionProgressItem> Sections { get; set; } = new();

        // Công việc
        public List<LecturerTaskItem> UpcomingTasks { get; set; } = new();
        public List<LecturerTaskItem> OverdueTasks { get; set; } = new();

        // Điểm danh
        public List<RecentAttendanceItem> RecentAttendances { get; set; } = new();
        public int SessionsNotTaken { get; set; }

        // Điểm số
        public int SectionsPendingGrade => Sections.Count(s => !s.IsGradeLocked && s.GradedStudents < s.TotalStudents);
    }

    public class TeachingScheduleItem
    {
        public int SectionId { get; set; }
        public string SectionCode { get; set; } = "";
        public string CourseName { get; set; } = "";
        public string Room { get; set; } = "";
        public int? StartPeriod { get; set; }
        public int? EndPeriod { get; set; }
        public int StudentCount { get; set; }
    }

    public class LecturerTaskItem
    {
        public string Title { get; set; } = "";
        public string Type { get; set; } = "Grade"; // Grade | Attendance
        public int SectionId { get; set; }
        public string SectionCode { get; set; } = "";
        public DateTime DueDate { get; set; }
    }

    public class SectionProgressItem
    {
        public int SectionId { get; set; }
        public string SectionCode { get; set; } = "";
        public string CourseName { get; set; } = "";
        public int TotalStudents { get; set; }
        public int GradedStudents { get; set; }
        public bool IsGradeLocked { get; set; }
    }

    public class RecentAttendanceItem
    {
        public int SectionId { get; set; }
        public string SectionCode { get; set; } = "";
        public int SessionNumber { get; set; }
        public DateTime Date { get; set; }
        public int Present { get; set; }
        public int Total { get; set; }
    }
}