using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Models;
using QuanLyDiemSinhVien.Repository;

namespace QuanLyDiemSinhVien.Areas.Student.Controllers
{
	[Area("Student")]
	[Authorize(Roles = "Student")]
	public class EnrollmentController : Controller
	{
		private readonly DataContext _context;

		public EnrollmentController(DataContext context)
		{
			_context = context;
		}
		// Xem lớp học phần đang mở
		public async Task<IActionResult> Index()
		{
			var openSections = await _context.CourseSections
				.Where(c => c.Status == "Open")
				.Include(c => c.Course)
				.Include(c => c.Lecturer)
				.ToListAsync();

			return View(openSections);
		}

		// Đăng ký lớp học phần
		[HttpPost]
		public async Task<IActionResult> Enroll(int sectionId)
		{
			var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
			if (student == null)
			{
				TempData["error"] = "Không tìm thấy sinh viên!";
				return RedirectToAction("Index");
			}

			// Kiểm tra đã đăng ký chưa
			bool alreadyRegistered = await _context.Enrollments
				.AnyAsync(e => e.StudentId == student.StudentId && e.SectionId == sectionId);

			if (alreadyRegistered)
			{
				TempData["error"] = "Bạn đã đăng ký lớp học phần này!";
				return RedirectToAction("Index");
			}

			var section = await _context.CourseSections.FindAsync(sectionId);
			if (section == null || section.RegisteredStudents >= section.MaxStudents)
			{
				TempData["error"] = "Lớp học phần không tồn tại hoặc đã đầy!";
				return RedirectToAction("Index");
			}

			_context.Enrollments.Add(new EnrollmentModel
			{
				StudentId = student.StudentId,
				SectionId = sectionId
			});

			section.RegisteredStudents++;

			await _context.SaveChangesAsync();

			TempData["success"] = "Đăng ký lớp học phần thành công!";
			return RedirectToAction("Index");
		}
		//hàm huỷ đăng ký
		[HttpPost]
		public async Task<IActionResult> UnEnroll(int sectionId)
		{
			var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);

			if (student == null)
			{
				TempData["error"] = "Không tìm thấy sinh viên!";
				return RedirectToAction("MyCourse");
			}

			// Tìm bản ghi đã đăng ký
			var enrollment = await _context.Enrollments
				.FirstOrDefaultAsync(e => e.StudentId == student.StudentId && e.SectionId == sectionId);

			if (enrollment == null)
			{
				TempData["error"] = "Bạn chưa đăng ký lớp này!";
				return RedirectToAction("MyCourse");
			}

			var section = await _context.CourseSections.FindAsync(sectionId);
			if (section != null && section.RegisteredStudents > 0)
			{
				section.RegisteredStudents--; // Giảm số lượng đăng ký
			}

			// Xoá đăng ký
			_context.Enrollments.Remove(enrollment);
			await _context.SaveChangesAsync();

			TempData["success"] = "Huỷ đăng ký lớp học phần thành công!";
			return RedirectToAction("MyCourse");
		}

		// Xem danh sách lớp đã đăng ký
		public async Task<IActionResult> MyCourse()
		{
			var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
			if (student == null) return RedirectToAction("Index");

			var myEnrollments = await _context.Enrollments
				.Where(e => e.StudentId == student.StudentId)
				.Include(e => e.CourseSection)
				.ThenInclude(cs => cs.Course)
				.Include(e => e.CourseSection)
				.ThenInclude(cs => cs.Lecturer)
				.ToListAsync();

			return View(myEnrollments);
		}

		// Xem thời khóa biểu theo tuần
		public async Task<IActionResult> Timetable(int week = 1)
		{
			var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);

			if (student == null)
				return RedirectToAction("Index");

			var enrollments = await _context.Enrollments
				.Where(e => e.StudentId == student.StudentId)
				.Include(e => e.CourseSection)
					.ThenInclude(cs => cs.Course)
				.Include(e => e.CourseSection)
					.ThenInclude(cs => cs.Lecturer)
				.ToListAsync();

			// Lọc theo tuần
			enrollments = enrollments
				.Where(e => week >= e.CourseSection.StartWeek
						 && week <= e.CourseSection.EndWeek)
				.ToList();

			ViewBag.Week = week;

			return View(enrollments);
		}

	}
}

