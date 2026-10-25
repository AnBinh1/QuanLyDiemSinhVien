using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Repository;

namespace QuanLyDiemSinhVien.Areas.Lecturer.Controllers
{
    [Area("Lecturer")]
    [Authorize(Roles = "Lecturer")]
    public class TimetableController : Controller
    {
        private readonly DataContext _context;
    public TimetableController(DataContext context)
        {
            _context = context;
        }

        // =========================================================
        // THỜI KHÓA BIỂU GIẢNG VIÊN
        // =========================================================

        public async Task<IActionResult> Index(int week = 1)
        {
            // Lấy UserId của tài khoản đang đăng nhập
            var userId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier
            )?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // Tìm giảng viên tương ứng với tài khoản đang đăng nhập
            var lecturer = await _context.Lecturers
                .FirstOrDefaultAsync(l => l.UserId == userId);

            if (lecturer == null)
            {
                TempData["error"] = "Không tìm thấy thông tin giảng viên!";
                return View(new List<QuanLyDiemSinhVien.Models.CourseSectionModel>());
            }

            // =====================================================
            // LẤY CÁC LỚP HỌC PHẦN ĐƯỢC PHÂN CÔNG CHO GIẢNG VIÊN
            // =====================================================

            var sections = await _context.CourseSections
                .Where(cs =>
                    cs.LecturerId == lecturer.LecturerId &&
                    week >= cs.StartWeek &&
                    week <= cs.EndWeek
                )
                .Include(cs => cs.Course)
                .OrderBy(cs => cs.DayOfWeek)
                .ThenBy(cs => cs.StartPeriod)
                .ToListAsync();

            ViewBag.Week = week;
            ViewBag.LecturerName = lecturer.FullName;

            return View(sections);
        }
    }

}
