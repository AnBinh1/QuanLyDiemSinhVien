using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Models;
using QuanLyDiemSinhVien.Repository;

namespace QuanLyDiemSinhVien.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin,Lecturer")]
	public class AttendanceController : Controller
	{
		private readonly DataContext _context;
		public AttendanceController(DataContext context)
		{
			_context = context;
		}
		public async Task<IActionResult> Index(int sectionId)
		{
			var sessions = await _context.AttendanceSessions
				.Include(s => s.CourseSection)
				.Where(s => s.SectionId == sectionId)
				.OrderByDescending(s => s.SessionDate)
				.ToListAsync();

			ViewBag.SectionId = sectionId;

			return View(sessions);
		}

		[HttpGet]
		public IActionResult Create()
		{
			var sections = _context.CourseSections
				.Include(cs => cs.Course)
				.Include(cs => cs.Lecturer)
				.Include(cs => cs.Major)
				.Where(cs => cs.Status == "Open") // chỉ chọn lớp đang mở
				.Select(cs => new { cs.SectionId, Name = cs.SectionCode + " - " + cs.Course.CourseName })
				.ToList();
			ViewBag.Majors = new SelectList(sections, "MajorId", "MajorName");
			ViewBag.Sections = new SelectList(sections, "SectionId", "Name");

			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(AttendanceSessionModel model)
		{
			if (!ModelState.IsValid)
			{
				// Nếu lỗi, load lại select
				var sections = _context.CourseSections
					.Include(cs => cs.Course)
					.Include(cs => cs.Lecturer)
					.Where(cs => cs.Status == "Open")
					.Select(cs => new { cs.SectionId, Name = cs.SectionCode + " - " + cs.Course.CourseName })
					.ToList();
				ViewBag.Sections = new SelectList(sections, "SectionId", "Name");

				return View(model);
			}

			_context.AttendanceSessions.Add(model);
			await _context.SaveChangesAsync();

			TempData["success"] = "Tạo buổi điểm danh thành công!";
			return RedirectToAction("Index", new { sectionId = model.SectionId });
		}



		[HttpGet]
		public async Task<IActionResult> Mark(int sessionId)
		{
			var session = await _context.AttendanceSessions
				.Include(s => s.CourseSection)
					.ThenInclude(cs => cs.Course)
				.FirstOrDefaultAsync(s => s.AttendanceSessionId == sessionId);

			if (session == null)
			{
				TempData["error"] = "Không tìm thấy buổi điểm danh!";
				return RedirectToAction("Index", new { sectionId = 0 });
			}

			// Lấy danh sách sinh viên trong lớp
			var students = await _context.Enrollments
				.Where(e => e.SectionId == session.SectionId)
				.Include(e => e.Student)
				.Select(e => e.Student!)
				.ToListAsync();

			// Lấy danh sách điểm danh hiện có
			var attendances = await _context.Attendances
				.Where(a => a.AttendanceSessionId == sessionId)
				.Include(a => a.Student)
				.ToListAsync();

			// Nếu chưa có điểm danh thì tạo mới cho tất cả sinh viên
			if (!attendances.Any())
			{
				foreach (var student in students)
				{
					_context.Attendances.Add(new AttendanceModel
					{
						AttendanceSessionId = sessionId,
						StudentId = student.StudentId,
						AttendanceStatus = (int)AttendanceStatusEnum.NotMarked
					});
				}
				await _context.SaveChangesAsync();

				attendances = await _context.Attendances
					.Where(a => a.AttendanceSessionId == sessionId)
					.Include(a => a.Student)
					.ToListAsync();
			}

			ViewBag.Session = session;
			return View(attendances);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Mark(List<AttendanceModel> model)
		{
			if (ModelState.IsValid && model.Any())
			{
				foreach (var attendance in model)
				{
					var existing = await _context.Attendances
						.FirstOrDefaultAsync(a => a.AttendanceId == attendance.AttendanceId);

					if (existing != null)
					{
						existing.AttendanceStatus = attendance.AttendanceStatus;
						existing.Note = attendance.Note;
					}
				}

				await _context.SaveChangesAsync();
				TempData["success"] = "Điểm danh thành công!";

				var sessionId = model.First().AttendanceSessionId;
				var sectionId = await _context.AttendanceSessions
					.Where(s => s.AttendanceSessionId == sessionId)
					.Select(s => s.SectionId)
					.FirstOrDefaultAsync();


				return RedirectToAction("Index", new { sectionId });
			}

			TempData["error"] = "Có lỗi xảy ra!";
			return View(model);
		}

		[HttpGet]
		public async Task<IActionResult> Details(int sessionId)
		{
			var attendances = await _context.Attendances
				.Where(a => a.AttendanceSessionId == sessionId)
				.Include(a => a.Student)
				.Include(a => a.AttendanceSession)
					.ThenInclude(s => s.CourseSection)
						.ThenInclude(cs => cs.Course)
				.ToListAsync();

			if (!attendances.Any())
			{
				TempData["error"] = "Chưa có dữ liệu điểm danh!";
				return RedirectToAction("Index", new { sectionId = 0 });
			}

			return View(attendances);
		}


		//Hàm xuất file Excel danh sách điểm danh sinh viên
		[HttpGet]
		public async Task<IActionResult> ExportToExcel(int sectionId, string week, string status = "all")
		{
			IQueryable<AttendanceModel> query = _context.Attendances
				.Include(a => a.AttendanceSession)
				.ThenInclude(c => c.CourseSection)
				.ThenInclude(c => c.Course)
				.Include(s => s.Student)
				.Where(a => a.AttendanceSession.SectionId == sectionId
						 && a.AttendanceSession.Week == week.ToString());

			var data = await query.ToListAsync();

			if (!data.Any())
			{
				TempData["error"] = "Không có dữ liệu điểm danh cho lớp và tuần này!";
				return RedirectToAction("ExportOptions", new { sectionId });
			}

			var workbook = new XLWorkbook();
			var worksheet = workbook.Worksheets.Add($"Điểm danh Lớp {sectionId} - Tuần {week}");

			// Header
			worksheet.Cell(1, 1).Value = "STT";
			worksheet.Cell(1, 2).Value = "Mã sinh viên";
			worksheet.Cell(1, 3).Value = "Tên sinh viên";
			worksheet.Cell(1, 4).Value = "Giới tính";
			worksheet.Cell(1, 5).Value = "Số điện thoại";
			worksheet.Cell(1, 6).Value = "Mã lớp học phần";
			worksheet.Cell(1, 7).Value = "Lớp học phần";
			worksheet.Cell(1, 8).Value = "Ngày học";
			worksheet.Cell(1, 9).Value = "Tuần học";
			worksheet.Cell(1, 10).Value = "Tiết học";
			worksheet.Cell(1, 11).Value = "Có mặt";
			worksheet.Cell(1, 12).Value = "Vắng phép";
			worksheet.Cell(1, 13).Value = "Vắng không phép";
			worksheet.Cell(1, 14).Value = "Muộn";
			worksheet.Cell(1, 15).Value = "Ghi chú";

			int row = 2;
			int stt = 1;

			foreach (var item in data)
			{
				worksheet.Cell(row, 1).Value = stt++;
				worksheet.Cell(row, 2).Value = item.Student.StudentCode;
				worksheet.Cell(row, 3).Value = item.Student.FullName;
				worksheet.Cell(row, 4).Value = item.Student.Gender;
				worksheet.Cell(row, 5).Value = item.Student.PhoneNumber;
				worksheet.Cell(row, 6).Value = item.AttendanceSession.CourseSection.Course.CourseCode;
				worksheet.Cell(row, 7).Value = item.AttendanceSession.CourseSection.SectionCode;
				worksheet.Cell(row, 8).Value = item.AttendanceSession.SessionDate.ToString("dd/MM/yyyy");
				worksheet.Cell(row, 9).Value = item.AttendanceSession.Week;
				worksheet.Cell(row, 10).Value =$"{item.AttendanceSession.CourseSection.StartPeriod} - {item.AttendanceSession.CourseSection.EndPeriod}";
				worksheet.Cell(row, 11).Value = item.AttendanceStatus == 1 ? "X" : "";
				worksheet.Cell(row, 12).Value = item.AttendanceStatus == 2 ? "X" : "";
				worksheet.Cell(row, 13).Value = item.AttendanceStatus == 3 ? "X" : "";
				worksheet.Cell(row, 14).Value = item.AttendanceStatus == 4 ? "X" : "";
				worksheet.Cell(row, 15).Value = item.Note;

				row++;
			}

			worksheet.Columns().AdjustToContents();

			using var stream = new MemoryStream();
			workbook.SaveAs(stream);
			var content = stream.ToArray();

			string fileName = $"DiemDanh_Lop{sectionId}_Tuan{week}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
			return File(content,
				"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				fileName);

		}

		[HttpGet]
		public async Task<IActionResult> ExportOptions(AttendanceSessionModel model)
		{
			if (!ModelState.IsValid)
			{
				// Nếu lỗi, load lại select
				var sections = _context.CourseSections
					.Include(cs => cs.Course)
					.Include(cs => cs.Lecturer)
					.Where(cs => cs.Status == "Open")
					.Select(cs => new { cs.SectionId, Name = cs.SectionCode + " - " + cs.Course.CourseName })
					.ToList();
				ViewBag.Sections = new SelectList(sections, "SectionId", "Name");
				
				return View(model);
			}

			return View();
		}


	}

}