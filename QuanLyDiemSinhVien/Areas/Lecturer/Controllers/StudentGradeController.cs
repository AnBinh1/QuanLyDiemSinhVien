using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Models;
using QuanLyDiemSinhVien.Models.ViewModel;
using QuanLyDiemSinhVien.Repository;

namespace QuanLyDiemSinhVien.Areas.Lecturer.Controllers
{
    [Area("Lecturer")]
    [Authorize(Roles = "Lecturer")]
    public class StudentGradeController : Controller
    {
        private readonly DataContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public StudentGradeController(
            DataContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // LẤY GIẢNG VIÊN ĐANG ĐĂNG NHẬP
        // =========================================================
        private async Task<LecturerModel?> GetCurrentLecturerAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return null;

            return await _context.Lecturers
                .FirstOrDefaultAsync(l => l.UserId == user.Id);
        }


        // =========================================================
        // DANH SÁCH LỚP HỌC PHẦN GIẢNG VIÊN PHỤ TRÁCH
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Sections()
        {
            var lecturer = await GetCurrentLecturerAsync();

            if (lecturer == null)
                return Forbid();

            // CHỈ LẤY CÁC LỚP HỌC PHẦN DO GIẢNG VIÊN NÀY DẠY
            var sections = await _context.CourseSections
                .Include(cs => cs.Course)
                .Include(cs => cs.Lecturer)
                .Where(cs => cs.LecturerId == lecturer.LecturerId)
                .Select(cs => new
                {
                    Section = cs,

                    StudentCount = _context.Enrollments
                        .Count(e => e.SectionId == cs.SectionId)
                })
                .ToListAsync();

            ViewBag.SectionStudentCounts = sections
                .ToDictionary(
                    x => x.Section.SectionId,
                    x => x.StudentCount
                );

            return View(
                sections
                    .Select(x => x.Section)
                    .ToList()
            );
        }


        // =========================================================
        // DANH SÁCH ĐIỂM CỦA MỘT LỚP HỌC PHẦN
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index(int? sectionId)
        {
            var lecturer = await GetCurrentLecturerAsync();

            if (lecturer == null)
                return Forbid();

            var query = _context.Grades
                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.Student)

                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.CourseSection)
                        .ThenInclude(cs => cs.Course)

                .Include(g => g.User)

                .AsQueryable();

            CourseSectionModel? courseSection = null;


            // =====================================================
            // NẾU CHỌN MỘT LỚP HỌC PHẦN
            // PHẢI KIỂM TRA LỚP ĐÓ THUỘC GIẢNG VIÊN HIỆN TẠI
            // =====================================================
            if (sectionId.HasValue)
            {
                courseSection = await _context.CourseSections
                    .Include(cs => cs.Course)
                    .FirstOrDefaultAsync(cs =>
                        cs.SectionId == sectionId.Value &&
                        cs.LecturerId == lecturer.LecturerId
                    );

                // Không tồn tại hoặc không thuộc giảng viên
                if (courseSection == null)
                    return NotFound();

                query = query.Where(g =>
                    g.Enrollment != null &&
                    g.Enrollment.SectionId == sectionId.Value
                );
            }
            else
            {
                // Nếu không truyền sectionId
                // CHỈ HIỂN THỊ ĐIỂM CỦA CÁC LỚP DO GIẢNG VIÊN DẠY
                query = query.Where(g =>
                    g.Enrollment != null &&
                    g.Enrollment.CourseSection != null &&
                    g.Enrollment.CourseSection.LecturerId
                        == lecturer.LecturerId
                );
            }


            var grades = await query
                .OrderBy(g => g.Enrollment!.Student!.StudentCode)
                .ToListAsync();

            ViewBag.Section = courseSection;

