using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Models;
using QuanLyDiemSinhVien.Repository;
using System.Security.Claims;

namespace QuanLyDiemSinhVien.Areas.Lecturer.Controllers
{
	[Authorize(Roles = "Lecturer")]
	[Area("Lecturer")]
	public class HomeController : Controller
	{
		private readonly DataContext _context;
		public HomeController(DataContext context)
		{
			_context = context;
        }

    // Quy ước của CourseSectionModel: 2 = Thứ Hai... 7 = Thứ Bảy.
    // GIẢ ĐỊNH: Chủ Nhật lưu là 8. Nếu dự án bạn lưu Chủ Nhật là 7 thì đổi lại dòng này.
    private static int ToSectionDayOfWeek(DateTime d) =>
        d.DayOfWeek == System.DayOfWeek.Sunday ? 8 : (int)d.DayOfWeek + 1;

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Giảng viên đang đăng nhập
            var lecturer = await _context.Lecturers
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.UserId == userId);

            if (lecturer == null)
                return Forbid();

            var facultyName = await _context.Faculties
                .Where(f => f.Id == lecturer.FacultyId)
                .Select(f => f.FacultyName)
                .FirstOrDefaultAsync();

            var vm = new LecturerDashboardViewModel
            {
                LecturerCode = lecturer.LecturerCode,
                FullName = lecturer.FullName,
                FacultyName = facultyName ?? "",
                Position = string.IsNullOrWhiteSpace(lecturer.Position) ? "Giảng viên" : lecturer.Position,
                AvatarUrl = null // TODO: gán đường dẫn ảnh nếu LecturerModel có cột lưu đường dẫn (ngoài AvatarFile)
            };

            // 2. Các lớp học phần giảng viên phụ trách
            var sections = await _context.CourseSections
                .AsNoTracking()
                .Include(s => s.Course)
                .Where(s => s.LecturerId == lecturer.LecturerId)
                .ToListAsync();

            var sectionIds = sections.Select(s => s.SectionId).ToList();

            // 3. Số sinh viên đã có điểm theo từng lớp
            // GIẢ ĐỊNH: StudentGradeModel có SectionId và StudentId
            var gradedByDict = await _context.Grades
                 .AsNoTracking()
                 .Where(g => sectionIds.Contains(g.Enrollment.SectionId))
                 .GroupBy(g => g.Enrollment.SectionId)
                 .Select(g => new { SectionId = g.Key, Count = g.Select(x => x.EnrollmentId).Distinct().Count() })
                 .ToDictionaryAsync(x => x.SectionId, x => x.Count);

            // 4. Tiến độ nhập điểm (sĩ số lấy từ RegisteredStudents của CourseSectionModel)
            vm.Sections = sections.Select(s => new SectionProgressItem
            {
                SectionId = s.SectionId,
                SectionCode = s.SectionCode,
                CourseName = s.Course?.CourseName ?? "",
                TotalStudents = s.RegisteredStudents,
                GradedStudents = gradedByDict.GetValueOrDefault(s.SectionId),
                IsGradeLocked = s.IsGradeLocked
            }).ToList();

            // 5. Lịch dạy hôm nay: lớp có DayOfWeek trùng hôm nay và hôm nay nằm trong StartDate..EndDate
            var todayDow = ToSectionDayOfWeek(today);
            vm.TodaySchedule = sections
                .Where(s => s.DayOfWeek == todayDow)
                .OrderBy(s => s.StartPeriod)
                .Select(s => new TeachingScheduleItem
                {
                    SectionId = s.SectionId,
                    SectionCode = s.SectionCode,
                    CourseName = s.Course?.CourseName ?? "",
                    Room = s.Room ?? "",
                    StartPeriod = s.StartPeriod,
                    EndPeriod = s.EndPeriod,
                    StudentCount = s.RegisteredStudents
                })
                .ToList();

