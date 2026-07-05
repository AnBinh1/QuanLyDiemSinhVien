using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Repository;

namespace QuanLyDiemSinhVien.Areas.Student.Controllers
{
	[Area("Student")]
	[Authorize(Roles = "Student,Admin")]
	public class ProfileController : Controller
	{

		public readonly DataContext _context;
		public ProfileController(DataContext context)
		{
			_context = context;
		}
		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			if (userId == null)
			{
				TempData["error"] = "Bạn chưa đăng nhập.";
				return RedirectToAction("Index", "Home");
			}

			var student = await _context.Students
				.Include(c => c.Class)
				.ThenInclude(m => m.Major)
				.ThenInclude(f => f.Faculty)
				.FirstOrDefaultAsync(s => s.UserId == userId);

			if (student == null)
			{
				TempData["error"] = "Không tìm thấy thông tin sinh viên.";
				return RedirectToAction("Index", "Home");
			}

			return View(student);
		}
	}
}
