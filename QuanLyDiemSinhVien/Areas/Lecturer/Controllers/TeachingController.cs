using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Repository;
using System.Security.Claims;

namespace QuanLyDiemSinhVien.Areas.Lecturer.Controllers
{
    [Area("Lecturer")]
    [Authorize(Roles = "Lecturer")]
    public class TeachingController : Controller
    {
        private readonly DataContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public TeachingController(
            DataContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // ============================================================
        // DANH SÁCH LỚP HỌC PHẦN CỦA GIẢNG VIÊN
        // URL:
        // /Lecturer/Teaching/Classes
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Classes(
            string? semester,
            string? academicYear,
            string? keyword)
        {
            // --------------------------------------------------------
            // 1. Lấy tài khoản Identity đang đăng nhập
            // --------------------------------------------------------

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }


            // --------------------------------------------------------
            // 2. Tìm LecturerId tương ứng với tài khoản
            // --------------------------------------------------------

            var lecturerId = await _context.Lecturers
                .Where(l => l.UserId == user.Id)
                .Select(l => l.LecturerId)
                .FirstOrDefaultAsync();

            if (lecturerId == 0)
            {
                return NotFound(
                    "Không tìm thấy thông tin giảng viên.");
            }


            // --------------------------------------------------------
            // 3. Chỉ lấy lớp học phần của giảng viên hiện tại
            // --------------------------------------------------------

            var query = _context.CourseSections
                .Include(cs => cs.Course)
                .Include(cs => cs.Major)
                .Where(cs => cs.LecturerId == lecturerId);


            // --------------------------------------------------------
            // 4. Lọc theo học kỳ
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(semester))
            {
                query = query.Where(cs =>
                    cs.Semester == semester);
            }


            // --------------------------------------------------------
            // 5. Lọc theo năm học
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(academicYear))
            {
                query = query.Where(cs =>
                    cs.AcademicYear == academicYear);
            }


