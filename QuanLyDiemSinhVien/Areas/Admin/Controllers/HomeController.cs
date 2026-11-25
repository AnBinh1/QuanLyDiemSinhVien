
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Models;
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
            var vm = new AdminDashboardViewModel
            {
                AdminName = User.Identity?.Name ?? "Quản trị viên"
            };

            // ===== SỐ LIỆU THEO MENU =====
            vm.TotalUsers = await _context.Users.CountAsync();
            vm.TotalFaculties = await _context.Faculties.CountAsync();
            vm.TotalMajors = await _context.Majors.CountAsync();
            vm.TotalClasses = await _context.Classes.CountAsync();
            vm.TotalStudents = await _context.Students.CountAsync();
            vm.ActiveStudents = await _context.Students.CountAsync(s => s.IsActive);
            vm.TotalLecturers = await _context.Lecturers.CountAsync();
            vm.ActiveLecturers = await _context.Lecturers.CountAsync(l => l.IsActive);
            vm.TotalCourses = await _context.Courses.CountAsync();
            vm.TotalSections = await _context.CourseSections.CountAsync();
            vm.TotalNews = await _context.News.CountAsync();

            // ===== BIỂU ĐỒ: SINH VIÊN THEO KHOA-VIỆN =====
            vm.StudentsByFaculty = await _context.Students
                .AsNoTracking()
                .Where(s => s.Class != null && s.Class.Major != null && s.Class.Major.Faculty != null)
                .GroupBy(s => s.Class!.Major!.Faculty!.FacultyName)
                .Select(g => new ChartItem { Label = g.Key, Value = g.Count() })
                .ToListAsync();

            // ===== BIỂU ĐỒ: GIỚI TÍNH =====
            vm.StudentsByGender = await _context.Students
                .AsNoTracking()
                .GroupBy(s => s.Gender)
                .Select(g => new ChartItem { Label = g.Key, Value = g.Count() })
                .ToListAsync();

            // ===== VIỆC CẦN XỬ LÝ =====
            var sectionsNoLecturer = await _context.CourseSections.CountAsync(s => s.LecturerId == null);
            var lecturersNoAccount = await _context.Lecturers.CountAsync(l => l.UserId == null || l.UserId == "");
            var studentsNoAccount = await _context.Students.CountAsync(s => s.UserId == null || s.UserId == "");

            if (sectionsNoLecturer > 0)
                vm.Todos.Add(new TodoItem
                {
                    Title = "Lớp học phần chưa có giảng viên",
                    Description = "Cần phân công giảng viên phụ trách.",
                    Count = sectionsNoLecturer,
                    Icon = "fa-user-plus",
                    Color = "danger",
                    Controller = "TeachingAssignment"
                });

            if (lecturersNoAccount > 0)
                vm.Todos.Add(new TodoItem
                {
                    Title = "Giảng viên chưa có tài khoản",
                    Description = "Gán tài khoản để giảng viên đăng nhập được.",
                    Count = lecturersNoAccount,
                    Icon = "fa-chalkboard-teacher",
                    Color = "info",
                    Controller = "Lecturer"
                });

            if (studentsNoAccount > 0)
                vm.Todos.Add(new TodoItem
                {
                    Title = "Sinh viên chưa có tài khoản",
                    Description = "Gán tài khoản để sinh viên xem điểm.",
                    Count = studentsNoAccount,
                    Icon = "fa-user-graduate",
                    Color = "info",
                    Controller = "Student"
                });

            return View(vm);
        }
    }
}
