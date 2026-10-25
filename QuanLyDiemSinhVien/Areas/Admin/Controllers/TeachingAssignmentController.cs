using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Models;
using QuanLyDiemSinhVien.Repository;

namespace QuanLyDiemSinhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class TeachingAssignmentController : Controller
    {
        private readonly DataContext _context;

        public TeachingAssignmentController(DataContext context)
        {
            _context = context;
        }


        // =========================================================
        // DANH SÁCH PHÂN CÔNG
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var sections = await _context.CourseSections
                .Include(cs => cs.Course)
                .Include(cs => cs.Lecturer)
                .OrderBy(cs => cs.AcademicYear)
                .ThenBy(cs => cs.Semester)
                .ThenBy(cs => cs.SectionCode)
                .ToListAsync();

            return View(sections);

        }


// =========================================================
// KHÓA ĐIỂM - CHỈ ADMIN ĐƯỢC THỰC HIỆN
// =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockGrades(int id)
        {
            var section = await _context.CourseSections
                .FirstOrDefaultAsync(cs => cs.SectionId == id);

            if (section == null)
            {
                return NotFound();
            }

            if (section.IsGradeLocked)
            {
                TempData["SuccessMessage"] = "Lớp học phần này đã được khóa điểm trước đó.";
                return RedirectToAction(nameof(Index));
            }

            section.IsGradeLocked = true;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Đã khóa điểm lớp học phần {section.SectionCode} thành công.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // MỞ KHÓA ĐIỂM - CHỈ ADMIN ĐƯỢC THỰC HIỆN
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockGrades(int id)
        {
            var section = await _context.CourseSections
                .FirstOrDefaultAsync(cs => cs.SectionId == id);

            if (section == null)
            {
                return NotFound();
            }

            if (!section.IsGradeLocked)
            {
                TempData["SuccessMessage"] = "Lớp học phần này chưa bị khóa điểm.";
                return RedirectToAction(nameof(Index));
            }

            section.IsGradeLocked = false;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Đã mở khóa điểm lớp học phần {section.SectionCode} thành công.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // HIỂN THỊ FORM PHÂN CÔNG
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Assign(int id)
        {
            var section = await _context.CourseSections
                .Include(cs => cs.Course)
                .Include(cs => cs.Lecturer)
                .FirstOrDefaultAsync(cs => cs.SectionId == id);

            if (section == null)
            {
                return NotFound();
            }

            ViewBag.Lecturers = await GetLecturerSelectListAsync(section.LecturerId);

            return View(section);
        }


        // =========================================================
        // LƯU PHÂN CÔNG
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(
            int id,
            int? lecturerId)
        {
            var section = await _context.CourseSections
                .FirstOrDefaultAsync(cs => cs.SectionId == id);

            if (section == null)
            {
                return NotFound();
            }


            // Nếu có chọn giảng viên
            if (lecturerId.HasValue)
            {
                var lecturerExists = await _context.Lecturers
                    .AnyAsync(l =>
                        l.LecturerId == lecturerId.Value &&
                        l.IsActive);

                if (!lecturerExists)
                {
                    ModelState.AddModelError(
                        "lecturerId",
                        "Giảng viên không tồn tại hoặc đã nghỉ công tác."
                    );

                    ViewBag.Lecturers = await GetLecturerSelectListAsync(lecturerId);

                    return View(section);
                }
            }


            // Lưu giảng viên vào lớp học phần
            section.LecturerId = lecturerId;

            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Phân công giảng dạy thành công!";


            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // HỦY PHÂN CÔNG
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var section = await _context.CourseSections
                .FirstOrDefaultAsync(cs => cs.SectionId == id);

            if (section == null)
            {
                return NotFound();
            }


            section.LecturerId = null;

            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Đã hủy phân công giảng dạy!";


            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // HÀM DÙNG CHUNG: lấy danh sách giảng viên cho dropdown
        // Chỉ SELECT đúng 2 cột cần dùng (LecturerId, FullName) để
        // tránh EF phải đọc các cột string khác có thể đang NULL
        // trong DB (Address, PhoneNumber, LecturerCode, Gender...)
        // gây lỗi SqlNullValueException khi ToListAsync().
        // =========================================================
        private async Task<SelectList> GetLecturerSelectListAsync(int? selectedId)
        {
            var lecturers = await _context.Lecturers
                .Where(l => l.IsActive)
                .OrderBy(l => l.FullName)
                .Select(l => new
                {
                    l.LecturerId,
                    l.FullName
                })
                .ToListAsync();

            return new SelectList(
                lecturers,
                "LecturerId",
                "FullName",
                selectedId
            );
        }
    }
}