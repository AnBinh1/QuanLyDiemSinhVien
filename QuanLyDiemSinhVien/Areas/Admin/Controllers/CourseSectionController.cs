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
	public class CourseSectionController : Controller
	{
		private readonly DataContext _context;
		public CourseSectionController(DataContext context)
		{
			_context = context;
		}

		
		// Index
		public async Task<IActionResult> Index()
		{
			var courseSections = await _context.CourseSections
				.Include(cs => cs.Course)
				.Include(cs => cs.Lecturer)
				.Include(cs => cs.Major)
				.ToListAsync();
			return View(courseSections);
		}

		[HttpGet]
		public async Task<IActionResult> Search(string searchString)
		{
			// Lưu giá trị tìm kiếm cho View
			ViewBag.SearchString = searchString;

			// Tạo query gốc
			var sectionQuery = _context.CourseSections
					.Include(s => s.Course)
					.Include(s => s.Lecturer)
					.Include(s => s.Major)
					.AsQueryable();

			// Nếu có nhập từ khóa thì lọc
			if (!string.IsNullOrEmpty(searchString))
			{
				searchString = searchString.Trim().ToLower();

				sectionQuery = sectionQuery.Where(s =>
					(s.SectionCode != null && s.SectionCode.ToLower().Contains(searchString)) ||
					(s.Semester != null && s.Semester.ToLower().Contains(searchString)) ||
					(s.AcademicYear != null && s.AcademicYear.ToLower().Contains(searchString)) ||
					(s.Room != null && s.Room.ToLower().Contains(searchString)) ||
					(s.Status != null && s.Status.ToLower().Contains(searchString)) ||
					(s.Notes != null && s.Notes.ToLower().Contains(searchString)) ||

					// Tìm theo Học phần
					(s.Course != null &&
						(
							(s.Course.CourseName != null && s.Course.CourseName.ToLower().Contains(searchString)) ||
							(s.Course.CourseCode != null && s.Course.CourseCode.ToLower().Contains(searchString))
						)
					) ||

					// Tìm theo giảng viên phụ trách
					(s.Lecturer != null &&
						(
							s.Lecturer.FullName != null && s.Lecturer.FullName.ToLower().Contains(searchString)
						)
					)
				);
			}

			// Lấy danh sách sau khi lọc
			var sections = await sectionQuery
					.OrderByDescending(s => s.SectionId)
					.ToListAsync();

			// Trả về View Index
			return View("Index", sections);
		}


		// Create GET
		public IActionResult Create()
		{
			LoadLecturersAndCourses();
			return View();
		}

		// Create POST
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(CourseSectionModel model)
		{
			LoadLecturersAndCourses();

			if (await _context.CourseSections
				.AnyAsync(c => c.SectionCode.ToLower().Trim() == model.SectionCode.ToLower().Trim()))
			{
				ModelState.AddModelError("SectionCode", $"Tên lớp học phần '{model.SectionCode}' đã tồn tại!");
			}

			// Nếu Status = Open thì bắt buộc phải nhập các trường lịch học
			if (model.Status == "Open")
			{
				if (!model.StartDate.HasValue)
					ModelState.AddModelError("StartDate", "Vui lòng nhập ngày bắt đầu.");

				if (!model.EndDate.HasValue)
					ModelState.AddModelError("EndDate", "Vui lòng nhập ngày kết thúc.");

				if (model.StartWeek <= 1)
					ModelState.AddModelError("StartWeek", "Vui lòng nhập tuần bắt đầu.");

				if (model.EndWeek <= 52)
					ModelState.AddModelError("EndWeek", "Vui lòng nhập tuần kết thúc.");

				if (model.DayOfWeek < 2 || model.DayOfWeek > 7)
					ModelState.AddModelError("DayOfWeek", "Vui lòng nhập thứ trong tuần (2–7).");

				if (model.StartPeriod <= 1)
					ModelState.AddModelError("StartPeriod", "Vui lòng nhập tiết bắt đầu.");

				if (model.EndPeriod <= 10)
					ModelState.AddModelError("EndPeriod", "Vui lòng nhập tiết kết thúc.");

				if (model.StartPeriod > model.EndPeriod)
					ModelState.AddModelError("", "Tiết bắt đầu không thể lớn hơn tiết kết thúc.");

				if (!model.MajorId.HasValue || model.MajorId <= 0)
					ModelState.AddModelError("MajorId", "Vui lòng chọn ngành học.");
			}

			// Nếu form có lỗi thì return lại view
			if (!ModelState.IsValid)
			{
				LoadLecturersAndCourses();
				return View(model);
			}




			try
			{
				_context.CourseSections.Add(model);
				await _context.SaveChangesAsync();
				TempData["success"] = "Thêm lớp học phần thành công!";
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				ModelState.AddModelError("", $"Lỗi khi thêm lớp học phần: {ex.Message}");
				return View(model);
			}
		}

		// Edit GET
		public async Task<IActionResult> Edit(int id)
		{
			LoadLecturersAndCourses();

			var courseSection = await _context.CourseSections
			.Include(c => c.Major)
			.FirstOrDefaultAsync(c => c.SectionId == id);
			
			if (courseSection == null)
			{
				TempData["error"] = "Không tìm thấy lớp học phần!";
				return RedirectToAction("Index");
			}
			return View(courseSection);
		}

		// Edit POST
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, CourseSectionModel model)
		{
			LoadLecturersAndCourses();

			if (id != model.SectionId)
			{
				TempData["error"] = "Id lớp học phần không khớp!";
				return RedirectToAction("Index");
			}
			// Kiểm tra trùng tên lớp học phần (trừ chính nó)
			if (await _context.CourseSections.AnyAsync(c => c.SectionCode.ToLower().Trim() == model.SectionCode.ToLower().Trim() && c.SectionId != model.SectionId))
			{
				ModelState.AddModelError("", $"Tên lớp học phần '{model.SectionCode}' đã tồn tại, vui lòng nhập tên lớp học phần khác!");
				return View(model);
			}


			try
			{
				_context.CourseSections.Update(model);
				await _context.SaveChangesAsync();
				TempData["success"] = "Cập nhật lớp học phần thành công!";
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				ModelState.AddModelError("", $"Lỗi khi cập nhật lớp học phần: {ex.Message}");
				return View(model);
			}
		}

		// OPEN SECTION (MỞ LỚP HỌC PHẦN)
		public async Task<IActionResult> OpenSection(int id)
		{
			var section = await _context.CourseSections.FindAsync(id);
			if (section == null)
			{
				TempData["error"] = "Không tìm thấy lớp học phần!";
				return RedirectToAction("Index");
			}

			// Chuyển trạng thái sang "Open"
			section.Status = "Open";

			// Cập nhật số lượng sinh viên đã đăng ký
			section.RegisteredStudents = await _context.Enrollments
				.CountAsync(e => e.SectionId == id);

			await _context.SaveChangesAsync();

			TempData["success"] = $"Lớp học phần {section.SectionCode} đã được mở!";
			return RedirectToAction("Index");
		}


		// Delete
		public async Task<IActionResult> Delete(int id)
		{
			var courseSection = await _context.CourseSections.FindAsync(id);
			if (courseSection == null)
			{
				TempData["error"] = "Không tìm thấy lớp học phần!";
				return RedirectToAction("Index");
			}

			//// Kiểm tra lớp đã có sinh viên đăng ký chưa
			//bool hasEnrollment = await _context.Enrollments.AnyAsync(e => e.SectionId == id);
			//if (hasEnrollment)
			//{
			//	TempData["error"] = "Không thể xóa lớp đã có sinh viên đăng ký!";
			//	return RedirectToAction("Index");
			//}

			try
			{
				_context.CourseSections.Remove(courseSection);
				await _context.SaveChangesAsync();
				TempData["success"] = "Xóa lớp học phần thành công!";
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				TempData["error"] = $"Lỗi khi xóa lớp học phần: {ex.Message}";
				return RedirectToAction("Index");
			}
		}
		// Hàm helper load dropdown
		private void LoadLecturersAndCourses()
		{
			var lecturers = _context.Lecturers
				.Select(l => new { l.LecturerId, FullName = l.FullName })
				.ToList();
			ViewBag.Lecturers = new SelectList(lecturers, "LecturerId", "FullName");

			var courses = _context.Courses
				.Select(c => new { c.CourseId, c.CourseName })
				.ToList();
			ViewBag.Courses = new SelectList(courses, "CourseId", "CourseName");

			var majors = _context.Majors
				.Select(m => new { m.MajorId, m.MajorName })
				.ToList();
			ViewBag.Majors = new SelectList(majors, "MajorId", "MajorName");
		}

	}
}
