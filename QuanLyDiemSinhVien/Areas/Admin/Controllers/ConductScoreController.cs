using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Models;
using QuanLyDiemSinhVien.Models.ViewModel;
using QuanLyDiemSinhVien.Repository;

namespace QuanLyDiemSinhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]   // TODO: đổi nếu tên role của bạn khác
    public class ConductScoreController : Controller
    {
        private readonly DataContext _db;
        public ConductScoreController(DataContext db) => _db = db;

        // Tên người đang đăng nhập, lưu vào CreatedBy / UpdatedBy
        private string CurrentUserName =>
            User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "Admin";

        private async Task LoadFiltersAsync(int? classId, int? semesterId)
        {
            var classes = await _db.Classes.OrderBy(c => c.ClassName).AsNoTracking().ToListAsync();
            var semesters = await _db.Semesters
                .OrderByDescending(s => s.StartYear).ThenByDescending(s => s.Term)
                .AsNoTracking().ToListAsync();

            ViewBag.Classes = new SelectList(classes, "ClassId", "ClassName", classId);
            ViewBag.Semesters = new SelectList(semesters, "Id", "DisplayName", semesterId);
        }

        // ===== Danh sách điểm đã nhập =====
        public async Task<IActionResult> Index(int? classId, int? semesterId, bool unanswered = false)
        {
            var query = _db.ConductScores
                .Include(c => c.Student).Include(c => c.Semester).AsQueryable();

            if (classId.HasValue) query = query.Where(c => c.Student!.ClassId == classId);
            if (semesterId.HasValue) query = query.Where(c => c.SemesterId == semesterId);
            if (unanswered)
                query = query.Where(c => c.Feedback != null && c.Feedback != "" && c.AdminReply == null);

            var list = await query
                .OrderByDescending(c => c.Semester!.StartYear).ThenByDescending(c => c.Semester!.Term)
                .ThenBy(c => c.Student!.StudentCode)
                .AsNoTracking().ToListAsync();

            ViewBag.Unanswered = unanswered;
            ViewBag.UnansweredCount = await _db.ConductScores
                .CountAsync(c => c.Feedback != null && c.Feedback != "" && c.AdminReply == null);

            await LoadFiltersAsync(classId, semesterId);
            return View(list);
        }

        // ===== Nhập điểm theo lớp hành chính =====
        public async Task<IActionResult> Entry(int? classId, int? semesterId)
        {
            await LoadFiltersAsync(classId, semesterId);
            var vm = new ConductEntryViewModel { ClassId = classId, SemesterId = semesterId };

            if (classId.HasValue && semesterId.HasValue)
            {
                var cls = await _db.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.ClassId == classId);
                var sem = await _db.Semesters.AsNoTracking().FirstOrDefaultAsync(s => s.Id == semesterId);
                if (cls == null || sem == null) return NotFound();
                vm.ClassName = cls.ClassName ?? "";
                vm.SemesterName = sem.DisplayName;

                var students = await _db.Students
                    .Where(s => s.ClassId == classId)
                    .OrderBy(s => s.StudentCode)
                    .AsNoTracking().ToListAsync();

                var ids = students.Select(s => s.StudentId).ToList();
                var existing = await _db.ConductScores
                    .Where(c => c.SemesterId == semesterId && ids.Contains(c.StudentId))
                    .AsNoTracking().ToDictionaryAsync(c => c.StudentId);

                vm.Rows = students.Select(s =>
                {
                    existing.TryGetValue(s.StudentId, out var sc);
                    return new ConductEntryRowViewModel
                    {
                        StudentId = s.StudentId,
                        StudentCode = s.StudentCode,
                        StudentName = s.FullName,
                        Score = sc?.Score,
                        UpdatedBy = sc?.UpdatedBy,
                        UpdatedAt = sc?.UpdatedAt
                    };
                }).ToList();
            }
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Entry(ConductEntryViewModel vm)
        {
            if (!vm.ClassId.HasValue || !vm.SemesterId.HasValue)
                return RedirectToAction(nameof(Entry));

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Có điểm không hợp lệ (phải từ 0 đến 100). Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(Entry), new { classId = vm.ClassId, semesterId = vm.SemesterId });
            }

            // chỉ chấp nhận sinh viên thật sự thuộc lớp đã chọn (chống sửa dữ liệu form)
            var postedIds = vm.Rows.Select(r => r.StudentId).ToList();
            var validIds = await _db.Students
                .Where(s => s.ClassId == vm.ClassId && postedIds.Contains(s.StudentId))
                .Select(s => s.StudentId).ToListAsync();

            var existing = await _db.ConductScores
                .Where(c => c.SemesterId == vm.SemesterId && validIds.Contains(c.StudentId))
                .ToDictionaryAsync(c => c.StudentId);

            var user = CurrentUserName;
            var now = DateTime.Now;
            int added = 0, updated = 0;

            foreach (var row in vm.Rows)
            {
                if (!validIds.Contains(row.StudentId) || row.Score == null) continue;   // để trống = bỏ qua

                if (existing.TryGetValue(row.StudentId, out var item))
                {
                    if (item.Score != row.Score.Value)
                    {
                        item.Score = row.Score.Value;
                        item.UpdatedBy = user;
                        item.UpdatedAt = now;
                        updated++;
                    }
                }
                else
                {
                    _db.ConductScores.Add(new ConductScoreModel
                    {
                        StudentId = row.StudentId,
                        SemesterId = vm.SemesterId.Value,
                        Score = row.Score.Value,
                        CreatedBy = user,
                        CreatedAt = now,
                        UpdatedBy = user,
                        UpdatedAt = now
                    });
                    added++;
                }
            }
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã lưu: thêm mới {added}, cập nhật {updated} sinh viên (người nhập: {user}).";
            return RedirectToAction(nameof(Entry), new { classId = vm.ClassId, semesterId = vm.SemesterId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int? classId, int? semesterId)
        {
            var item = await _db.ConductScores.FindAsync(id);
            if (item == null) return NotFound();

            _db.ConductScores.Remove(item);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã xóa điểm.";
            return RedirectToAction(nameof(Index), new { classId, semesterId });
        }

        // ===== Trả lời phản hồi của sinh viên =====
        public async Task<IActionResult> Reply(int id, int? classId, int? semesterId)
        {
            var item = await _db.ConductScores
                .Include(c => c.Student).Include(c => c.Semester)
                .AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (item == null) return NotFound();

            if (string.IsNullOrWhiteSpace(item.Feedback))
            {
                TempData["Error"] = "Sinh viên này chưa gửi phản hồi.";
                return RedirectToAction(nameof(Index), new { classId, semesterId });
            }

            return View(new ConductReplyViewModel
            {
                ScoreId = item.Id,
                StudentCode = item.Student!.StudentCode,
                StudentName = item.Student.FullName,
                SemesterName = item.Semester!.DisplayName,
                Score = item.Score,
                Feedback = item.Feedback,
                Reply = item.AdminReply,
                ReturnClassId = classId,
                ReturnSemesterId = semesterId
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(ConductReplyViewModel vm)
        {
            var item = await _db.ConductScores
                .Include(c => c.Student).Include(c => c.Semester)
                .FirstOrDefaultAsync(c => c.Id == vm.ScoreId);
            if (item == null) return NotFound();

            if (!ModelState.IsValid)
            {
                // nạp lại phần hiển thị vì form không gửi lại các trường này
                vm.StudentCode = item.Student!.StudentCode;
                vm.StudentName = item.Student.FullName;
                vm.SemesterName = item.Semester!.DisplayName;
                vm.Score = item.Score;
                vm.Feedback = item.Feedback;
                return View(vm);
            }

            item.AdminReply = vm.Reply!.Trim();
            item.RepliedBy = CurrentUserName;
            item.RepliedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã gửi câu trả lời cho sinh viên.";
            return RedirectToAction(nameof(Index),
                new { classId = vm.ReturnClassId, semesterId = vm.ReturnSemesterId });
        }
    }
}