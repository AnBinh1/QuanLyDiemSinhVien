using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Repository;

namespace QuanLyDiemSinhVien.Controllers
{
	public class StudentController : Controller
	{
		public readonly DataContext _context;
		public StudentController(DataContext context)
		{
			_context = context;
		}
		[HttpGet]
		public async Task<IActionResult> Index(int id)
		{
			var student = await _context.Students
				.Include(c => c.Class)
				.ThenInclude(m => m.Major)
				.ThenInclude(f => f.Faculty)
				.FirstOrDefaultAsync(s => s.StudentId == id);

			if (student == null)
			{
				TempData["error"] = "Không tìm thấy thông tin sinh viên.";
				return RedirectToAction("Index");
			}

			return View(student);
		}
		
	}
}