            return View(grades);
        }



        // =========================================================
        // NHẬP ĐIỂM - GET
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Enter(int sectionId)
        {
            var lecturer = await GetCurrentLecturerAsync();

            if (lecturer == null)
                return Forbid();

            // =====================================================
            // KIỂM TRA LỚP HỌC PHẦN
            // CHỈ CHO PHÉP GIẢNG VIÊN NHẬP LỚP MÌNH DẠY
            // =====================================================
            var section = await _context.CourseSections
                .Include(cs => cs.Course)
                .FirstOrDefaultAsync(cs =>
                    cs.SectionId == sectionId &&
                    cs.LecturerId == lecturer.LecturerId
                );

            if (section == null)
                return NotFound();

            // =====================================================
            // BỔ SUNG: TRUYỀN TRẠNG THÁI KHÓA ĐIỂM SANG VIEW
            // =====================================================
            ViewBag.IsGradeLocked = section.IsGradeLocked;

            // =====================================================
            // LẤY SINH VIÊN ĐÃ ĐĂNG KÝ
            // =====================================================
            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Where(e => e.SectionId == sectionId)
                .OrderBy(e => e.Student.StudentCode)
                .ToListAsync();

            // =====================================================
            // LẤY CÁC BẢNG ĐIỂM ĐÃ CÓ
            // =====================================================
            var enrollmentIds = enrollments
                .Select(e => e.EnrollmentId)
                .ToList();

            var grades = await _context.Grades
                .Where(g => enrollmentIds.Contains(g.EnrollmentId))
                .ToDictionaryAsync(g => g.EnrollmentId);

            // =====================================================
            // TẠO VIEW MODEL
            // =====================================================
            var model = new GradeEntryViewModel
            {
                SectionId = section.SectionId,
                SectionCode = section.SectionCode,
                CourseName = section.Course?.CourseName,
                StudentCount = enrollments.Count,

                Students = enrollments.Select(e =>
                {
                    if (grades.TryGetValue(e.EnrollmentId, out var grade))
                    {
                        return new GradeEntryItemViewModel
                        {
                            EnrollmentId = e.EnrollmentId,
                            StudentCode = e.Student?.StudentCode,
                            FullName = e.Student?.FullName,
                            ContinuousAssessment = grade.ContinuousAssessment,
                            FinalExam = grade.FinalExam,
                            Notes = grade.Notes
                        };
                    }

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


        // =========================================================
        // NHẬP ĐIỂM - POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enter(
            GradeEntryViewModel model)
        {
            var lecturer = await GetCurrentLecturerAsync();

            if (lecturer == null)
                return Forbid();

            // =====================================================
            // KIỂM TRA LỚP HỌC PHẦN VÀ QUYỀN GIẢNG VIÊN
            // =====================================================
            var section = await _context.CourseSections
                .FirstOrDefaultAsync(cs =>
                    cs.SectionId == model.SectionId &&
                    cs.LecturerId == lecturer.LecturerId
                );

            // Lớp không tồn tại hoặc không thuộc giảng viên
            if (section == null)
                return NotFound();

            // =====================================================
            // KIỂM TRA KHÓA ĐIỂM
            // PHẢI KIỂM TRA TRƯỚC KHI THÊM HOẶC SỬA ĐIỂM
            // =====================================================
            if (section.IsGradeLocked)
            {
                TempData["ErrorMessage"] =
                    "Lớp học phần đã khóa điểm. Bạn không thể thêm hoặc chỉnh sửa điểm. Vui lòng liên hệ Admin.";

                return RedirectToAction(
                    nameof(Enter),
                    new { sectionId = model.SectionId }
                );
            }

            // =====================================================
            // LẤY TOÀN BỘ ENROLLMENT CỦA LỚP
            // =====================================================
            var enrollments = await _context.Enrollments
                .Where(e => e.SectionId == model.SectionId)
                .ToListAsync();

            var enrollmentIds = enrollments
                .Select(e => e.EnrollmentId)
                .ToList();

            // =====================================================
            // KIỂM TRA DỮ LIỆU GỬI LÊN
            // =====================================================
            if (model.Students == null)
            {
                TempData["ErrorMessage"] = "Danh sách sinh viên không hợp lệ.";

                return RedirectToAction(
                    nameof(Enter),
                    new { sectionId = model.SectionId }
                );
            }

            foreach (var student in model.Students)
            {
                // Không cho phép gửi EnrollmentId
                // không thuộc lớp học phần
                if (!enrollmentIds.Contains(student.EnrollmentId))
                {
                    ModelState.AddModelError(
                        "",
                        "Dữ liệu sinh viên không hợp lệ."
                    );

                    return View(model);
                }
            }

            // =====================================================
            // LẤY CÁC ĐIỂM ĐÃ TỒN TẠI
            // =====================================================
            var existingGrades = await _context.Grades
                .Where(g => enrollmentIds.Contains(g.EnrollmentId))
                .ToDictionaryAsync(g => g.EnrollmentId);

            // =====================================================
            // GIẢNG VIÊN ĐANG NHẬP ĐIỂM
            // =====================================================
            var user = await _userManager.GetUserAsync(User);

            // =====================================================
            // CẬP NHẬT / THÊM MỚI
            // =====================================================
            foreach (var student in model.Students)
            {
                // ĐÃ CÓ ĐIỂM → CẬP NHẬT
                if (existingGrades.TryGetValue(
                    student.EnrollmentId,
                    out var grade))
                {
                    grade.ContinuousAssessment =
                        student.ContinuousAssessment;

                    grade.FinalExam =
                        student.FinalExam;

                    grade.Notes =
                        student.Notes;

                    grade.UserId = user?.Id;

                    grade.UpdatedAt = DateTime.Now;
                }
                else
                {
                    // CHƯA CÓ ĐIỂM → THÊM MỚI
                    var newGrade = new StudentGradeModel
                    {
                        EnrollmentId = student.EnrollmentId,

                        ContinuousAssessment =
                            student.ContinuousAssessment,

                        FinalExam =
                            student.FinalExam,

                        Notes = student.Notes,

                        UserId = user?.Id,

                        UpdatedAt = DateTime.Now
                    };

                    _context.Grades.Add(newGrade);
                }
            }

            // =====================================================
            // LƯU ĐIỂM
            // =====================================================
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Đã lưu điểm cho toàn bộ sinh viên thành công!";

            return RedirectToAction(
                nameof(Enter),
                new { sectionId = model.SectionId }
            );
        }


        // =========================================================
        // AJAX - LẤY SINH VIÊN THEO LỚP
        // =========================================================
        [HttpGet]
        public async Task<JsonResult> GetStudentsBySection(
            int sectionId)
        {
            var lecturer = await GetCurrentLecturerAsync();

            if (lecturer == null)
            {
                return new JsonResult(
                    new
                    {
                        success = false,
                        message = "Không xác định được giảng viên."
                    });
            }


            // =====================================================
            // KIỂM TRA LỚP CÓ THUỘC GIẢNG VIÊN KHÔNG
            // =====================================================
            var isMySection = await _context.CourseSections
                .AnyAsync(cs =>
                    cs.SectionId == sectionId &&
                    cs.LecturerId == lecturer.LecturerId
                );

            if (!isMySection)
            {
                return new JsonResult(
                    new
                    {
                        success = false,
                        message =
                            "Bạn không có quyền truy cập lớp học phần này."
                    });
            }


            var students = await _context.Enrollments
                .Include(e => e.Student)
                .Where(e =>
                    e.SectionId == sectionId)
                .Select(e => new
                {
                    e.EnrollmentId,

                    Text =
                        e.Student.StudentCode
                        + " - "
                        + e.Student.FullName
                })
                .ToListAsync();


            return new JsonResult(
                new SelectList(
                    students,
                    "EnrollmentId",
                    "Text"
                )
            );
        }


        // =========================================================
        // LOAD DROPDOWN
        // =========================================================
        private async Task LoadDropdownsAsync(
            int? sectionId = null)
        {
            var lecturer = await GetCurrentLecturerAsync();

            if (lecturer == null)
            {
                ViewBag.CourseSections =
                    new SelectList(
                        new List<SelectListItem>());

                ViewBag.Students =
                    new SelectList(
                        new List<SelectListItem>());

                return;
            }


            // =====================================================
            // CHỈ LẤY LỚP HỌC PHẦN DO GIẢNG VIÊN PHỤ TRÁCH
            // =====================================================
            var sections = await _context.CourseSections
                .Where(cs =>
                    cs.Status == "Open" &&
                    cs.LecturerId == lecturer.LecturerId)
                .ToListAsync();


            ViewBag.CourseSections =
                new SelectList(
                    sections,
                    "SectionId",
                    "SectionCode",
                    sectionId
                );


            // =====================================================
            // SINH VIÊN CỦA LỚP
            // =====================================================
            if (sectionId.HasValue)
            {
                // Kiểm tra lớp có thuộc giảng viên không
                var isMySection =
                    await _context.CourseSections
                        .AnyAsync(cs =>
                            cs.SectionId == sectionId.Value &&
                            cs.LecturerId ==
                                lecturer.LecturerId
                        );

                if (isMySection)
                {
                    var students =
                        await _context.Enrollments
                            .Include(e => e.Student)
                            .Where(e =>
                                e.SectionId ==
                                sectionId.Value)
                            .Select(e => new
                            {
                                e.EnrollmentId,

                                Text =
                                    e.Student.StudentCode
                                    + " - "
                                    + e.Student.FullName
                            })
                            .ToListAsync();

                    ViewBag.Students =
                        new SelectList(
                            students,
                            "EnrollmentId",
                            "Text"
                        );
                }
                else
                {
                    ViewBag.Students =
                        new SelectList(
                            new List<SelectListItem>(),
                            "Value",
                            "Text"
                        );
                }
            }
            else
            {
                ViewBag.Students =
                    new SelectList(
                        new List<SelectListItem>(),
                        "Value",
                        "Text"
                    );
            }
        }


        // =========================================================
        // XUẤT EXCEL
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> ExportToExcel(
            int sectionId)
        {
            var lecturer = await GetCurrentLecturerAsync();

            if (lecturer == null)
                return Forbid();


            // =====================================================
            // KIỂM TRA LỚP CÓ THUỘC GIẢNG VIÊN KHÔNG
            // =====================================================
            var courseSection = await _context.CourseSections
                .Include(cs => cs.Course)
                .FirstOrDefaultAsync(cs =>
                    cs.SectionId == sectionId &&
                    cs.LecturerId == lecturer.LecturerId
                );

            if (courseSection == null)
                return NotFound();


            // =====================================================
            // LẤY ĐIỂM CỦA SINH VIÊN TRONG LỚP
            // =====================================================
            var data = await _context.Grades
                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.Student)
                        .ThenInclude(s => s.Class)
                            .ThenInclude(c => c.Major)
                                .ThenInclude(m => m.Faculty)

                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.CourseSection)

                .Where(g =>
                    g.Enrollment != null &&
                    g.Enrollment.SectionId == sectionId)

                .OrderBy(g =>
                    g.Enrollment!.Student!.StudentCode)

                .ToListAsync();


            // =====================================================
            // TẠO FILE EXCEL
            // =====================================================
            using var workbook = new XLWorkbook();


            string sheetName =
                courseSection.SectionCode;

            if (sheetName.Length > 31)
            {
                sheetName =
                    sheetName.Substring(0, 31);
            }


            var worksheet =
                workbook.Worksheets.Add(sheetName);


            // =====================================================
            // THÔNG TIN
            // =====================================================
            worksheet.Cell(1, 1).Value =
                "DANH SÁCH ĐIỂM LỚP HỌC PHẦN";

            worksheet.Range(1, 1, 1, 19).Merge();

            worksheet.Cell(1, 1).Style.Font.Bold = true;

            worksheet.Cell(1, 1).Style.Font.FontSize = 16;

            worksheet.Cell(1, 1)
                .Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;


            worksheet.Cell(2, 1).Value =
                "Lớp học phần:";

            worksheet.Cell(2, 2).Value =
                courseSection.SectionCode;


            worksheet.Cell(3, 1).Value =
                "Học phần:";

            worksheet.Cell(3, 2).Value =
                courseSection.Course?.CourseName;


            worksheet.Cell(4, 1).Value =
                "Học kỳ:";

            worksheet.Cell(4, 2).Value =
                courseSection.Semester;


            worksheet.Cell(5, 1).Value =
                "Năm học:";

            worksheet.Cell(5, 2).Value =
                courseSection.AcademicYear;


            worksheet.Cell(6, 1).Value =
                "Số sinh viên:";

            worksheet.Cell(6, 2).Value =
                data.Count;


            // =====================================================
            // HEADER
            // =====================================================
            int headerRow = 8;


            worksheet.Cell(headerRow, 1).Value =
                "STT";

            worksheet.Cell(headerRow, 2).Value =
                "Mã sinh viên";

            worksheet.Cell(headerRow, 3).Value =
                "Tên sinh viên";

            worksheet.Cell(headerRow, 4).Value =
                "Lớp học phần";

            worksheet.Cell(headerRow, 5).Value =
                "Lớp hành chính";

            worksheet.Cell(headerRow, 6).Value =
                "Ngành học";

            worksheet.Cell(headerRow, 7).Value =
                "Khoa-viện";

            worksheet.Cell(headerRow, 8).Value =
                "Ngày sinh";

            worksheet.Cell(headerRow, 9).Value =
                "Giới tính";

            worksheet.Cell(headerRow, 10).Value =
                "Số CCCD";

            worksheet.Cell(headerRow, 11).Value =
                "Địa chỉ";

            worksheet.Cell(headerRow, 12).Value =
                "Số điện thoại";

            worksheet.Cell(headerRow, 13).Value =
                "Ngày nhập học";

            worksheet.Cell(headerRow, 14).Value =
                "Quốc tịch";

            worksheet.Cell(headerRow, 15).Value =
                "Điểm quá trình";

            worksheet.Cell(headerRow, 16).Value =
                "Điểm cuối kỳ";

            worksheet.Cell(headerRow, 17).Value =
                "Điểm tổng kết";

            worksheet.Cell(headerRow, 18).Value =
                "Điểm chữ";

            worksheet.Cell(headerRow, 19).Value =
                "Ghi chú";


            var headerRange =
                worksheet.Range(
                    headerRow,
                    1,
                    headerRow,
                    19
                );

            headerRange.Style.Font.Bold = true;

            headerRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            headerRange.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            headerRange.Style.Border.OutsideBorder =
                XLBorderStyleValues.Thin;

            headerRange.Style.Border.InsideBorder =
                XLBorderStyleValues.Thin;


            // =====================================================
            // DATA
            // =====================================================
            int row = headerRow + 1;

            int stt = 1;


            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value =
                    stt++;

                worksheet.Cell(row, 2).Value =
                    item.Enrollment?.Student?.StudentCode;

                worksheet.Cell(row, 3).Value =
                    item.Enrollment?.Student?.FullName;

                worksheet.Cell(row, 4).Value =
                    item.Enrollment?.CourseSection?.SectionCode;

                worksheet.Cell(row, 5).Value =
                    item.Enrollment?.Student?.Class?.ClassName;

                worksheet.Cell(row, 6).Value =
                    item.Enrollment?.Student?.Class?.Major?.MajorName;

                worksheet.Cell(row, 7).Value =
                    item.Enrollment?.Student?.Class?.Major?.Faculty?.FacultyName;

                worksheet.Cell(row, 8).Value =
                    item.Enrollment?.Student?.DateOfBirth
                        .ToString("dd/MM/yyyy");

                worksheet.Cell(row, 9).Value =
                    item.Enrollment?.Student?.Gender;

                worksheet.Cell(row, 10).Value =
                    item.Enrollment?.Student?.IdentityNumber;

                worksheet.Cell(row, 11).Value =
                    item.Enrollment?.Student?.Address;

                worksheet.Cell(row, 12).Value =
                    item.Enrollment?.Student?.PhoneNumber;

                worksheet.Cell(row, 13).Value =
                    item.Enrollment?.Student?.EnrollmentDate
                        .ToString("dd/MM/yyyy");

                worksheet.Cell(row, 14).Value =
                    item.Enrollment?.Student?.Nationality;

                worksheet.Cell(row, 15).Value =
                    item.ContinuousAssessment;

                worksheet.Cell(row, 16).Value =
                    item.FinalExam;

                worksheet.Cell(row, 17).Value =
                    item.TotalScore;

                worksheet.Cell(row, 18).Value =
                    item.LetterGrade;

                worksheet.Cell(row, 19).Value =
                    item.Notes;

                row++;
            }


            // =====================================================
            // FORMAT
            // =====================================================
            if (data.Count > 0)
            {
                var dataRange =
                    worksheet.Range(
                        headerRow,
                        1,
                        row - 1,
                        19
                    );

                dataRange.Style.Border.OutsideBorder =
                    XLBorderStyleValues.Thin;

                dataRange.Style.Border.InsideBorder =
                    XLBorderStyleValues.Thin;


                worksheet
                    .Range(
                        headerRow + 1,
                        15,
                        row - 1,
                        17
                    )
                    .Style.NumberFormat.Format =
                    "0.00";
            }


            worksheet.Column(1)
                .Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            worksheet.Column(8)
                .Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            worksheet.Column(9)
                .Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            worksheet.Column(15)
                .Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            worksheet.Column(16)
                .Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            worksheet.Column(17)
                .Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            worksheet.Column(18)
                .Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;


            worksheet.Columns()
                .AdjustToContents();

            worksheet.Column(11).Width = 30;

            worksheet.Column(19).Width = 30;


            // =====================================================
            // DOWNLOAD
            // =====================================================
            using var stream =
                new MemoryStream();

            workbook.SaveAs(stream);


            string fileName =
                $"Diem_{courseSection.SectionCode}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";


            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }
    }
}