using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Models;
using QuanLyDiemSinhVien.Repository;

namespace QuanLyDiemSinhVien.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin,Lecturer")]
	public class EnrollmentController : Controller
	{
		private readonly DataContext _context;

		public EnrollmentController(DataContext context)
		{
			_context = context;
		}
		// HIỂN THỊ TẤT CẢ LỚP HỌC PHẦN MỞ
		public IActionResult Index()
		{
			var openSections = _context.CourseSections
				.Where(c => c.Status == "Open")
				.Include(c => c.Course)
				.Include(c => c.Lecturer)
				.ThenInclude(c => c.Major)
				.ToList();

			return View(openSections);
		}

		//// Hiển thị danh sách học phần
		//public IActionResult Index()
		//{
		//	var sections = _context.CourseSections
		//					.Include(c => c.Course)
		//					.Include(c => c.Lecturer)
		//					.ToList();

		//	return View(sections);
		//}

		//// Hiển thị danh sách sinh viên + danh sách đã đăng ký
		//public IActionResult Manage(int sectionId)
		//{
		//	var section = _context.CourseSections
		//					.Include(s => s.Course)
		//					.FirstOrDefault(s => s.SectionId == sectionId);

		//	if (section == null) return NotFound();

		//	var registered = _context.Enrollments
		//						.Include(e => e.Student)
		//						.Where(e => e.SectionId == sectionId)
		//						.ToList();

		//	var students = _context.Students.ToList();

		//	ViewBag.Section = section;
		//	ViewBag.Registered = registered;

		//	return View(students);
		//}
		//// Thêm 1 sinh viên vào lớp học phần
		//[HttpPost]
		//public IActionResult Add(int studentId, int sectionId)
		//{
		//	// Kiểm tra trùng
		//	bool exists = _context.Enrollments
		//					 .Any(e => e.StudentId == studentId && e.SectionId == sectionId);

		//	if (!exists)
		//	{
		//		var enrollment = new EnrollmentModel
		//		{
		//			StudentId = studentId,
		//			SectionId = sectionId
		//		};

		//		_context.Enrollments.Add(enrollment);

		//		// Tăng số lượng RegisteredStudents
		//		var section = _context.CourseSections.Find(sectionId);
		//		section.RegisteredStudents++;

		//		_context.SaveChanges();
		//	}

		//	return RedirectToAction("Manage", new { sectionId });
		//}
		//// Xóa đăng ký
		//public IActionResult Delete(int enrollmentId, int sectionId)
		//{
		//	var enroll = _context.Enrollments.Find(enrollmentId);

		//	if (enroll != null)
		//	{
		//		_context.Enrollments.Remove(enroll);

		//		var section = _context.CourseSections.Find(sectionId);
		//		section.RegisteredStudents--;

		//		_context.SaveChanges();
		//	}

		//	return RedirectToAction("Manage", new { sectionId });
		//}
	}
}

