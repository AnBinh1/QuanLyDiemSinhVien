
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Repository;
using QuanLyDiemSinhVien.ViewModels;

namespace QuanLyDiemSinhVien.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class HomeController : Controller
    {
        private readonly DataContext _context;

        public HomeController(DataContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            // 1. Lấy thông tin Admin đang đăng nhập
            var model = new AdminDashboardViewModel
            {
                AdminName = User.Identity?.Name ?? "Quản trị viên",
                Today = today
            };

            // 2. Thống kê tổng quan hệ thống
            model.TotalStudents = await _context.Students.CountAsync();
            model.TotalLecturers = await _context.Lecturers.CountAsync();
            model.TotalFaculties = await _context.Faculties.CountAsync();
            model.TotalMajors = await _context.Majors.CountAsync();
            model.TotalCourseSections = await _context.CourseSections.CountAsync();
            model.TotalEnrollments = await _context.Enrollments.CountAsync();

            // 3. Thống kê sinh viên theo khoa
            var facultyStats = await _context.Students
                .Where(s => s.Class != null &&
                            s.Class.Major != null &&
                            s.Class.Major.Faculty != null)
                .GroupBy(s => s.Class!.Major!.Faculty!.FacultyName)
                .Select(g => new
                {
                    Name = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            model.FacultyNames = facultyStats
                .Select(x => x.Name)
                .ToList();

            model.FacultyCounts = facultyStats
                .Select(x => x.Count)
                .ToList();

            // 4. Thống kê sinh viên theo giới tính
            var genderStats = await _context.Students
                .GroupBy(s => s.Gender)
                .Select(g => new
                {
                    Gender = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            model.GenderLabels = genderStats
                .Select(x => string.IsNullOrWhiteSpace(x.Gender)
                    ? "Chưa cập nhật"
                    : x.Gender)
                .ToList();

            model.GenderCounts = genderStats
                .Select(x => x.Count)
                .ToList();

            // 5. Kiểm tra lớp học phần còn thiếu điểm
            var sectionStats = await _context.CourseSections
                .Select(s => new
                {
                    s.SectionId,
                    s.SectionCode,
                    s.Semester,
                    s.AcademicYear,
                    s.EndDate,
                    s.IsGradeLocked,

                    // Tổng số sinh viên đăng ký lớp
                    EnrolledCount = _context.Enrollments
                        .Count(e => e.SectionId == s.SectionId),

                    // Số sinh viên chưa có bản ghi điểm
                    MissingCount = _context.Enrollments
                        .Count(e => e.SectionId == s.SectionId &&
                            !_context.Grades.Any(g =>
                                g.EnrollmentId == e.EnrollmentId))
                })
                .ToListAsync();

            // Chỉ hiển thị lớp thực sự còn thiếu điểm
            model.IncompleteGradeSections = sectionStats
                .Where(s => s.MissingCount > 0)
                .OrderByDescending(s => s.MissingCount)
                .Select(s => new IncompleteGradeSectionViewModel
                {
                    SectionCode = s.SectionCode,
                    Semester = s.Semester,
                    AcademicYear = s.AcademicYear,
                    EnrolledCount = s.EnrolledCount,
                    MissingCount = s.MissingCount,
                    IsGradeLocked = s.IsGradeLocked
                })
                .ToList();

            // 6. Các lớp có ngày kết thúc trong khoảng 30 ngày
            // trước hoặc sau ngày hiện tại
            model.UpcomingSections = sectionStats
                .Where(s => s.EndDate.HasValue &&
                    s.EndDate.Value.Date >= today.AddDays(-30) &&
                    s.EndDate.Value.Date <= today.AddDays(30))
                .OrderBy(s => s.EndDate)
                .Select(s => new UpcomingSectionViewModel
                {
                    SectionCode = s.SectionCode,
                    EndDate = s.EndDate!.Value,
                    MissingCount = s.MissingCount
                })
                .ToList();

            return View(model);
        }
    }
}
