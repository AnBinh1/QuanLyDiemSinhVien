
namespace QuanLyDiemSinhVien.ViewModels
{
    // Dữ liệu tổng hợp của trang Dashboard Admin
    public class AdminDashboardViewModel
    {
        public string AdminName { get; set; } = "";
        public DateTime Today { get; set; }

        // Thống kê tổng quan
        public int TotalStudents { get; set; }
        public int TotalLecturers { get; set; }
        public int TotalFaculties { get; set; }
        public int TotalMajors { get; set; }
        public int TotalCourseSections { get; set; }
        public int TotalEnrollments { get; set; }

        // Biểu đồ sinh viên theo khoa
        public List<string> FacultyNames { get; set; } = new();
        public List<int> FacultyCounts { get; set; } = new();

        // Biểu đồ sinh viên theo giới tính
        public List<string> GenderLabels { get; set; } = new();
        public List<int> GenderCounts { get; set; } = new();

        // Danh sách lớp còn thiếu điểm
        public List<IncompleteGradeSectionViewModel>
            IncompleteGradeSections
        { get; set; } = new();

        // Danh sách lớp sắp hoặc vừa kết thúc
        public List<UpcomingSectionViewModel>
            UpcomingSections
        { get; set; } = new();
    }

    // Thông tin lớp học phần còn thiếu điểm
    public class IncompleteGradeSectionViewModel
    {
        public string SectionCode { get; set; } = "";
        public string Semester { get; set; } = "";
        public string AcademicYear { get; set; } = "";

        public int EnrolledCount { get; set; }
        public int MissingCount { get; set; }

        public bool IsGradeLocked { get; set; }
    }

    // Thông tin thời điểm kết thúc lớp học phần
    public class UpcomingSectionViewModel
    {
        public string SectionCode { get; set; } = "";
        public DateTime EndDate { get; set; }
        public int MissingCount { get; set; }
    }
}
