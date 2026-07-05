using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Repository;

namespace QuanLyDiemSinhVien.Areas.Admin.Controllers
{
	[Authorize(Roles = "Admin,Lecturer")]
	[Area("Admin")]
	public class HomeController : Controller
	{
		private readonly DataContext _context;
		public HomeController(DataContext context)
		{
			_context = context;
		}

		public IActionResult Index()
		{
			ViewBag.TotalStudents = _context.Students.Count();
			ViewBag.TotalLecturers = _context.Lecturers.Count();
			ViewBag.TotalFaculties = _context.Faculties.Count();
			ViewBag.TotalMajors = _context.Majors.Count();

			// 1. BIỂU ĐỒ SỐ LƯỢNG SINH VIÊN THEO KHOA–VIỆN
			// ============================================
			var studentByFaculty = _context.Students
				.Include(s => s.Class)
				.ThenInclude(c => c.Major)
				.ThenInclude(m => m.Faculty)
				.Where(s => s.Class != null &&
							s.Class.Major != null &&
							s.Class.Major.Faculty != null)
				.GroupBy(s => s.Class.Major.Faculty.FacultyName)
				.Select(g => new
				{
					FacultyName = g.Key,
					Count = g.Count()
				})
				.ToList();

			ViewBag.FacultyNames = studentByFaculty.Select(x => x.FacultyName).ToList();
			ViewBag.FacultyCounts = studentByFaculty.Select(x => x.Count).ToList();


			// ===============================
			// 2. BIỂU ĐỒ SINH VIÊN THEO GIỚI TÍNH
			// ===============================
			var genderStatistic = _context.Students
				.GroupBy(s => s.Gender)
				.Select(g => new
				{
					Gender = g.Key,
					Count = g.Count()
				})
				.ToList();

			ViewBag.GenderLabels = genderStatistic.Select(g => g.Gender).ToList(); // Nam/Nữ
			ViewBag.GenderCounts = genderStatistic.Select(g => g.Count).ToList();

			return View();
		}
	}
}

