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
	public class StudentController : Controller
	{
		public readonly DataContext _context;
		public StudentController(DataContext context)
		{
			_context = context;
		}
		public async Task<IActionResult> Index()
		{
			var students = await _context.Students
				.Include(c => c.Class)
				.ThenInclude(m => m.Major)
				.ThenInclude(f => f.Faculty)
				.OrderByDescending(c => c.StudentId)
				.ToListAsync();

			return View(students);
		}
		[HttpGet]
		public async Task<IActionResult> Search(string searchString)
		{
			if (string.IsNullOrWhiteSpace(searchString))
			{
				TempData["error"] = "Vui lòng nhập mã hoặc tên sinh viên để tìm kiếm.";
				return RedirectToAction("Index");
			}

			searchString = searchString.Trim().ToLower();

			var students = await _context.Students
				.Include(c => c.Class)
					.ThenInclude(m => m.Major)
						.ThenInclude(f => f.Faculty)
				.Where(s => s.StudentCode.ToLower().Contains(searchString)
						 || s.FullName.ToLower().Contains(searchString))
				.OrderByDescending(s => s.StudentId)
				.ToListAsync();

			if (students.Count == 0)
			{
				TempData["error"] = $"Không tìm thấy sinh viên với từ khóa '{searchString}'";
			}
			else
			{
				TempData["success"] = $"Tìm thấy {students.Count} sinh viên với từ khóa '{searchString}'";
			}

			ViewBag.SearchString = searchString;
			return View("Index", students); // trả về cùng view Index
		}


		[HttpGet]
		public async Task<IActionResult> Details(int id)
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


		[HttpGet]
		public async Task<IActionResult> Create()
		{
			ViewBag.FacultyList = new SelectList(await _context.Faculties.ToListAsync(), "Id", "FacultyName");
			ViewBag.MajorList = new SelectList(await _context.Majors.ToListAsync(), "MajorId", "MajorName");
			ViewBag.ClassList = new SelectList(await _context.Classes.ToListAsync(), "ClassId", "ClassName");
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(StudentModel student)
		{
			if (!ModelState.IsValid)
			{
				TempData["error"] = "Vui lòng nhập đầy đủ thông tin hợp lệ!";

				// Load lại dropdown nếu có lỗi nhập liệu
				ViewBag.FacultyList = new SelectList(await _context.Faculties.ToListAsync(), "Id", "FacultyName");
				ViewBag.MajorList = new SelectList(await _context.Majors.ToListAsync(), "MajorId", "MajorName");
				ViewBag.ClassList = new SelectList(await _context.Classes.ToListAsync(), "ClassId", "ClassName");
				return View(student);
			}

			// Kiểm tra trùng mã sinh viên (StudentCode)
			var existStudent = await _context.Students
				.FirstOrDefaultAsync(s => s.StudentCode.ToLower().Trim() == student.StudentCode.ToLower().Trim());

			if (existStudent != null)
			{
				TempData["error"] = $"Mã sinh viên '{student.StudentCode}' đã tồn tại trong cơ sở dữ liệu, vui lòng nhập mã sinh viên khác!";
				ViewBag.FacultyList = new SelectList(await _context.Faculties.ToListAsync(), "Id", "FacultyName");
				ViewBag.MajorList = new SelectList(await _context.Majors.ToListAsync(), "MajorId", "MajorName");
				ViewBag.ClassList = new SelectList(await _context.Classes.ToListAsync(), "ClassId", "ClassName");
				return View(student);
			}

			try
			{
				_context.Students.Add(student);
				await _context.SaveChangesAsync();

				TempData["success"] = "Thêm sinh viên thành công!";
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				TempData["error"] = $"Lỗi khi thêm sinh viên: {ex.Message}";
				ViewBag.FacultyList = new SelectList(await _context.Faculties.ToListAsync(), "Id", "FacultyName");
				ViewBag.MajorList = new SelectList(await _context.Majors.ToListAsync(), "MajorId", "MajorName");
				ViewBag.ClassList = new SelectList(await _context.Classes.ToListAsync(), "ClassId", "ClassName");
				return View(student);
			}
		}

		[HttpGet]
		public async Task<IActionResult> Edit(int id)
		{
			var student = await _context.Students
				.Include(s => s.Class)
				.ThenInclude(m => m.Major)
				.ThenInclude(f => f.Faculty)
				.FirstOrDefaultAsync(s => s.StudentId == id);

			if (student == null)
			{
				TempData["error"] = "Không tìm thấy sinh viên để chỉnh sửa.";
				return RedirectToAction("Index");
			}

			// ✅ Lấy khoa, ngành, lớp tương ứng với sinh viên hiện tại
			int? facultyId = student.Class?.Major?.FacultyId;
			int? majorId = student.Class?.MajorId;
			int? classId = student.ClassId;

			// ✅ Load dropdown có sẵn giá trị đang chọn
			ViewBag.FacultyList = new SelectList(await _context.Faculties.ToListAsync(), "Id", "FacultyName", facultyId);
			ViewBag.MajorList = new SelectList(await _context.Majors.Where(m => m.FacultyId == facultyId).ToListAsync(), "MajorId", "MajorName", majorId);
			ViewBag.ClassList = new SelectList(await _context.Classes.Where(c => c.MajorId == majorId).ToListAsync(), "ClassId", "ClassName", classId);

			return View(student);
		}


		// ✅ Sửa sinh viên (POST)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, StudentModel student)
		{
			if (id != student.StudentId)
			{
				TempData["error"] = "Dữ liệu không hợp lệ!";
				return RedirectToAction("Index");
			}

			if (!ModelState.IsValid)
			{
				await LoadDropdownLists();
				return View(student);
			}

			// Kiểm tra trùng mã (ngoại trừ sinh viên hiện tại)
			var existStudent = await _context.Students
				.FirstOrDefaultAsync(s => s.StudentCode.ToLower().Trim() == student.StudentCode.ToLower().Trim() && s.StudentId != id);

			if (existStudent != null)
			{
				TempData["error"] = $"Mã sinh viên '{student.StudentCode}' đã tồn tại trong cơ sở dữ liệu, vui lòng nhập mã sinh viên khác!";
				await LoadDropdownLists();
				return View(student);
			}

			try
			{
				_context.Update(student);
				await _context.SaveChangesAsync();

				TempData["success"] = "Cập nhật thông tin sinh viên thành công!";
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				TempData["error"] = $"Lỗi khi cập nhật: {ex.Message}";
				await LoadDropdownLists();
				return View(student);
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(int id)
		{
			var student = await _context.Students.FindAsync(id);
			if (student == null)
			{
				TempData["error"] = "Không tìm thấy sinh viên cần xóa!";
				return RedirectToAction(nameof(Index));
			}

			try
			{
				_context.Students.Remove(student);
				await _context.SaveChangesAsync();
				TempData["success"] = "Xóa Sinh viên thành công!";
			}
			catch (Exception ex)
			{
				TempData["error"] = $"Lỗi khi xóa sinh viên: {ex.Message}";
			}

			return RedirectToAction(nameof(Index));
		}

		[HttpGet]
		public async Task<IActionResult> GetMajorsByFaculty(int facultyId)
		{
			var majors = await _context.Majors
				.Where(m => m.FacultyId == facultyId)
				.Select(m => new
				{
					majorId = m.MajorId,
					majorName = m.MajorName
				})
				.ToListAsync();

			return Json(majors);
		}

		[HttpGet]
		public async Task<IActionResult> GetClassesByMajor(int majorId)
		{
			var classes = await _context.Classes
				.Where(c => c.MajorId == majorId)
				.Select(c => new
				{
					classId = c.ClassId,
					className = c.ClassName
				})
				.ToListAsync();

			return Json(classes);
		}

		private async Task LoadDropdownLists()
		{
			ViewBag.FacultyList = new SelectList(await _context.Faculties.ToListAsync(), "Id", "FacultyName");
			ViewBag.MajorList = new SelectList(await _context.Majors.ToListAsync(), "MajorId", "MajorName");
			ViewBag.ClassList = new SelectList(await _context.Classes.ToListAsync(), "ClassId", "ClassName");
		}
	}
}
