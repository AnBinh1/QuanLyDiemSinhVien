using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Models;
using QuanLyDiemSinhVien.Repository;

namespace QuanLyDiemSinhVien.Areas.Lecturer.Controllers
{
    [Area("Lecturer")]
    [Authorize(Roles = "Lecturer")]
    public class AttendanceController : Controller
    {
        private readonly DataContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        // Map trạng thái (query string) -> mã trạng thái trong DB
        private static readonly Dictionary<string, int> StatusMap = new()
        {
            ["notmarked"] = 0,
            ["present"] = 1,
            ["excused"] = 2,
            ["unexcused"] = 3,
            ["late"] = 4
        };

        public AttendanceController(
            DataContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // 1. HÀM DÙNG CHUNG
        // =========================================================

        private string? GetCurrentUserId()
        {
            return _userManager.GetUserId(User);
        }

        private async Task<bool> IsSectionOwnedByCurrentLecturerAsync(
            int sectionId)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
                return false;

            return await _context.CourseSections.AnyAsync(cs =>
                cs.SectionId == sectionId &&
                cs.Lecturer != null &&
                cs.Lecturer.UserId == userId);
        }

        private async Task LoadSectionsAsync(int? selectedSectionId = null)
        {
            var userId = GetCurrentUserId();

            var sections = await _context.CourseSections
                .Where(cs =>
                    cs.Status == "Open" &&
                    cs.Lecturer != null &&
                    cs.Lecturer.UserId == userId)
                .Select(cs => new
                {
                    cs.SectionId,
                    Name = cs.SectionCode + " - " + cs.Course!.CourseName
                })
                .ToListAsync();

            ViewBag.Sections = new SelectList(
                sections,
                "SectionId",
                "Name",
                selectedSectionId);
        }

        // =========================================================
        // 2. DANH SÁCH BUỔI ĐIỂM DANH
        // URL: /Lecturer/Attendance/Index?sectionId=1
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index(int? sectionId, int? id)
        {
            // Link ở trang lớp học phần có thể truyền sectionId hoặc id
            sectionId ??= id;

            if (sectionId == null || sectionId <= 0)
            {
                return RedirectToAction("Classes", "Teaching",
                    new { area = "Lecturer" });
            }

            if (!await IsSectionOwnedByCurrentLecturerAsync(sectionId.Value))
            {
                return Forbid();
            }

            var sessions = await _context.AttendanceSessions
                .Include(s => s.CourseSection)
                .Where(s => s.SectionId == sectionId.Value)
                .OrderByDescending(s => s.SessionDate)
                .ThenByDescending(s => s.AttendanceSessionId)
                .ToListAsync();

            ViewBag.SectionId = sectionId.Value;

            return View(sessions);
        }

        // =========================================================
        // 3. FORM TẠO BUỔI ĐIỂM DANH
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create(int? sectionId)
        {
            await LoadSectionsAsync(sectionId);

            var model = new AttendanceSessionModel();

            if (sectionId.HasValue)
            {
                if (!await IsSectionOwnedByCurrentLecturerAsync(
                    sectionId.Value))
                {
                    return Forbid();
                }

                model.SectionId = sectionId.Value;
            }

            return View(model);
        }

        // =========================================================
        // 4. XỬ LÝ TẠO BUỔI ĐIỂM DANH
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AttendanceSessionModel model)
        {
            // Navigation property không đến từ form nên bỏ qua validate
            ModelState.Remove("CourseSection");

            // Chống overposting: luôn để EF tự sinh khóa chính
            model.AttendanceSessionId = 0;

            // Chuẩn hóa tuần để khi xuất Excel luôn khớp
            model.Week = model.Week?.Trim() ?? "";

            var userId = GetCurrentUserId();

            var validSection = await _context.CourseSections
                .AnyAsync(cs =>
                    cs.SectionId == model.SectionId &&
                    cs.Status == "Open" &&
                    cs.Lecturer != null &&
                    cs.Lecturer.UserId == userId);

            if (!validSection)
            {
                ModelState.AddModelError(
                    nameof(model.SectionId),
                    "Lớp học phần không hợp lệ hoặc không thuộc quyền phụ trách.");
            }

            if (!ModelState.IsValid)
            {
                await LoadSectionsAsync(model.SectionId);
                return View(model);
            }

            _context.AttendanceSessions.Add(model);
            await _context.SaveChangesAsync();

            TempData["success"] = "Tạo buổi điểm danh thành công!";

            return RedirectToAction(
                nameof(Index),
                new { sectionId = model.SectionId });
        }