            // --------------------------------------------------------
            // 6. Tìm kiếm
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(cs =>
                    cs.SectionCode.Contains(keyword) ||
                    (
                        cs.Course != null &&
                        (
                            cs.Course.CourseCode.Contains(keyword) ||
                            cs.Course.CourseName.Contains(keyword)
                        )
                    )
                );
            }


            // --------------------------------------------------------
            // 7. Lấy danh sách lớp học phần
            // --------------------------------------------------------

            var classes = await query
                .OrderByDescending(cs => cs.AcademicYear)
                .ThenBy(cs => cs.Semester)
                .ThenBy(cs => cs.Course!.CourseName)
                .ThenBy(cs => cs.SectionCode)
                .ToListAsync();


            // --------------------------------------------------------
            // 8. TÍNH SỐ SINH VIÊN ĐÃ ĐĂNG KÝ
            // --------------------------------------------------------
            //
            // Đếm trực tiếp từ bảng Enrollments.
            //
            // Không lấy RegisteredStudents từ dữ liệu cũ trong
            // CourseSections để đảm bảo số lượng luôn chính xác.
            // --------------------------------------------------------

            var sectionIds = classes
                .Select(cs => cs.SectionId)
                .ToList();

            var enrollmentCounts = await _context.Enrollments
                .Where(e => sectionIds.Contains(e.SectionId))
                .GroupBy(e => e.SectionId)
                .Select(g => new
                {
                    SectionId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(
                    x => x.SectionId,
                    x => x.Count
                );


            // --------------------------------------------------------
            // 9. Gán số sinh viên đã đăng ký vào từng lớp
            // --------------------------------------------------------

            foreach (var section in classes)
            {
                section.RegisteredStudents =
                    enrollmentCounts.TryGetValue(
                        section.SectionId,
                        out var count)
                        ? count
                        : 0;
            }


            // --------------------------------------------------------
            // 10. Dữ liệu bộ lọc
            // --------------------------------------------------------

            ViewBag.Semester = semester;
            ViewBag.AcademicYear = academicYear;
            ViewBag.Keyword = keyword;


            ViewBag.Semesters = await _context.CourseSections
                .Where(cs =>
                    cs.LecturerId == lecturerId)
                .Select(cs => cs.Semester)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();


            ViewBag.AcademicYears = await _context.CourseSections
                .Where(cs =>
                    cs.LecturerId == lecturerId)
                .Select(cs => cs.AcademicYear)
                .Distinct()
                .OrderByDescending(x => x)
                .ToListAsync();


            // --------------------------------------------------------
            // 11. Trả dữ liệu về View
            // --------------------------------------------------------

            return View(classes);
        }


        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var lecturerId = await _context.Lecturers
                .Where(l => l.UserId == user.Id)
                .Select(l => l.LecturerId)
                .FirstOrDefaultAsync();

            if (lecturerId == 0)
                return NotFound("Không tìm thấy thông tin giảng viên.");

            var section = await _context.CourseSections
                .Include(cs => cs.Course)
                .Include(cs => cs.Lecturer)
                .Include(cs => cs.Major)
                .FirstOrDefaultAsync(cs =>
                    cs.SectionId == id &&
                    cs.LecturerId == lecturerId);

            if (section == null)
                return NotFound("Không tìm thấy lớp học phần hoặc bạn không được phân công lớp này.");

            // Đếm số sinh viên đã đăng ký
            section.RegisteredStudents = await _context.Enrollments
                .CountAsync(e => e.SectionId == section.SectionId);

            return View(section);
        }

        [HttpGet]
        public async Task<IActionResult> Students(int? sectionId, string? keyword)
        {
            // Menu "Danh sách sinh viên" không có mã lớp -> chọn lớp trước
            if (sectionId == null || sectionId <= 0)
            {
                return RedirectToAction(nameof(Classes));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Chỉ xem được lớp do chính giảng viên này phụ trách
            var section = await _context.CourseSections
                .Include(cs => cs.Course)
                .FirstOrDefaultAsync(cs =>
                    cs.SectionId == sectionId.Value &&
                    cs.Lecturer != null &&
                    cs.Lecturer.UserId == userId);

            if (section == null)
            {
                return Forbid();
            }

            // Sinh viên đã đăng ký đúng lớp học phần này
            var query =
                from e in _context.Enrollments
                join s in _context.Students on e.StudentId equals s.StudentId
                where e.SectionId == sectionId.Value
                select s;

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();

                query = query.Where(s =>
                    s.StudentCode.Contains(kw) ||
                    s.FullName.Contains(kw));
            }

            var students = await query
                .Distinct()
                .OrderBy(s => s.StudentCode)
                .ToListAsync();

            ViewBag.SectionId = section.SectionId;
            ViewBag.SectionCode = section.SectionCode;
            ViewBag.CourseName = section.Course?.CourseName;
            ViewBag.Keyword = keyword ?? "";

            return View(students);
        }

        // Hàm xuất file Excel danh sách sinh viên theo lớp học phần
        [HttpGet]
        public async Task<IActionResult> ExportStudents(
            int sectionId,
            string? keyword)
        {
            // Lấy ID tài khoản giảng viên đang đăng nhập
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Kiểm tra lớp học phần có thuộc giảng viên này phụ trách không
            var section = await _context.CourseSections
                .Include(cs => cs.Course)
                .FirstOrDefaultAsync(cs =>
                    cs.SectionId == sectionId &&
                    cs.Lecturer != null &&
                    cs.Lecturer.UserId == userId);

            // Không tìm thấy lớp hoặc giảng viên không phụ trách lớp này
            if (section == null)
            {
                return Forbid();
            }

            // Lấy danh sách sinh viên đã đăng ký lớp học phần
            var query =
                from e in _context.Enrollments
                join s in _context.Students
                    on e.StudentId equals s.StudentId
                where e.SectionId == sectionId
                select s;

            // Lọc theo mã sinh viên hoặc họ tên nếu có từ khóa tìm kiếm
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();

                query = query.Where(s =>
                    s.StudentCode.Contains(kw) ||
                    s.FullName.Contains(kw));
            }

            var students = await query
                .Distinct()
                .OrderBy(s => s.StudentCode)
                .ToListAsync();

            // Tạo file Excel
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Danh sách sinh viên");

            // Tiêu đề
            worksheet.Cell(1, 1).Value =
                "DANH SÁCH SINH VIÊN LỚP HỌC PHẦN";
            worksheet.Range(1, 1, 1, 7).Merge();

            worksheet.Cell(2, 1).Value = "Mã lớp học phần:";
            worksheet.Cell(2, 2).Value = section.SectionCode;

            worksheet.Cell(3, 1).Value = "Tên học phần:";
            worksheet.Cell(3, 2).Value =
                section.Course?.CourseName ?? "";

            worksheet.Cell(4, 1).Value = "Tổng số sinh viên:";
            worksheet.Cell(4, 2).Value = students.Count;

            // Tiêu đề các cột
            worksheet.Cell(6, 1).Value = "STT";
            worksheet.Cell(6, 2).Value = "Mã sinh viên";
            worksheet.Cell(6, 3).Value = "Họ và tên";
            worksheet.Cell(6, 4).Value = "Giới tính";
            worksheet.Cell(6, 5).Value = "Ngày sinh";
            worksheet.Cell(6, 6).Value = "Số điện thoại";
            worksheet.Cell(6, 7).Value = "Email";

            // Ghi dữ liệu sinh viên
            int row = 7;
            int stt = 1;

            foreach (var student in students)
            {
                worksheet.Cell(row, 1).Value = stt++;

                // Đặt kiểu văn bản để giữ nguyên mã sinh viên
                worksheet.Cell(row, 2).Value =
                    student.StudentCode ?? "";

                worksheet.Cell(row, 3).Value =
                    student.FullName ?? "";

                worksheet.Cell(row, 4).Value =
                    student.Gender ?? "";

                // Ngày sinh: giữ đúng kiểu ngày tháng trong Excel
                var dateCell = worksheet.Cell(row, 5);

                if (student.DateOfBirth != default)
                {
                    dateCell.Value = student.DateOfBirth;
                    dateCell.Style.DateFormat.Format = "dd/MM/yyyy";
                }

                worksheet.Cell(row, 6).Value =
                    student.PhoneNumber ?? "";

                worksheet.Cell(row, 7).Value =
                    student.Email ?? "";

                row++;
            }

            // Định dạng tiêu đề chính
            var title = worksheet.Range(1, 1, 1, 7);
            title.Style.Font.Bold = true;
            title.Style.Font.FontSize = 16;
            title.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;
            title.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            // Định dạng thông tin lớp học phần
            worksheet.Range(2, 1, 4, 1).Style.Font.Bold = true;

            // Định dạng hàng tiêu đề cột
            var header = worksheet.Range(6, 1, 6, 7);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.LightBlue;
            header.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;
            header.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            // Kẻ đường viền bảng
            var lastRow = Math.Max(6, row - 1);
            var table = worksheet.Range(6, 1, lastRow, 7);

            table.Style.Border.OutsideBorder =
                XLBorderStyleValues.Thin;
            table.Style.Border.InsideBorder =
                XLBorderStyleValues.Thin;

            // Căn giữa STT, giới tính và ngày sinh
            if (students.Count > 0)
            {
                worksheet.Range(7, 1, lastRow, 1)
                    .Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;

                worksheet.Range(7, 4, lastRow, 5)
                    .Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;
            }

            // Tự động điều chỉnh độ rộng cột
            worksheet.Columns().AdjustToContents();

            // Giới hạn độ rộng để tránh cột quá rộng
            worksheet.Column(3).Width =
                Math.Min(worksheet.Column(3).Width, 35);
            worksheet.Column(7).Width =
                Math.Min(worksheet.Column(7).Width, 35);

            // Cố định hàng tiêu đề khi cuộn
            worksheet.SheetView.FreezeRows(6);

            // Xuất file Excel
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            string fileName =
                $"DanhSachSinhVien_{section.SectionCode}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}