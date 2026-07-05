using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
		private readonly DataContext _context;
		private readonly UserManager<IdentityUser> _userManager;

		public StudentController(DataContext context, UserManager<IdentityUser> userManager)
		{
			_context = context;
			_userManager = userManager;
		}

		public async Task<IActionResult> Index()
		{
			// Lấy danh sách sinh viên, include Class -> Major -> Faculty và User
			var students = await _context.Students
				.Include(s => s.Class)
				.ThenInclude(c => c.Major)
				.ThenInclude(m => m.Faculty)
				.Include(s => s.User)
				.OrderByDescending(s => s.StudentId)
				.ToListAsync();

			return View(students);
		}
		[HttpGet]
		public async Task<IActionResult> Search(string searchString)
		{
			// Lưu giá trị tìm kiếm để hiển thị lại trong input
			ViewBag.SearchString = searchString;

			// Lấy danh sách sinh viên, include Class -> Major -> Faculty và User
			var studentsQuery = _context.Students
				.Include(s => s.Class)
				.ThenInclude(c => c.Major)
				.ThenInclude(m => m.Faculty)
				.Include(s => s.User)
				.AsQueryable();

			if (!string.IsNullOrEmpty(searchString))
			{
				searchString = searchString.Trim().ToLower();
				studentsQuery = studentsQuery.Where(s =>
					(s.StudentCode != null && s.StudentCode.ToLower().Contains(searchString)) ||
					(s.FullName != null && s.FullName.ToLower().Contains(searchString))
				);
			}

			var students = await studentsQuery
				.OrderByDescending(s => s.StudentId)
				.ToListAsync();

			return View("Index", students); // Trả về View Index với danh sách đã lọc
		}

		[HttpGet]
		public async Task<IActionResult> Details(int id)
		{
			var student = await _context.Students
				.Include(s => s.Class)
				.ThenInclude(c => c.Major)
				.ThenInclude(m => m.Faculty)
				.Include(s => s.User)
				.FirstOrDefaultAsync(s => s.StudentId == id);

			if (student == null)
			{
				TempData["error"] = "Không tìm thấy sinh viên.";
				return RedirectToAction("Index");
			}

			return View(student);
		}

		//Hàm xuất file Excel danh sách sinh viên
		public async Task<IActionResult> ExportToExcel(string status = "all")
		{
			IQueryable<StudentModel> query = _context.Students
											.Include(s => s.Class)
											.ThenInclude(s => s.Major)
											.ThenInclude(s => s.Faculty);

			// 👉 Lọc theo trạng thái
			if (status == "active")
			{
				query = query.Where(s => s.IsActive == true);
			}
			else if (status == "inactive")
			{
				query = query.Where(s => s.IsActive == false);
			}

			var data = await query.ToListAsync();

			var workbook = new XLWorkbook();
			var worksheet = workbook.Worksheets.Add("Danh sách ");

			// Header
			worksheet.Cell(1, 1).Value = "STT";
			worksheet.Cell(1, 2).Value = "Mã sinh viên";
			worksheet.Cell(1, 3).Value = "Tên sinh viên";
			worksheet.Cell(1, 4).Value = "Lớp hành chính";
			worksheet.Cell(1, 5).Value = "Ngành học";
			worksheet.Cell(1, 6).Value = "Khoa-viện";
			worksheet.Cell(1, 7).Value = "Ngày sinh";
			worksheet.Cell(1, 8).Value = "Giới tính";
			worksheet.Cell(1, 9).Value = "Số CCCD";
			worksheet.Cell(1, 10).Value = "Địa chỉ";
			worksheet.Cell(1, 11).Value = "Số điện thoại";
			worksheet.Cell(1, 12).Value = "Ngày nhập học";
			worksheet.Cell(1, 13).Value = "Quốc tịch";
			worksheet.Cell(1, 14).Value = "Trạng thái";

			// Data
			int row = 2;
			int stt = 1;
			foreach (var item in data)
			{
				worksheet.Cell(row, 1).Value = stt++;
				worksheet.Cell(row, 2).Value = item.StudentCode;
				worksheet.Cell(row, 3).Value = item.FullName;
				worksheet.Cell(row, 4).Value = item.Class.ClassName;
				worksheet.Cell(row, 5).Value = item.Class.Major.MajorName;
				worksheet.Cell(row, 6).Value = item.Class.Major.Faculty.FacultyName;
				worksheet.Cell(row, 7).Value = item.DateOfBirth.ToString("dd/MM/yyyy");
				worksheet.Cell(row, 8).Value = item.Gender;
				worksheet.Cell(row, 9).Value = item.IdentityNumber;
				worksheet.Cell(row, 10).Value = item.Address;
				worksheet.Cell(row, 11).Value = item.PhoneNumber;
				worksheet.Cell(row, 12).Value = item.EnrollmentDate.ToString("dd/MM/yyyy");
				worksheet.Cell(row, 13).Value = item.Nationality;
				worksheet.Cell(row, 14).Value = item.IsActive;
				row++;
			}

			worksheet.Columns().AdjustToContents();

			var stream = new MemoryStream();
			workbook.SaveAs(stream);
			stream.Position = 0;

			string fileName = $"DanhSach_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
			return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
		}




		[HttpGet]
		public async Task<IActionResult> Create()
		{
			await LoadDropdownLists();
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(StudentModel student)
		{
			if (!ModelState.IsValid)
			{
				TempData["error"] = "Vui lòng nhập đầy đủ thông tin hợp lệ!";
				await LoadDropdownLists();
				return View(student);
			}

			// Kiểm tra trùng StudentCode
			var existStudent = await _context.Students
				.FirstOrDefaultAsync(s => s.StudentCode.ToLower().Trim() == student.StudentCode.ToLower().Trim());

			if (existStudent != null)
			{
				TempData["error"] = $"Mã sinh viên '{student.StudentCode}' đã tồn tại, yêu cầu nhập mã sinh viên khác!";
				await LoadDropdownLists();
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
				await LoadDropdownLists();
				return View(student);
			}
		}

		[HttpGet]
		public async Task<IActionResult> Edit(int id)
		{
			var student = await _context.Students
				.Include(s => s.Class)
					.ThenInclude(c => c.Major)
						.ThenInclude(m => m.Faculty)
				.FirstOrDefaultAsync(s => s.StudentId == id);

			if (student == null)
			{
				TempData["error"] = "Không tìm thấy sinh viên.";
				return RedirectToAction("Index");
			}

			await LoadDropdownLists(student);
			return View(student);
		}

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
				await LoadDropdownLists(student);
				return View(student);
			}

			var existStudent = await _context.Students
				.FirstOrDefaultAsync(s => s.StudentCode.ToLower().Trim() == student.StudentCode.ToLower().Trim() && s.StudentId != id);

			if (existStudent != null)
			{
				TempData["error"] = $"Mã sinh viên '{student.StudentCode}' đã tồn tại!";
				await LoadDropdownLists(student);
				return View(student);
			}

			try
			{
				_context.Students.Update(student);
				await _context.SaveChangesAsync();

				TempData["success"] = "Cập nhật sinh viên thành công!";
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				TempData["error"] = $"Lỗi khi cập nhật: {ex.Message}";
				await LoadDropdownLists(student);
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
				TempData["error"] = "Không tìm thấy sinh viên!";
				return RedirectToAction("Index");
			}

			try
			{
				_context.Students.Remove(student);
				await _context.SaveChangesAsync();
				TempData["success"] = "Xóa sinh viên thành công!";
			}
			catch (Exception ex)
			{
				TempData["error"] = $"Lỗi khi xóa sinh viên: {ex.Message}";
			}

			return RedirectToAction("Index");
		}

		[HttpGet]
		public async Task<IActionResult> GetMajorsByFaculty(int facultyId)
		{
			var majors = await _context.Majors
				.Where(m => m.FacultyId == facultyId)
				.Select(m => new { majorId = m.MajorId, majorName = m.MajorName })
				.ToListAsync();

			return Json(majors);
		}

		[HttpGet]
		public async Task<IActionResult> GetClassesByMajor(int majorId)
		{
			var classes = await _context.Classes
				.Where(c => c.MajorId == majorId)
				.Select(c => new { classId = c.ClassId, className = c.ClassName })
				.ToListAsync();

			return Json(classes);
		}

		// Hàm load dropdown và set selected
		private async Task LoadDropdownLists(StudentModel? student = null)
		{
			int? facultyId = student?.Class?.Major?.FacultyId;
			int? majorId = student?.Class?.MajorId;
			int? classId = student?.ClassId;
			string? userId = student?.UserId;

			ViewBag.FacultyList = new SelectList(await _context.Faculties.ToListAsync(), "Id", "FacultyName", facultyId);
			ViewBag.MajorList = new SelectList(await _context.Majors.Where(m => m.FacultyId == facultyId).ToListAsync(), "MajorId", "MajorName", majorId);
			ViewBag.ClassList = new SelectList(await _context.Classes.Where(c => c.MajorId == majorId).ToListAsync(), "ClassId", "ClassName", classId);
			ViewBag.Users = new SelectList(await _userManager.Users.ToListAsync(), "Id", "UserName", userId);
		}
	}
}