        // =========================================================
        // 5. MỞ TRANG ĐIỂM DANH SINH VIÊN
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Mark(int sessionId)
        {
            var session = await _context.AttendanceSessions
                .Include(s => s.CourseSection)
                    .ThenInclude(cs => cs!.Course)
                .FirstOrDefaultAsync(s =>
                    s.AttendanceSessionId == sessionId);

            if (session == null)
            {
                TempData["error"] = "Không tìm thấy buổi điểm danh!";
                return RedirectToAction(nameof(Index));
            }

            if (!await IsSectionOwnedByCurrentLecturerAsync(session.SectionId))
            {
                return Forbid();
            }

            // Sinh viên đã đăng ký lớp học phần
            var studentIds = await _context.Enrollments
                .Where(e => e.SectionId == session.SectionId)
                .Select(e => e.StudentId)
                .Distinct()
                .ToListAsync();

            // Bản ghi điểm danh đã tồn tại
            var existingStudentIds = await _context.Attendances
                .Where(a => a.AttendanceSessionId == sessionId)
                .Select(a => a.StudentId)
                .ToListAsync();

            // Bổ sung bản ghi còn thiếu
            var missingStudentIds = studentIds
                .Except(existingStudentIds)
                .ToList();

            foreach (var studentId in missingStudentIds)
            {
                _context.Attendances.Add(new AttendanceModel
                {
                    AttendanceSessionId = sessionId,
                    StudentId = studentId,
                    AttendanceStatus = (int)AttendanceStatusEnum.NotMarked
                });
            }

            if (missingStudentIds.Count > 0)
            {
                await _context.SaveChangesAsync();
            }

            var attendances = await _context.Attendances
                .Where(a =>
                    a.AttendanceSessionId == sessionId &&
                    studentIds.Contains(a.StudentId))
                .Include(a => a.Student)
                .OrderBy(a => a.Student!.StudentCode)
                .ToListAsync();

            ViewBag.Session = session;

            return View(attendances);
        }

        // =========================================================
        // 6. LƯU KẾT QUẢ ĐIỂM DANH
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Mark(List<AttendanceModel> model)
        {
            if (model == null || model.Count == 0)
            {
                TempData["error"] = "Không có dữ liệu điểm danh để lưu!";
                return RedirectToAction(nameof(Index));
            }

            var sessionIds = model
                .Select(a => a.AttendanceSessionId)
                .Distinct()
                .ToList();

            if (sessionIds.Count != 1)
            {
                return BadRequest("Dữ liệu chứa nhiều buổi điểm danh.");
            }

            var sessionId = sessionIds[0];

            var session = await _context.AttendanceSessions
                .FirstOrDefaultAsync(s =>
                    s.AttendanceSessionId == sessionId);

            if (session == null)
            {
                return NotFound("Không tìm thấy buổi điểm danh.");
            }

            if (!await IsSectionOwnedByCurrentLecturerAsync(session.SectionId))
            {
                return Forbid();
            }

            // Trạng thái hợp lệ: 0..4, và không trùng AttendanceId
            if (model.Any(a => a.AttendanceStatus < 0 || a.AttendanceStatus > 4) ||
                model.Select(a => a.AttendanceId).Distinct().Count() != model.Count)
            {
                TempData["error"] = "Dữ liệu điểm danh không hợp lệ!";
                return RedirectToAction(nameof(Mark), new { sessionId });
            }

            var submittedIds = model.Select(a => a.AttendanceId).ToList();

            // Chỉ lấy các bản ghi thuộc ĐÚNG buổi này và có trong dữ liệu gửi lên.
            // Nếu có ID giả mạo / thuộc buổi khác thì số lượng sẽ không khớp.
            var existingRecords = await _context.Attendances
                .Where(a =>
                    a.AttendanceSessionId == sessionId &&
                    submittedIds.Contains(a.AttendanceId))
                .ToListAsync();

            if (existingRecords.Count != model.Count)
            {
                TempData["error"] =
                    "Danh sách điểm danh đã thay đổi. " +
                    "Vui lòng tải lại trang và thử lại.";

                return RedirectToAction(nameof(Mark), new { sessionId });
            }

            var submittedRecords = model.ToDictionary(a => a.AttendanceId);

            foreach (var existing in existingRecords)
            {
                var submitted = submittedRecords[existing.AttendanceId];

                existing.AttendanceStatus = submitted.AttendanceStatus;
                existing.Note = submitted.Note?.Trim();
            }

            await _context.SaveChangesAsync();

            TempData["success"] = "Lưu điểm danh thành công!";

            return RedirectToAction(
                nameof(Index),
                new { sectionId = session.SectionId });
        }

