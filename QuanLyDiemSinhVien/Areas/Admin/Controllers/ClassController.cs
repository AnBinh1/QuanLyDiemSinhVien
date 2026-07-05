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
	public class ClassController : Controller
	{
		private readonly DataContext _context;

		public ClassController(DataContext context)
		{
			_context = context;
		}

		public async Task<IActionResult> Index()
		{
			var classes = await _context.Classes
				.Include(c => c.Major)
				.ThenInclude(m => m.Faculty)
				.OrderByDescending(c => c.ClassId)
				.ToListAsync();

			return View(classes);
		}

		[HttpGet]
		public async Task<IActionResult> Search(string searchString)
		{
			// Giữ giá trị search để hiển thị lại trên View
			ViewBag.SearchString = searchString;

			// Tạo query
			var query = _context.Classes
				.Include(c => c.Major)
				.ThenInclude(m => m.Faculty)
				.AsQueryable();

			if (!string.IsNullOrEmpty(searchString))
			{
				searchString = searchString.Trim().ToLower();

				query = query.Where(c =>
					(c.ClassName != null && c.ClassName.ToLower().Contains(searchString)) ||

					// tìm theo tên ngành
					(c.Major != null &&
						c.Major.MajorName != null &&
						c.Major.MajorName.ToLower().Contains(searchString)
					) ||

					// tìm theo tên khoa
					(c.Major != null &&
						c.Major.Faculty != null &&
						c.Major.Faculty.FacultyName != null &&
						c.Major.Faculty.FacultyName.ToLower().Contains(searchString)
					)
				);
			}

			// Lấy danh sách
			var classes = await query
				.OrderByDescending(c => c.ClassId)
				.ToListAsync();

			return View("Index", classes);
		}


		[HttpGet]
		public async Task<IActionResult> Create()
		{
			ViewBag.FacultyList = new SelectList(await _context.Faculties.ToListAsync(), "Id", "FacultyName");
			ViewBag.MajorList = new List<SelectListItem>(); // ban đầu rỗng
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(ClassModel Class)
		{
			if (!ModelState.IsValid)
			{
				await LoadDropdownsAsync(Class.MajorId);
				TempData["error"] = "Vui lòng nhập đầy đủ thông tin hợp lệ!";
				return View(Class);
			}
			var existName = await _context.Classes
				.AnyAsync(c => c.ClassName.ToLower().Trim() == Class.ClassName.ToLower().Trim());
			if (existName)
			{
				await LoadDropdownsAsync(Class.MajorId);
				TempData["error"] = $"Tên lớp '{Class.ClassName}' đã tồn tại!";
				return View(Class);
			}

			try
			{
				_context.Classes.Add(Class);
				await _context.SaveChangesAsync();
				TempData["success"] = "Thêm lớp học thành công!";
				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				await LoadDropdownsAsync(Class.MajorId);
				TempData["error"] = $"Lỗi khi thêm lớp học: {ex.Message}";
				return View(Class);
			}
		}
		[HttpGet]
		public async Task<IActionResult> Edit(int id)
		{
			var Class = await _context.Classes
				.Include(c => c.Major)
				.ThenInclude(m => m.Faculty)
				.FirstOrDefaultAsync(c => c.ClassId == id);

			if (Class == null)
			{
				TempData["error"] = "Không tìm thấy lớp học!";
				return RedirectToAction(nameof(Index));
			}

			await LoadDropdownsAsync(Class.MajorId);
			return View(Class);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, ClassModel Class)
		{
			if (id != Class.ClassId)
			{
				TempData["error"] = "ID lớp học không khớp!";
				return RedirectToAction(nameof(Index));
			}

			if (!ModelState.IsValid)
			{
				await LoadDropdownsAsync(Class.MajorId);
				TempData["error"] = "Vui lòng nhập đầy đủ thông tin hợp lệ!";
				return View(Class);
			}

			
			var duplicate = await _context.Classes
				.AnyAsync(c => c.ClassName.ToLower().Trim() == Class.ClassName.ToLower().Trim() && c.ClassId != Class.ClassId);
			if (duplicate)
			{
				await LoadDropdownsAsync(Class.MajorId);
				TempData["error"] = $"Tên lớp '{Class.ClassName}' đã tồn tại!";
				return View(Class);
			}

			try
			{
				_context.Classes.Update(Class);
				await _context.SaveChangesAsync();
				TempData["success"] = "Cập nhật lớp học thành công!";
				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				await LoadDropdownsAsync(Class.MajorId);
				TempData["error"] = $"Lỗi khi cập nhật lớp học: {ex.Message}";
				return View(Class);
			}
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(int id)
		{
			var Class = await _context.Classes.FindAsync(id);
			if (Class == null)
			{
				TempData["error"] = "Không tìm thấy lớp học cần xóa!";
				return RedirectToAction(nameof(Index));
			}

			try
			{
				_context.Classes.Remove(Class);
				await _context.SaveChangesAsync();
				TempData["success"] = "Xóa lớp học thành công!";
			}
			catch (Exception ex)
			{
				TempData["error"] = $"Lỗi khi xóa lớp học: {ex.Message}";
			}

			return RedirectToAction(nameof(Index));
		}
		[HttpGet]
		public async Task<IActionResult> GetMajorsByFaculty(int facultyId)
		{
			var majors = await _context.Majors
				.Where(m => m.FacultyId == facultyId)
				.Select(m => new { m.MajorId, m.MajorName })
				.ToListAsync();

			return Json(majors);
		}
		private async Task LoadDropdownsAsync(int? majorId = null)
		{
			int? facultyId = null;

			if (majorId.HasValue)
			{
				facultyId = await _context.Majors
					.Where(m => m.MajorId == majorId)
					.Select(m => (int?)m.FacultyId)
					.FirstOrDefaultAsync();
			}

			ViewBag.FacultyList = new SelectList(await _context.Faculties.ToListAsync(), "Id", "FacultyName", facultyId);
			ViewBag.MajorList = new SelectList(await _context.Majors
				.Where(m => facultyId == null || m.FacultyId == facultyId)
				.ToListAsync(), "MajorId", "MajorName", majorId);
		}
	}
}
