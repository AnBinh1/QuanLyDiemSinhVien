using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Repository;
using QuanLyDiemSinhVien.Services;
using System.Security.Claims;

namespace QuanLyDiemSinhVien.Areas.Student.Controllers
{
    [Area("Student")]
    [Authorize(Roles = "Student")]   // TODO: đổi nếu tên role của bạn khác
    public class ConductScoreController : Controller
    {
        private readonly DataContext _db;
        private readonly ConductScoreService _service;

        public ConductScoreController(DataContext db, ConductScoreService service)
        {
            _db = db;
            _service = service;
        }

        private async Task<int?> GetStudentIdAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = await _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId);
            return student?.StudentId;
        }

        public async Task<IActionResult> Index()
        {
            var studentId = await GetStudentIdAsync();
            if (studentId == null) return NotFound("Tài khoản chưa được liên kết với sinh viên.");

            var rows = await _service.GetScoreTableAsync(studentId.Value);

            ViewBag.CumulativeScore = rows.LastOrDefault()?.CumulativeScore;
            ViewBag.CumulativeRank = rows.LastOrDefault()?.CumulativeRank;
            return View(rows);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Feedback(int scoreId, string? content)
        {
            var studentId = await GetStudentIdAsync();
            if (studentId == null) return NotFound();

            var item = await _db.ConductScores
                .FirstOrDefaultAsync(c => c.Id == scoreId && c.StudentId == studentId); // chỉ sửa được điểm của chính mình
            if (item == null) return NotFound();

            var newText = string.IsNullOrWhiteSpace(content) ? null : content.Trim();
            if (newText != item.Feedback)
            {
                item.Feedback = newText;
                item.AdminReply = null;      // phản hồi mới thì câu trả lời cũ không còn đúng
                item.RepliedBy = null;
                item.RepliedAt = null;
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = "Đã gửi phản hồi.";
            return RedirectToAction(nameof(Index));
        }
    }
}