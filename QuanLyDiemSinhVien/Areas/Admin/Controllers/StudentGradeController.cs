using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Models;
using QuanLyDiemSinhVien.Models.ViewModel;
using QuanLyDiemSinhVien.Repository;

namespace QuanLyDiemSinhVien.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")]
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
        [HttpGet]
        public async Task<IActionResult> Index(int? sectionId)
        {
            var query = _context.Grades
                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.Student)
                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.CourseSection)
                        .ThenInclude(cs => cs.Course)
                .Include(g => g.User)
                .AsQueryable();

            CourseSectionModel? section = null;

            // Nếu có SectionId thì chỉ lấy điểm của lớp học phần đó
            if (sectionId.HasValue)
            {
                section = await _context.CourseSections
                    .Include(cs => cs.Course)
                    .FirstOrDefaultAsync(cs => cs.SectionId == sectionId.Value);

                if (section == null)
                {
                    return NotFound();
                }

                query = query.Where(g =>
                    g.Enrollment != null &&
                    g.Enrollment.SectionId == sectionId.Value);
            }

            var grades = await query
                .OrderBy(g => g.Enrollment!.Student!.StudentCode)
                .ToListAsync();

            ViewBag.Section = section;

            return View(grades);
        }

        // ================================
        // DANH SÁCH LỚP HỌC PHẦN
        // ================================
        public async Task<IActionResult> Sections()
        {
            var sections = await _context.CourseSections
                .Include(cs => cs.Course)
                .Include(cs => cs.Lecturer)
                .Select(cs => new
                {
                    Section = cs,
                    StudentCount = _context.Enrollments
                        .Count(e => e.SectionId == cs.SectionId)
                })
                .ToListAsync();

            ViewBag.SectionStudentCounts = sections
                .ToDictionary(x => x.Section.SectionId, x => x.StudentCount);

            return View(sections.Select(x => x.Section).ToList());
        }

        // ================================
        // NHẬP ĐIỂM THEO LỚP HỌC PHẦN
        // ================================
        [HttpGet]
        public async Task<IActionResult> Enter(int sectionId)
        {
            // 1. Lấy thông tin lớp học phần
            var section = await _context.CourseSections
                .Include(cs => cs.Course)
                .FirstOrDefaultAsync(cs => cs.SectionId == sectionId);

            if (section == null)
                return NotFound();

            // 2. Lấy toàn bộ sinh viên đã đăng ký lớp học phần
            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Where(e => e.SectionId == sectionId)
                .OrderBy(e => e.Student.StudentCode)
                .ToListAsync();

            // 3. Lấy toàn bộ bảng điểm đã có của lớp học phần
            var enrollmentIds = enrollments
                .Select(e => e.EnrollmentId)
                .ToList();

            var grades = await _context.Grades
                .Where(g => enrollmentIds.Contains(g.EnrollmentId))
                .ToDictionaryAsync(g => g.EnrollmentId);

            // 4. Tạo ViewModel
            var model = new GradeEntryViewModel
            {
                SectionId = section.SectionId,
                SectionCode = section.SectionCode,
                CourseName = section.Course?.CourseName,
                StudentCount = enrollments.Count,

                Students = enrollments.Select(e =>
                {
                    // Nếu sinh viên đã có điểm
                    if (grades.TryGetValue(e.EnrollmentId, out var grade))
                    {
                        return new GradeEntryItemViewModel
                        {
                            EnrollmentId = e.EnrollmentId,
                            StudentCode = e.Student?.StudentCode,
                            FullName = e.Student?.FullName,

                            ContinuousAssessment =
                                grade.ContinuousAssessment,

                            FinalExam =
                                grade.FinalExam,

                            Notes =
                                grade.Notes
                        };
                    }

                    // Nếu chưa có điểm
                    return new GradeEntryItemViewModel
                    {
                        EnrollmentId = e.EnrollmentId,
                        StudentCode = e.Student?.StudentCode,
                        FullName = e.Student?.FullName,

                        ContinuousAssessment = 0,
                        FinalExam = 0,

                        Notes = ""
                    };
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enter(GradeEntryViewModel model)
        {
            // 1. Kiểm tra lớp học phần có tồn tại không
            var section = await _context.CourseSections
                .FirstOrDefaultAsync(cs => cs.SectionId == model.SectionId);

            if (section == null)
                return NotFound();


            // 2. Lấy toàn bộ Enrollment thực tế của lớp học phần
            var enrollments = await _context.Enrollments
                .Where(e => e.SectionId == model.SectionId)
                .ToListAsync();

            var enrollmentIds = enrollments
                .Select(e => e.EnrollmentId)
                .ToList();


            // 3. Kiểm tra dữ liệu gửi lên
            // Không cho phép gửi EnrollmentId không thuộc lớp học phần
            foreach (var student in model.Students)
            {
                if (!enrollmentIds.Contains(student.EnrollmentId))
                {
                    ModelState.AddModelError(
                        "",
                        "Dữ liệu sinh viên không hợp lệ."
                    );

                    return View(model);
                }
            }


            // 4. Lấy những bảng điểm đã tồn tại
            var existingGrades = await _context.Grades
                .Where(g => enrollmentIds.Contains(g.EnrollmentId))
                .ToDictionaryAsync(g => g.EnrollmentId);


            // 5. Người đang nhập điểm
            var user = await _userManager.GetUserAsync(User);


            // 6. Cập nhật hoặc thêm mới điểm
            foreach (var student in model.Students)
            {
                if (existingGrades.TryGetValue(
                        student.EnrollmentId,
                        out var grade))
                {
                    // Đã có điểm → cập nhật
                    grade.ContinuousAssessment =
                        student.ContinuousAssessment;

                    grade.FinalExam =
                        student.FinalExam;

                    grade.Notes =
                        student.Notes;

                    grade.UserId =
                        user?.Id;

                    grade.UpdatedAt =
                        DateTime.Now;
                }
                else
                {
                    // Chưa có điểm → tạo mới
                    var newGrade = new StudentGradeModel
                    {
                        EnrollmentId =
                            student.EnrollmentId,

                        ContinuousAssessment =
                            student.ContinuousAssessment,

                        FinalExam =
                            student.FinalExam,

                        Notes =
                            student.Notes,

                        UserId =
                            user?.Id,

                        UpdatedAt =
                            DateTime.Now
                    };

                    _context.Grades.Add(newGrade);
                }
            }


            // 7. Lưu toàn bộ một lần
            await _context.SaveChangesAsync();


            // 8. Quay lại trang nhập điểm
            TempData["SuccessMessage"] =
                "Đã lưu điểm cho toàn bộ sinh viên thành công!";

            return RedirectToAction(
                nameof(Enter),
                new { sectionId = model.SectionId }
            );
        }

        // GET: StudentGrade/Create
        public async Task<IActionResult> Create()
		{
			await LoadDropdownsAsync();
			return View();
		}

		//// POST: StudentGrade/Create
		//[HttpPost]
		//[ValidateAntiForgeryToken]
		//public async Task<IActionResult> Create(StudentGradeModel model)
		//{
		//	if (ModelState.IsValid)
		//	{
		//		var user = await _userManager.GetUserAsync(User);
		//		model.UserId = user?.Id;
		//		model.UpdatedAt = DateTime.Now;

		//		_context.Grades.Add(model);
		//		await _context.SaveChangesAsync();
		//		return RedirectToAction(nameof(Index));
		//	}

		//	await LoadDropdownsAsync(model.EnrollmentId);
		//	return View(model);
		//}

		//// GET: StudentGrade/Edit/5
		//public async Task<IActionResult> Edit(int id)
		//{
		//	var grade = await _context.Grades
		//		.Include(g => g.Enrollment)
		//			.ThenInclude(e => e.Student)
		//		.FirstOrDefaultAsync(g => g.GradeId == id);

		//	if (grade == null) return NotFound();

		//	await LoadDropdownsAsync(grade.Enrollment?.SectionId);
		//	return View(grade);
		//}

		//// POST: StudentGrade/Edit/5
		//[HttpPost]
		//[ValidateAntiForgeryToken]
		//public async Task<IActionResult> Edit(int id, StudentGradeModel model)
		//{
		//	if (id != model.GradeId) return NotFound();

		//	if (ModelState.IsValid)
		//	{
		//		try
		//		{
		//			var user = await _userManager.GetUserAsync(User);
		//			model.UserId = user?.Id;
		//			model.UpdatedAt = DateTime.Now;

		//			_context.Grades.Update(model);
		//			await _context.SaveChangesAsync();
		//			return RedirectToAction(nameof(Index));
		//		}
		//		catch (DbUpdateConcurrencyException)
		//		{
		//			if (!_context.Grades.Any(e => e.GradeId == id)) return NotFound();
		//			else throw;
		//		}
		//	}

		//	await LoadDropdownsAsync(model.EnrollmentId);
		//	return View(model);
		//}

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
        [HttpGet]
        public async Task<IActionResult> ExportToExcel(int sectionId)
        {
            // Lấy thông tin lớp học phần
            var courseSection = await _context.CourseSections
                .Include(cs => cs.Course)
                .FirstOrDefaultAsync(cs => cs.SectionId == sectionId);

            if (courseSection == null)
            {
                return NotFound();
            }

            // CHỈ lấy điểm của sinh viên thuộc lớp học phần được chọn
            var data = await _context.Grades
                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.Student)
                        .ThenInclude(s => s.Class)
                            .ThenInclude(c => c.Major)
                                .ThenInclude(m => m.Faculty)

                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.CourseSection)

                .Where(g => g.Enrollment != null &&
                            g.Enrollment.SectionId == sectionId)

                .OrderBy(g => g.Enrollment!.Student!.StudentCode)

                .ToListAsync();


            // =========================
            // TẠO FILE EXCEL
            // =========================

            using var workbook = new XLWorkbook();

            string sheetName = courseSection.SectionCode;

            if (sheetName.Length > 31)
            {
                sheetName = sheetName.Substring(0, 31);
            }

            var worksheet = workbook.Worksheets.Add(sheetName);


            // =========================
            // THÔNG TIN LỚP HỌC PHẦN
            // =========================

            worksheet.Cell(1, 1).Value = "DANH SÁCH ĐIỂM LỚP HỌC PHẦN";

            worksheet.Range(1, 1, 1, 19).Merge();

            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(2, 1).Value = "Lớp học phần:";
            worksheet.Cell(2, 2).Value = courseSection.SectionCode;
            worksheet.Cell(3, 1).Value = "Học phần:";
            worksheet.Cell(3, 2).Value = courseSection.Course?.CourseName;
            worksheet.Cell(4, 1).Value = "Học kỳ:";
            worksheet.Cell(4, 2).Value = courseSection.Semester;
            worksheet.Cell(5, 1).Value = "Năm học:";
            worksheet.Cell(5, 2).Value = courseSection.AcademicYear;
            worksheet.Cell(6, 1).Value = "Số sinh viên:";
            worksheet.Cell(6, 2).Value = data.Count;


            // =========================
            // HEADER
            // =========================

            int headerRow = 8;

            worksheet.Cell(headerRow, 1).Value = "STT";
            worksheet.Cell(headerRow, 2).Value = "Mã sinh viên";
            worksheet.Cell(headerRow, 3).Value = "Tên sinh viên";
            worksheet.Cell(headerRow, 4).Value = "Lớp học phần";
            worksheet.Cell(headerRow, 5).Value = "Lớp hành chính";
            worksheet.Cell(headerRow, 6).Value = "Ngành học";
            worksheet.Cell(headerRow, 7).Value = "Khoa-viện";
            worksheet.Cell(headerRow, 8).Value = "Ngày sinh";
            worksheet.Cell(headerRow, 9).Value = "Giới tính";
            worksheet.Cell(headerRow, 10).Value = "Số CCCD";
            worksheet.Cell(headerRow, 11).Value = "Địa chỉ";
            worksheet.Cell(headerRow, 12).Value = "Số điện thoại";
            worksheet.Cell(headerRow, 13).Value = "Ngày nhập học";
            worksheet.Cell(headerRow, 14).Value = "Quốc tịch";
            worksheet.Cell(headerRow, 15).Value = "Điểm quá trình";
            worksheet.Cell(headerRow, 16).Value = "Điểm cuối kỳ";
            worksheet.Cell(headerRow, 17).Value = "Điểm tổng kết";
            worksheet.Cell(headerRow, 18).Value = "Điểm chữ";
            worksheet.Cell(headerRow, 19).Value = "Ghi chú";


            // =========================
            // STYLE HEADER
            // =========================

            var headerRange = worksheet.Range(headerRow, 1, headerRow, 19);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            // =========================
            // DATA
            // =========================

            int row = headerRow + 1;
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


            // =========================
            // FORMAT DATA
            // =========================

            if (data.Count > 0)
            {
                var dataRange = worksheet.Range( headerRow, 1, row - 1,19 );

                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                worksheet.Range( headerRow + 1,15, row - 1,17 ).Style.NumberFormat.Format = "0.00";
            }


            // =========================
            // CĂN GIỮA
            // =========================

            worksheet.Column(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Column(8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Column(9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Column(15).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Column(16).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Column(17).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Column(18).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;


            // =========================
            // ĐIỀU CHỈNH ĐỘ RỘNG
            // =========================

            worksheet.Columns().AdjustToContents();
            worksheet.Column(11).Width = 30;
            worksheet.Column(19).Width = 30;


            // =========================
            // XUẤT FILE
            // =========================

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            string fileName = $"Diem_{courseSection.SectionCode}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }
    }
}