        // =========================================================
        // 7. XEM CHI TIẾT ĐIỂM DANH
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int sessionId)
        {
            var session = await _context.AttendanceSessions
                .Include(s => s.CourseSection)
                    .ThenInclude(cs => cs!.Course)
                .FirstOrDefaultAsync(s =>
                    s.AttendanceSessionId == sessionId);

            if (session == null)
            {
                return NotFound("Không tìm thấy buổi điểm danh.");
            }

            if (!await IsSectionOwnedByCurrentLecturerAsync(session.SectionId))
            {
                return Forbid();
            }

            var attendances = await _context.Attendances
                .Where(a => a.AttendanceSessionId == sessionId)
                .Include(a => a.Student)
                .OrderBy(a => a.Student!.StudentCode)
                .ToListAsync();

            ViewBag.Session = session;

            return View(attendances);
        }

        // =========================================================
        // 8. FORM CHỌN TUẦN VÀ TRẠNG THÁI ĐỂ XUẤT EXCEL
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ExportOptions(int sectionId)
        {
            if (!await IsSectionOwnedByCurrentLecturerAsync(sectionId))
            {
                return Forbid();
            }

            // Chỉ cho chọn các tuần đã có buổi điểm danh -> không bao giờ gõ sai
            var weeks = await _context.AttendanceSessions
                .Where(s => s.SectionId == sectionId)
                .Select(s => s.Week)
                .Distinct()
                .ToListAsync();

            ViewBag.Weeks = weeks
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Select(w => w!)
                .OrderBy(w => w.Length)   // "Tuần 2" đứng trước "Tuần 10"
                .ThenBy(w => w)
                .ToList();

            ViewBag.SectionId = sectionId;

            return View(new AttendanceSessionModel { SectionId = sectionId });
        }

        // =========================================================
        // 9. XUẤT EXCEL ĐIỂM DANH
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(
            int sectionId,
            string? week,
            string status = "all")
        {
            if (!await IsSectionOwnedByCurrentLecturerAsync(sectionId))
            {
                return Forbid();
            }

            week = week?.Trim();

            if (string.IsNullOrWhiteSpace(week))
            {
                TempData["error"] = "Vui lòng chọn tuần cần xuất Excel!";
                return RedirectToAction(nameof(ExportOptions), new { sectionId });
            }

            status = (status ?? "all").Trim().ToLowerInvariant();

            if (status != "all" && !StatusMap.ContainsKey(status))
            {
                TempData["error"] = "Trạng thái xuất Excel không hợp lệ!";
                return RedirectToAction(nameof(ExportOptions), new { sectionId });
            }

            IQueryable<AttendanceModel> query = _context.Attendances
                .Where(a =>
                    a.AttendanceSession != null &&
                    a.AttendanceSession.SectionId == sectionId &&
                    a.AttendanceSession.Week == week)
                .Include(a => a.AttendanceSession!)
                    .ThenInclude(s => s.CourseSection!)
                        .ThenInclude(cs => cs.Course)
                .Include(a => a.Student);

            if (StatusMap.TryGetValue(status, out var statusValue))
            {
                query = query.Where(a => a.AttendanceStatus == statusValue);
            }

            var data = await query
                .OrderBy(a => a.Student!.StudentCode)
                .ToListAsync();

            if (data.Count == 0)
            {
                TempData["error"] = "Không có dữ liệu phù hợp để xuất Excel!";
                return RedirectToAction(nameof(ExportOptions), new { sectionId });
            }

            using var workbook = new XLWorkbook();

            var safeWeek = week
                .Replace(":", "-")
                .Replace("/", "-")
                .Replace("\\", "-")
                .Replace("[", "(")
                .Replace("]", ")")
                .Replace("*", "-")
                .Replace("?", "-");

            var sheetName = $"DiemDanh_{sectionId}_{safeWeek}";

            if (sheetName.Length > 31)
                sheetName = sheetName[..31];

            var worksheet = workbook.Worksheets.Add(sheetName);

            string[] headers =
            {
                "STT", "Mã sinh viên", "Tên sinh viên", "Giới tính",
                "Số điện thoại", "Mã môn học", "Mã lớp học phần",
                "Ngày học", "Tuần học", "Tiết học",
                "Có mặt", "Vắng phép", "Vắng không phép", "Muộn",
                "Trạng thái", "Ghi chú"
            };

            for (int col = 0; col < headers.Length; col++)
            {
                worksheet.Cell(1, col + 1).Value = headers[col];
            }

            var headerRange = worksheet.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            int row = 2;
            int stt = 1;

            foreach (var item in data)
            {
                var student = item.Student;
                var attendanceSession = item.AttendanceSession;
                var courseSection = attendanceSession?.CourseSection;
                var course = courseSection?.Course;

                worksheet.Cell(row, 1).Value = stt++;
                worksheet.Cell(row, 2).Value = student?.StudentCode ?? "";
                worksheet.Cell(row, 3).Value = student?.FullName ?? "";
                worksheet.Cell(row, 4).Value = student?.Gender?.ToString() ?? "";
                worksheet.Cell(row, 5).Value = student?.PhoneNumber?.ToString() ?? "";
                worksheet.Cell(row, 6).Value = course?.CourseCode ?? "";
                worksheet.Cell(row, 7).Value = courseSection?.SectionCode ?? "";

                worksheet.Cell(row, 8).Value =
                    attendanceSession?.SessionDate.ToString("dd/MM/yyyy") ?? "";

                worksheet.Cell(row, 9).Value = attendanceSession?.Week ?? "";

                worksheet.Cell(row, 10).Value =
                    courseSection == null
                        ? ""
                        : $"{courseSection.StartPeriod} - {courseSection.EndPeriod}";

                worksheet.Cell(row, 11).Value = item.AttendanceStatus == 1 ? "X" : "";
                worksheet.Cell(row, 12).Value = item.AttendanceStatus == 2 ? "X" : "";
                worksheet.Cell(row, 13).Value = item.AttendanceStatus == 3 ? "X" : "";
                worksheet.Cell(row, 14).Value = item.AttendanceStatus == 4 ? "X" : "";

                worksheet.Cell(row, 15).Value =
                    GetAttendanceStatusName(item.AttendanceStatus);

                worksheet.Cell(row, 16).Value = item.Note ?? "";

                row++;
            }

            worksheet.Columns().AdjustToContents();
            worksheet.SheetView.FreezeRows(1);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var fileName =
                $"DiemDanh_Lop{sectionId}_Tuan{safeWeek}_" +
                $"{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // =========================================================
        // 10. CHUYỂN MÃ TRẠNG THÁI THÀNH TÊN
        // =========================================================

        private static string GetAttendanceStatusName(int status)
        {
            return status switch
            {
                0 => "Chưa điểm danh",
                1 => "Có mặt",
                2 => "Vắng phép",
                3 => "Vắng không phép",
                4 => "Muộn",
                _ => "Không xác định"
            };
        }
    }
}