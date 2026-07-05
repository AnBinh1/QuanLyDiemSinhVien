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
	public class CourseController : Controller
	{
		private readonly DataContext _context;
		public CourseController(DataContext context)
		{
			_context = context;
		}

		//Danh sách học phần
		public async Task<IActionResult> Index()
		{
			var courses = await _context.Courses.ToListAsync();
			return View(courses);
		}

		[HttpGet]
		public async Task<IActionResult> Search(string searchString)
		{
			// Giữ lại giá trị tìm kiếm để hiển thị trên View
			ViewBag.SearchString = searchString;

			// Tạo query ban đầu
			var courseQuery = _context.Courses
					.Include(c => c.PrerequisiteCourse) // Nếu muốn hiển thị học phần tiên quyết
					.AsQueryable();

			// Nếu có nhập từ khóa
			if (!string.IsNullOrEmpty(searchString))
			{
				searchString = searchString.Trim().ToLower();

				courseQuery = courseQuery.Where(c =>
					(c.CourseCode != null && c.CourseCode.ToLower().Contains(searchString)) ||
					(c.CourseName != null && c.CourseName.ToLower().Contains(searchString))
				);
			}

			// Lấy danh sách sau khi lọc
			var courses = await courseQuery
					.OrderByDescending(c => c.CourseId)
					.ToListAsync();

			// Trả về View Index
			return View("Index", courses);
		}


		//Create
		[HttpGet]
		public IActionResult Create()
		{
			LoadDropdowns(); // Load dropdown Loại Học Phần & Học Phần Tiên Quyết
			return View();
		}

		//POST: Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(CourseModel course)
		{
			LoadDropdowns(course.CourseType, course.PrerequisiteCourseId);

			if (!ModelState.IsValid)
				return View(course);

			// Kiểm tra trùng mã và tên học phần
			if (await _context.Courses.AnyAsync(c => c.CourseCode.ToLower().Trim() == course.CourseCode.ToLower().Trim()))
			{
				ModelState.AddModelError("", $"Mã học phần '{course.CourseCode}' đã tồn tại!");
				return View(course);
			}
			if (await _context.Courses.AnyAsync(c => c.CourseName.ToLower().Trim() == course.CourseName.ToLower().Trim()))
			{
				ModelState.AddModelError("", $"Tên học phần '{course.CourseName}' đã tồn tại!");
				return View(course);
			}

			try
			{
				_context.Courses.Add(course);
				await _context.SaveChangesAsync();
				TempData["success"] = "Thêm học phần thành công!";
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				ModelState.AddModelError("", $"Lỗi khi thêm học phần: {ex.Message}");
				return View(course);
			}
		}

		//GET: Edit
		[HttpGet]		
		public async Task<IActionResult> Edit(int id)
		{
			var course = await _context.Courses.FindAsync(id);
			if (course == null)
			{
				TempData["error"] = "Không tìm thấy học phần";
				return RedirectToAction("Index");
			}

			LoadDropdowns(course.CourseType, course.PrerequisiteCourseId);
			return View(course);
		}

		// POST: Edit
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, CourseModel course)
		{
			if (id != course.CourseId)
			{
				TempData["error"] = "Id học phần không khớp";
				return RedirectToAction("Index");
			}

			LoadDropdowns(course.CourseType, course.PrerequisiteCourseId);

			if (!ModelState.IsValid)
				return View(course);

			// Kiểm tra trùng mã và tên (trừ chính nó)
			if (await _context.Courses.AnyAsync(c => c.CourseCode.ToLower().Trim() == course.CourseCode.ToLower().Trim() && c.CourseId != course.CourseId))
			{
				ModelState.AddModelError("", $"Mã học phần '{course.CourseCode}' đã tồn tại!");
				return View(course);
			}
			if (await _context.Courses.AnyAsync(c => c.CourseName.ToLower().Trim() == course.CourseName.ToLower().Trim() && c.CourseId != course.CourseId))
			{
				ModelState.AddModelError("", $"Tên học phần '{course.CourseName}' đã tồn tại!");
				return View(course);
			}

			try
			{
				_context.Courses.Update(course);
				await _context.SaveChangesAsync();
				TempData["success"] = "Cập nhật học phần thành công!";
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				ModelState.AddModelError("", $"Lỗi khi cập nhật học phần: {ex.Message}");
				return View(course);
			}
		}

		// Delete
		public async Task<IActionResult> Delete(int id)
		{
			var course = await _context.Courses.FindAsync(id);
			if (course != null)
			{
				_context.Courses.Remove(course);
				await _context.SaveChangesAsync();
				TempData["success"] = "Học phần đã được xoá!";
			}
			return RedirectToAction("Index");
		}

		// Hàm Load Dropdowns
		private void LoadDropdowns(string selectedType = null, int? selectedPrereqId = null)
		{
			// Loại học phần
			var courseTypes = new List<string> { "Bắt buộc","Tự chọn" };
			ViewBag.CourseTypes = new SelectList(courseTypes, selectedType);

			// Học phần tiên quyết
			var courses = _context.Courses
				.OrderBy(c => c.CourseName)
				.Select(c => new { c.CourseId, c.CourseName })
				.ToList();
			ViewBag.Courses = new SelectList(courses, "CourseId", "CourseName", selectedPrereqId);
		}
	}
}
