using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
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
    public class LecturerController : Controller
    {
        private readonly DataContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public LecturerController(
            DataContext context,
            UserManager<IdentityUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // =========================================================
        // DANH SÁCH GIẢNG VIÊN
        // =========================================================
        public async Task<IActionResult> Index()
        {
            var lecturers = await _context.Lecturers
                .Include(l => l.Major)
                .Include(l => l.Faculty)
                .Include(l => l.User)
                .OrderByDescending(l => l.LecturerId)
                .ToListAsync();

            return View(lecturers);
        }

        // =========================================================
        // CHI TIẾT GIẢNG VIÊN
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var lecturer = await _context.Lecturers
                .Include(l => l.Major)
                .Include(l => l.Faculty)
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.LecturerId == id);

            if (lecturer == null)
            {
                TempData["error"] = "Không tìm thấy thông tin giảng viên.";
                return RedirectToAction("Index");
            }

            return View(lecturer);
        }

        // =========================================================
        // TÌM KIẾM
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Search(string searchString)
        {
            ViewBag.SearchString = searchString;

            var LecturersQuery = _context.Lecturers
                .Include(c => c.Major)
                .Include(m => m.Faculty)
                .Include(s => s.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim().ToLower();

                LecturersQuery = LecturersQuery.Where(s =>
                    (s.LecturerCode != null &&
                     s.LecturerCode.ToLower().Contains(searchString)) ||

                    (s.FullName != null &&
                     s.FullName.ToLower().Contains(searchString))
                );
            }

            var lecturer = await LecturersQuery
                .OrderByDescending(s => s.LecturerId)
                .ToListAsync();

            return View("Index", lecturer);
        }
        [HttpGet]
        // Hàm xuất file Excel danh sách giảng viên
        public async Task<IActionResult> ExportToExcel(string status = "all")
        {
            var query = _context.Lecturers
                .Include(l => l.Faculty)
                .Include(l => l.Major)
                .AsQueryable();

            // =========================
            // LỌC TRẠNG THÁI
            // =========================

            if (status == "active")
            {
                query = query.Where(l => l.IsActive);
            }
            else if (status == "inactive")
            {
                query = query.Where(l => !l.IsActive);
            }

            // =========================
            // LẤY DỮ LIỆU
            // =========================

            var data = await query
                .OrderBy(l => l.LecturerCode)
                .Select(l => new
                {
                    LecturerCode = l.LecturerCode ?? "",
                    FullName = l.FullName ?? "",
                    Gender = l.Gender ?? "",
                    l.DateOfBirth,

                    FacultyName = l.Faculty != null
                        ? (l.Faculty.FacultyName ?? "")
                        : "",

                    MajorName = l.Major != null
                        ? (l.Major.MajorName ?? "")
                        : "",

                    Position = l.Position ?? "",
                    AcademicPosition = l.AcademicPosition ?? "",
                    AcademicDegree = l.AcademicDegree ?? "",
                    PhoneNumber = l.PhoneNumber ?? "",
                    Email = l.Email ?? "",

                    l.JoinDate,
                    l.RetireDate,
                    l.IsActive
                })
                .ToListAsync();

            // =========================
            // TẠO WORKBOOK
            // =========================

            using var workbook = new XLWorkbook();

            var worksheet = workbook.Worksheets.Add("Danh sách giảng viên");

            // =========================
            // TIÊU ĐỀ CỘT
            // =========================

            worksheet.Cell(1, 1).Value = "STT";
            worksheet.Cell(1, 2).Value = "Mã giảng viên";
            worksheet.Cell(1, 3).Value = "Họ và tên";
            worksheet.Cell(1, 4).Value = "Giới tính";
            worksheet.Cell(1, 5).Value = "Ngày sinh";
            worksheet.Cell(1, 6).Value = "Khoa-viện";
            worksheet.Cell(1, 7).Value = "Bộ môn/Ngành";
            worksheet.Cell(1, 8).Value = "Chức vụ";
            worksheet.Cell(1, 9).Value = "Học hàm";
            worksheet.Cell(1, 10).Value = "Học vị";
            worksheet.Cell(1, 11).Value = "Số điện thoại";
            worksheet.Cell(1, 12).Value = "Email";
            worksheet.Cell(1, 13).Value = "Ngày vào trường";
            worksheet.Cell(1, 14).Value = "Ngày nghỉ việc";
            worksheet.Cell(1, 15).Value = "Trạng thái";

            // =========================
            // GHI DỮ LIỆU
            // =========================

            int row = 2;
            int stt = 1;

            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = stt++;
                worksheet.Cell(row, 2).Value = item.LecturerCode;
                worksheet.Cell(row, 3).Value = item.FullName;
                worksheet.Cell(row, 4).Value = item.Gender;

                worksheet.Cell(row, 5).Value =
                    item.DateOfBirth.ToString("dd/MM/yyyy");

                worksheet.Cell(row, 6).Value =
                    item.FacultyName;

                worksheet.Cell(row, 7).Value =
                    item.MajorName;

                worksheet.Cell(row, 8).Value =
                    item.Position;

                worksheet.Cell(row, 9).Value =
                    item.AcademicPosition;

                worksheet.Cell(row, 10).Value =
                    item.AcademicDegree;

                worksheet.Cell(row, 11).Value =
                    item.PhoneNumber;

                worksheet.Cell(row, 12).Value =
                    item.Email;

                // Ngày vào trường
                worksheet.Cell(row, 13).Value =
                    item.JoinDate.HasValue
                        ? item.JoinDate.Value.ToString("dd/MM/yyyy")
                        : "";

                // Ngày nghỉ việc
                worksheet.Cell(row, 14).Value =
                    item.RetireDate.HasValue
                        ? item.RetireDate.Value.ToString("dd/MM/yyyy")
                        : "Chưa cập nhật";

                // Trạng thái
                worksheet.Cell(row, 15).Value =
                    item.IsActive
                        ? "Đang công tác"
                        : "Nghỉ công tác";

                row++;
            }

            // =========================
            // ĐỊNH DẠNG HEADER
            // =========================

            var headerRange =
                worksheet.Range(1, 1, 1, 15);

            headerRange.Style.Font.Bold = true;

            headerRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            headerRange.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            headerRange.Style.Border.OutsideBorder =
                XLBorderStyleValues.Thin;

            headerRange.Style.Border.InsideBorder =
                XLBorderStyleValues.Thin;

            // =========================
            // ĐỊNH DẠNG DỮ LIỆU
            // =========================

            if (data.Count > 0)
            {
                var dataRange =
                    worksheet.Range(1, 1, row - 1, 15);

                dataRange.Style.Border.OutsideBorder =
                    XLBorderStyleValues.Thin;

                dataRange.Style.Border.InsideBorder =
                    XLBorderStyleValues.Thin;
            }

            // =========================
            // CĂN GIỮA
            // =========================

            worksheet.Column(1).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            worksheet.Column(2).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            worksheet.Column(4).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            worksheet.Column(5).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            worksheet.Column(13).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            worksheet.Column(14).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            worksheet.Column(15).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            // =========================
            // ĐỘ RỘNG CỘT
            // =========================

            worksheet.Columns().AdjustToContents();

            worksheet.Column(1).Width = 8;
            worksheet.Column(2).Width = 15;
            worksheet.Column(3).Width = 25;
            worksheet.Column(6).Width = 25;
            worksheet.Column(7).Width = 25;
            worksheet.Column(8).Width = 20;
            worksheet.Column(9).Width = 15;
            worksheet.Column(10).Width = 15;
            worksheet.Column(11).Width = 16;
            worksheet.Column(12).Width = 30;
            worksheet.Column(14).Width = 18;
            worksheet.Column(15).Width = 18;

            // =========================
            // CỐ ĐỊNH HÀNG TIÊU ĐỀ
            // =========================

            worksheet.SheetView.FreezeRows(1);

            // =========================
            // XUẤT FILE
            // =========================

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            string fileName =
                $"DanhSachGiangVien_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }
        // =========================================================
        // THÊM GIẢNG VIÊN - GET
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();

            return View();
        }

        // =========================================================
        // THÊM GIẢNG VIÊN - POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LecturerModel lecturer)
        {
            if (!ModelState.IsValid)
                return await ReloadCreateView(lecturer);

            // Kiểm tra trùng mã giảng viên
            if (await _context.Lecturers.AnyAsync(l =>
                l.LecturerCode.ToLower().Trim() ==
                lecturer.LecturerCode.ToLower().Trim()))
            {
                TempData["error"] =
                    $"Mã giảng viên '{lecturer.LecturerCode}' đã tồn tại!";

                return await ReloadCreateView(lecturer);
            }

            // Upload avatar nếu có
            if (lecturer.AvatarFile?.Length > 0)
            {
                lecturer.AvatarUrl =
                    await SaveAvatarAsync(lecturer.AvatarFile);
            }

            try
            {
                _context.Lecturers.Add(lecturer);

                await _context.SaveChangesAsync();

                TempData["success"] =
                    "Thêm giảng viên thành công!";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["error"] =
                    $"Lỗi khi thêm giảng viên: {ex.Message}";

                return await ReloadCreateView(lecturer);
            }
        }

        // =========================================================
        // SỬA GIẢNG VIÊN - GET
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var lecturer = await _context.Lecturers
                .Include(l => l.Major)
                .FirstOrDefaultAsync(l => l.LecturerId == id);

            if (lecturer == null)
                return RedirectToAction("Index");

            // Truyền cả MajorId và UserId
            // để LoadDropdownsAsync biết tài khoản hiện tại
            await LoadDropdownsAsync(
                lecturer.MajorId,
                lecturer.UserId
            );

            return View(lecturer);
        }

        // =========================================================
        // SỬA GIẢNG VIÊN - POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            LecturerModel lecturer)
        {
            if (id != lecturer.LecturerId)
                return RedirectToAction("Index");

            // =====================================================
            // KIỂM TRA NGÀY NGHỈ CÔNG TÁC
            // =====================================================

            // Nếu chọn "Nghỉ công tác"
            if (!lecturer.IsActive)
            {
                // Bắt buộc phải nhập ngày nghỉ
                if (!lecturer.RetireDate.HasValue)
                {
                    ModelState.AddModelError(
                        nameof(lecturer.RetireDate),
                        "Vui lòng nhập ngày nghỉ công tác."
                    );
                }
                // Không cho phép ngày nghỉ lớn hơn ngày hiện tại
                else if (lecturer.RetireDate.Value.Date > DateTime.Now.Date)
                {
                    ModelState.AddModelError(
                        nameof(lecturer.RetireDate),
                        "Ngày nghỉ công tác không được lớn hơn ngày hiện tại."
                    );
                }
            }
            else
            {
                // Nếu đang công tác thì không cần ngày nghỉ
                // và xóa ngày nghỉ cũ nếu trước đó có
                lecturer.RetireDate = null;
            }

            // =====================================================
            // KIỂM TRA MODEL
            // =====================================================

            if (!ModelState.IsValid)
                return await ReloadEditView(lecturer);

            // =====================================================
            // KIỂM TRA TRÙNG MÃ GIẢNG VIÊN
            // =====================================================

            if (await _context.Lecturers.AnyAsync(l =>
                l.LecturerCode.ToLower().Trim() ==
                lecturer.LecturerCode.ToLower().Trim() &&
                l.LecturerId != id))
            {
                TempData["error"] =
                    $"Mã giảng viên '{lecturer.LecturerCode}' đã tồn tại!";

                return await ReloadEditView(lecturer);
            }

            // =====================================================
            // LẤY GIẢNG VIÊN TRONG DATABASE
            // =====================================================

            var lecturerInDb =
                await _context.Lecturers.FindAsync(id);

            if (lecturerInDb == null)
                return RedirectToAction("Index");

            // =====================================================
            // LƯU AVATAR CŨ
            // =====================================================

            var currentAvatar = lecturerInDb.AvatarUrl;

            // =====================================================
            // CẬP NHẬT CÁC FIELD
            // =====================================================

            _context.Entry(lecturerInDb)
                .CurrentValues
                .SetValues(lecturer);

            // Giữ avatar cũ nếu không upload ảnh mới
            lecturerInDb.AvatarUrl = currentAvatar;

            // =====================================================
            // UPLOAD AVATAR MỚI
            // =====================================================

            if (lecturer.AvatarFile?.Length > 0)
            {
                lecturerInDb.AvatarUrl =
                    await SaveAvatarAsync(lecturer.AvatarFile);
            }

            // =====================================================
            // LƯU DATABASE
            // =====================================================

            try
            {
                await _context.SaveChangesAsync();

                TempData["success"] =
                    "Cập nhật giảng viên thành công!";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["error"] =
                    $"Lỗi khi cập nhật: {ex.Message}";

                return await ReloadEditView(lecturer);
            }
        }

        // =========================================================
        // XÓA GIẢNG VIÊN
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var lecturer =
                await _context.Lecturers.FindAsync(id);

            if (lecturer == null)
            {
                TempData["error"] =
                    "Không tìm thấy giảng viên để xóa.";

                return RedirectToAction("Index");
            }

            try
            {
                _context.Lecturers.Remove(lecturer);

                await _context.SaveChangesAsync();

                TempData["success"] =
                    "Xóa giảng viên thành công!";
            }
            catch (Exception ex)
            {
                TempData["error"] =
                    $"Lỗi khi xóa: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // =========================================================
        // LOAD DROPDOWN
        // =========================================================
        private async Task LoadDropdownsAsync(
            int? majorId = null,
            string? userId = null)
        {
            int? facultyId = null;

            // =====================================================
            // LẤY KHOA TỪ NGÀNH
            // =====================================================
            if (majorId.HasValue)
            {
                facultyId = await _context.Majors
                    .Where(m => m.MajorId == majorId)
                    .Select(m => (int?)m.FacultyId)
                    .FirstOrDefaultAsync();
            }

            // =====================================================
            // KHOA
            // =====================================================
            ViewBag.FacultyList = new SelectList(
                await _context.Faculties.ToListAsync(),
                "Id",
                "FacultyName",
                facultyId
            );

            // =====================================================
            // NGÀNH
            // =====================================================
            ViewBag.MajorList = new SelectList(
                await _context.Majors
                    .Where(m =>
                        !facultyId.HasValue ||
                        m.FacultyId == facultyId)
                    .ToListAsync(),
                "MajorId",
                "MajorName",
                majorId
            );

            // =====================================================
            // TÀI KHOẢN
            //
            // Chỉ hiển thị tài khoản:
            // 1. Chưa gán cho sinh viên
            // 2. Chưa gán cho giảng viên
            //
            // Nếu đang Edit thì tài khoản hiện tại
            // của giảng viên vẫn được hiển thị.
            // =====================================================
            var availableUsers = await _userManager.Users
                .Where(u =>
                    (
                        !_context.Students
                            .Any(s => s.UserId == u.Id)
                        &&
                        !_context.Lecturers
                            .Any(l => l.UserId == u.Id)
                    )
                    ||
                    u.Id == userId
                )
                .OrderBy(u => u.UserName)
                .ToListAsync();

            ViewBag.Users = new SelectList(
                availableUsers,
                "Id",
                "UserName",
                userId
            );
        }

        // =========================================================
        // LOAD LẠI VIEW CREATE
        // =========================================================
        private async Task<IActionResult> ReloadCreateView(
            LecturerModel lecturer)
        {
            await LoadDropdownsAsync(
                lecturer.MajorId,
                lecturer.UserId
            );

            return View(lecturer);
        }

        // =========================================================
        // LOAD LẠI VIEW EDIT
        // =========================================================
        private async Task<IActionResult> ReloadEditView(
            LecturerModel lecturer)
        {
            await LoadDropdownsAsync(
                lecturer.MajorId,
                lecturer.UserId
            );

            return View(lecturer);
        }

        // =========================================================
        // LƯU AVATAR
        // =========================================================
        private async Task<string> SaveAvatarAsync(
            Microsoft.AspNetCore.Http.IFormFile avatarFile)
        {
            var folder = Path.Combine(
                _webHostEnvironment.WebRootPath,
                "uploads/lecturers"
            );

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fileName =
                Guid.NewGuid() +
                Path.GetExtension(avatarFile.FileName);

            var filePath =
                Path.Combine(folder, fileName);

            using var stream =
                new FileStream(
                    filePath,
                    FileMode.Create
                );

            await avatarFile.CopyToAsync(stream);

            return "/uploads/lecturers/" + fileName;
        }

        // =========================================================
        // LẤY NGÀNH THEO KHOA
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetMajorsByFaculty(
            int facultyId)
        {
            var majors = await _context.Majors
                .Where(m => m.FacultyId == facultyId)
                .Select(m => new
                {
                    m.MajorId,
                    m.MajorName
                })
                .ToListAsync();

            return Json(majors);
        }
    
    
    
    }


}