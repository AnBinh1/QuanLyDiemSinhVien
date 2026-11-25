using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Models.ViewModel;
using QuanLyDiemSinhVien.Repository;
using QuanLyDiemSinhVien.Services;
using System.Security.Claims;

namespace QuanLyDiemSinhVien.Areas.Student.Controllers
{
	[Area("Student")]
	[Authorize(Roles = "Student")]
	public class GradesController : Controller
	{
		private readonly DataContext _context;
		public GradesController(DataContext context)
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

			// Lấy sinh viên theo tài khoản đăng nhập
			var student = await _context.Students
					.Include(s => s.Class)
						.ThenInclude(c => c.Major)
							.ThenInclude(f => f.Faculty)
				.FirstOrDefaultAsync(s => s.UserId == userId);

			if (student == null)
			{
				TempData["error"] = "Không tìm thấy thông tin sinh viên.";
				return RedirectToAction("Index", "Home");
			}

			// Lấy danh sách điểm của sinh viên này
			var grades = await _context.Grades
				.Include(g => g.Enrollment)
					.ThenInclude(e => e.CourseSection)
					.ThenInclude(c => c.Course)
				.Where(g => g.Enrollment.StudentId == student.StudentId)
				.ToListAsync();

			// Gửi cả student info và grades sang View
			var model = new StudentGradesViewModel
			{
				Student = student,
				Grades = grades
			};

			return View(model);
		}
        public async Task<IActionResult> Summary()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return NotFound();

            var grades = await _context.Grades
                .Include(g => g.Enrollment)
                    .ThenInclude(e => e!.CourseSection)
                        .ThenInclude(cs => cs!.Course)
                .Where(g => g.Enrollment!.StudentId == student.StudentId)
                .ToListAsync();

            var records = grades
                .Where(g => g.Enrollment?.CourseSection?.Course != null)
                .Select(g => new GradeRecord
                {
                    CourseCode = g.Enrollment!.CourseSection!.Course!.CourseCode,
                    Credits = g.Enrollment.CourseSection.Course.Credits,
                    Semester = g.Enrollment.CourseSection.Semester,
                    AcademicYear = g.Enrollment.CourseSection.AcademicYear,
                    TotalScore = (double)g.TotalScore,
                    IncludeInGraduation = g.IsGraduationEligible
                })
                .ToList();

            var vm = new TranscriptSummaryViewModel
            {
                StudentName = student.FullName,
                StudentCode = student.StudentCode,
                Rows = GradeCalculator.BuildSummary(records)
            };

            return View(vm);
        }
    }
}
