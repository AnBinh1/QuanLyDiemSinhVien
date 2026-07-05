using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
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
	[Authorize(Roles = "Admin,Lecturer")]
	public class StudentGradeController : Controller
	{
		private readonly DataContext _context;
		private readonly UserManager<IdentityUser> _userManager;

		public StudentGradeController(DataContext context, UserManager<IdentityUser> userManager)
		{
			_context = context;
			_userManager = userManager;
		}

		// Hiển thị danh sách điểm
		public async Task<IActionResult> Index()
		{
			var grades = await _context.Grades
				.Include(g => g.Enrollment)
					.ThenInclude(e => e.Student)
				.Include(g => g.Enrollment)
					.ThenInclude(e => e.CourseSection)
					.ThenInclude(cs => cs.Course)
				.Include(g => g.User)
				.ToListAsync();

			return View(grades);
		}

		// GET: StudentGrade/Create
		public async Task<IActionResult> Create()
		{
			await LoadDropdownsAsync();
			return View();
		}

		// POST: StudentGrade/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(StudentGradeModel model)
		{
			if (ModelState.IsValid)
			{
				var user = await _userManager.GetUserAsync(User);
				model.UserId = user?.Id;
				model.UpdatedAt = DateTime.Now;

				_context.Grades.Add(model);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}

			await LoadDropdownsAsync(model.EnrollmentId);
			return View(model);
		}

		// GET: StudentGrade/Edit/5
		public async Task<IActionResult> Edit(int id)
		{
			var grade = await _context.Grades
				.Include(g => g.Enrollment)
					.ThenInclude(e => e.Student)
				.FirstOrDefaultAsync(g => g.GradeId == id);

			if (grade == null) return NotFound();

			await LoadDropdownsAsync(grade.Enrollment?.SectionId);
			return View(grade);
		}

		// POST: StudentGrade/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, StudentGradeModel model)
		{
			if (id != model.GradeId) return NotFound();

			if (ModelState.IsValid)
			{
				try
				{
					var user = await _userManager.GetUserAsync(User);
					model.UserId = user?.Id;
					model.UpdatedAt = DateTime.Now;

					_context.Grades.Update(model);
					await _context.SaveChangesAsync();
					return RedirectToAction(nameof(Index));
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!_context.Grades.Any(e => e.GradeId == id)) return NotFound();
					else throw;
				}
			}

			await LoadDropdownsAsync(model.EnrollmentId);
			return View(model);
		}

		// AJAX: Lấy danh sách sinh viên theo lớp học phần
		[HttpGet]
		public async Task<JsonResult> GetStudentsBySection(int sectionId)
		{
			var students = await _context.Enrollments
				.Include(e => e.Student)
				.Where(e => e.SectionId == sectionId)
				.Select(e => new
				{
					e.EnrollmentId,
					Text = e.Student.StudentCode + " - " + e.Student.FullName
				})
				.ToListAsync();

			return Json(new SelectList(students, "EnrollmentId", "Text"));
		}

		// Hàm load dropdown Lớp học phần + Sinh viên
		private async Task LoadDropdownsAsync(int? sectionId = null)
		{
			// Lớp học phần đang mở
			var sections = await _context.CourseSections
				.Where(cs => cs.Status == "Open")
				.ToListAsync();

			ViewBag.CourseSections = new SelectList(sections, "SectionId", "SectionCode", sectionId);

			// Sinh viên thuộc lớp học phần được chọn
			if (sectionId.HasValue)
			{
				var students = await _context.Enrollments
					.Include(e => e.Student)
					.Where(e => e.SectionId == sectionId.Value)
					.Select(e => new
					{
						e.EnrollmentId,
						Text = e.Student.StudentCode + " - " + e.Student.FullName
					})
					.ToListAsync();

				ViewBag.Students = new SelectList(students, "EnrollmentId", "Text");
			}
			else
			{
				ViewBag.Students = new SelectList(new List<SelectListItem>(), "Value", "Text");
			}
		}

		public async Task<IActionResult> ExportToExcel(string status = "all")
		{
			IQueryable<StudentGradeModel> query = _context.Grades
							.Include(g => g.Enrollment)
								.ThenInclude(e => e.Student)
								.ThenInclude(s => s.Class)
								.ThenInclude(c => c.Major)
								.ThenInclude(m => m.Faculty)
							.Include(g => g.Enrollment)
								.ThenInclude(e => e.CourseSection);

			var data = await query.ToListAsync();
			// Lọc theo status
			if (status == "A")
				query = query.Where(g => g.LetterGrade == "A");
			else if (status == "F")
				query = query.Where(g => g.LetterGrade == "F");

			var workbook = new XLWorkbook();
			var worksheet = workbook.Worksheets.Add("Danh sách ");
			// Header
			worksheet.Cell(1, 1).Value = "STT";
			worksheet.Cell(1, 2).Value = "Mã sinh viên";
			worksheet.Cell(1, 3).Value = "Tên sinh viên";
			worksheet.Cell(1, 4).Value = "Lớp học phần";
			worksheet.Cell(1, 5).Value = "Lớp hành chính";
			worksheet.Cell(1, 6).Value = "Ngành học";
			worksheet.Cell(1, 7).Value = "Khoa-viện";
			worksheet.Cell(1, 8).Value = "Ngày sinh";
			worksheet.Cell(1, 9).Value = "Giới tính";
			worksheet.Cell(1, 10).Value = "Số CCCD";
			worksheet.Cell(1, 11).Value = "Địa chỉ";
			worksheet.Cell(1, 12).Value = "Số điện thoại";
			worksheet.Cell(1, 13).Value = "Ngày nhập học";
			worksheet.Cell(1, 14).Value = "Quốc tịch";
			worksheet.Cell(1, 15).Value = "Điểm quá trình";
			worksheet.Cell(1, 16).Value = "Điểm cuối kỳ";
			worksheet.Cell(1, 17).Value = "Điểm tổng kết";
			worksheet.Cell(1, 18).Value = "Điểm chữ";
			worksheet.Cell(1, 19).Value = "Ghi chú";

			// Data
			int row = 2;
			int stt = 1;
			foreach (var item in data)
			{
				worksheet.Cell(row, 1).Value = stt++;
				worksheet.Cell(row, 2).Value = item.Enrollment?.Student?.StudentCode;
				worksheet.Cell(row, 3).Value = item.Enrollment?.Student?.FullName;
				worksheet.Cell(row, 4).Value = item.Enrollment?.CourseSection?.SectionCode;
				worksheet.Cell(row, 5).Value = item.Enrollment?.Student?.Class?.ClassName;
				worksheet.Cell(row, 6).Value = item.Enrollment?.Student?.Class?.Major?.MajorName;
				worksheet.Cell(row, 7).Value = item.Enrollment?.Student?.Class?.Major?.Faculty?.FacultyName;
				worksheet.Cell(row, 8).Value = item.Enrollment?.Student?.DateOfBirth.ToString("dd/MM/yyyy");
				worksheet.Cell(row, 9).Value = item.Enrollment?.Student?.Gender;
				worksheet.Cell(row, 10).Value = item.Enrollment?.Student?.IdentityNumber;
				worksheet.Cell(row, 11).Value = item.Enrollment?.Student?.Address;
				worksheet.Cell(row, 12).Value = item.Enrollment?.Student?.PhoneNumber;
				worksheet.Cell(row, 13).Value = item.Enrollment?.Student?.EnrollmentDate.ToString("dd/MM/yyyy");
				worksheet.Cell(row, 14).Value = item.Enrollment?.Student?.Nationality;
				worksheet.Cell(row, 15).Value = item.ContinuousAssessment;
				worksheet.Cell(row, 16).Value = item.FinalExam;
				worksheet.Cell(row, 17).Value = item.TotalScore;
				worksheet.Cell(row, 18).Value = item.LetterGrade;
				worksheet.Cell(row, 19).Value = item.Notes;
				row++;
			}

			worksheet.Columns().AdjustToContents();

			var stream = new MemoryStream();
			workbook.SaveAs(stream);
			stream.Position = 0;

			string fileName = $"DanhSach_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
			return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
		}

		
	}
}