            // 6. Công việc: lớp chưa khóa điểm và chưa nhập đủ điểm.
            // Hạn tạm lấy là EndDate của lớp học phần (CourseSectionModel chưa có cột hạn nhập điểm).
            var pendingTasks = vm.Sections
                .Join(sections, p => p.SectionId, s => s.SectionId, (p, s) => new { p, s })
                .Where(x => !x.p.IsGradeLocked
                            && x.p.GradedStudents < x.p.TotalStudents
                            && x.s.EndDate != null)
                .Select(x => new LecturerTaskItem
                {
                    Title = "Nhập điểm",
                    Type = "Grade",
                    SectionId = x.s.SectionId,
                    SectionCode = x.s.SectionCode,
                    DueDate = x.s.EndDate!.Value
                })
                .ToList();

            vm.UpcomingTasks = pendingTasks.Where(t => t.DueDate.Date >= today).ToList();
            vm.OverdueTasks = pendingTasks.Where(t => t.DueDate.Date < today).ToList();

            // 7. Điểm danh gần đây
            var sessions = await _context.AttendanceSessions
                .AsNoTracking()
                .Where(a => sectionIds.Contains(a.SectionId))
                .OrderByDescending(a => a.SessionDate)
                .Take(5)
                .ToListAsync();

            var sessionIds = sessions.Select(s => s.AttendanceSessionId).ToList();

            // >>> CHỈ SỬA DÒNG NÀY: copy điều kiện hiển thị badge "Có mặt" trong view chi tiết buổi học
            // (đổi item -> a). Ví dụ: a.Status == 1 | a.IsPresent | a.AttendanceStatus == "Có mặt"
            // Muốn tính cả "Đi muộn" thì thêm: || a.AttendanceStatus == "Đi muộn"
            Func<AttendanceModel, bool> isPresent = a => a.AttendanceStatus == 1 || a.AttendanceStatus == 2;

            // Lấy dữ liệu về bộ nhớ rồi mới đếm (Func không dịch được sang SQL)
            var attendances = await _context.Attendances
                .AsNoTracking()
                .Where(a => sessionIds.Contains(a.AttendanceSessionId))
                .ToListAsync();

            var attendanceStats = attendances
                .GroupBy(a => a.AttendanceSessionId)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Total = g.Count(),
                        Present = g.Count(isPresent)
                    });

            var codeBySection = sections.ToDictionary(s => s.SectionId, s => s.SectionCode);

            // Đánh số buổi theo thứ tự thời gian trong từng lớp
            var sessionNumbers = await _context.AttendanceSessions
                .AsNoTracking()
                .Where(a => sectionIds.Contains(a.SectionId))
                .OrderBy(a => a.SessionDate)
                .Select(a => new { a.AttendanceSessionId, a.SectionId })
                .ToListAsync();

            var numberById = sessionNumbers
                .GroupBy(x => x.SectionId)
                .SelectMany(g => g.Select((x, i) => new { x.AttendanceSessionId, Number = i + 1 }))
                .ToDictionary(x => x.AttendanceSessionId, x => x.Number);

            vm.RecentAttendances = sessions.Select(s =>
            {
                attendanceStats.TryGetValue(s.AttendanceSessionId, out var st);
                return new RecentAttendanceItem
                {
                    SectionId = s.SectionId,
                    SectionCode = codeBySection.GetValueOrDefault(s.SectionId, ""),
                    SessionNumber = numberById.GetValueOrDefault(s.AttendanceSessionId),
                    Date = s.SessionDate,
                    Present = st?.Present ?? 0,
                    Total = st?.Total ?? 0
                };
            }).ToList();

            // 8. Buổi chưa điểm danh: lớp có lịch dạy hôm nay nhưng chưa có buổi điểm danh hôm nay
            var takenToday = await _context.AttendanceSessions
                .AsNoTracking()
                .Where(a => sectionIds.Contains(a.SectionId) && a.SessionDate.Date == today)
                .Select(a => a.SectionId)
                .Distinct()
                .ToListAsync();

            vm.SessionsNotTaken = vm.TodaySchedule.Count(s => !takenToday.Contains(s.SectionId));

            return View(vm);
        }
    }
}

