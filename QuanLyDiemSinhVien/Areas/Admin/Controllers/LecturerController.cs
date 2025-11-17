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

		public LecturerController(DataContext context, UserManager<IdentityUser> userManager, IWebHostEnvironment webHostEnvironment)
		{
			_context = context;
			_userManager = userManager;
			_webHostEnvironment = webHostEnvironment;
		}

		// ================== Index ==================
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

		// ================== Details ==================
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

		// ================== Create ==================
		[HttpGet]
		public async Task<IActionResult> Create()
		{
			await LoadDropdownsAsync();
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(LecturerModel lecturer)
		{
			if (!ModelState.IsValid) return await ReloadCreateView(lecturer);

			// Kiểm tra trùng mã giảng viên
			if (await _context.Lecturers.AnyAsync(l => l.LecturerCode.ToLower().Trim() == lecturer.LecturerCode.ToLower().Trim()))
			{
				TempData["error"] = $"Mã giảng viên '{lecturer.LecturerCode}' đã tồn tại!";
				return await ReloadCreateView(lecturer);
			}

			// Upload avatar nếu có
			if (lecturer.AvatarFile?.Length > 0)
			{
				lecturer.AvatarUrl = await SaveAvatarAsync(lecturer.AvatarFile);
			}

			try
			{
				_context.Lecturers.Add(lecturer);
				await _context.SaveChangesAsync();
				TempData["success"] = "Thêm giảng viên thành công!";
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				TempData["error"] = $"Lỗi khi thêm giảng viên: {ex.Message}";
				return await ReloadCreateView(lecturer);
			}
		}

		// ================== Edit ==================
		[HttpGet]
		public async Task<IActionResult> Edit(int id)
		{
			var lecturer = await _context.Lecturers.FindAsync(id);
			if (lecturer == null) return RedirectToAction("Index");

			await LoadDropdownsAsync(lecturer.MajorId);
			ViewBag.Users = new SelectList(await _userManager.Users.ToListAsync(), "Id", "UserName", lecturer.UserId);
			return View(lecturer);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, LecturerModel lecturer)
		{
			if (id != lecturer.LecturerId) return RedirectToAction("Index");
			if (!ModelState.IsValid) return await ReloadEditView(lecturer);

			// Kiểm tra trùng mã (ngoại trừ chính bản ghi)
			if (await _context.Lecturers.AnyAsync(l => l.LecturerCode.ToLower().Trim() == lecturer.LecturerCode.ToLower().Trim() && l.LecturerId != id))
			{
				TempData["error"] = $"Mã giảng viên '{lecturer.LecturerCode}' đã tồn tại!";
				return await ReloadEditView(lecturer);
			}

			var lecturerInDb = await _context.Lecturers.FindAsync(id);
			if (lecturerInDb == null) return RedirectToAction("Index");

			// 🔹 Lưu avatar cũ trước
			var currentAvatar = lecturerInDb.AvatarUrl;

			// Cập nhật các field ngoại trừ AvatarUrl
			_context.Entry(lecturerInDb).CurrentValues.SetValues(lecturer);

			// 🔹 Giữ avatar cũ nếu không upload mới
			lecturerInDb.AvatarUrl = currentAvatar;

			// 🔹 Upload avatar nếu có file mới
			if (lecturer.AvatarFile?.Length > 0)
			{
				lecturerInDb.AvatarUrl = await SaveAvatarAsync(lecturer.AvatarFile);
			}

			try
			{
				await _context.SaveChangesAsync();
				TempData["success"] = "Cập nhật giảng viên thành công!";
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				TempData["error"] = $"Lỗi khi cập nhật: {ex.Message}";
				return await ReloadEditView(lecturer);
			}
		}


		// ================== Delete ==================
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(int id)
		{
			var lecturer = await _context.Lecturers.FindAsync(id);
			if (lecturer == null)
			{
				TempData["error"] = "Không tìm thấy giảng viên để xóa.";
				return RedirectToAction("Index");
			}

			try
			{
				_context.Lecturers.Remove(lecturer);
				await _context.SaveChangesAsync();
				TempData["success"] = "Xóa giảng viên thành công!";
			}
			catch (Exception ex)
			{
				TempData["error"] = $"Lỗi khi xóa: {ex.Message}";
			}
			return RedirectToAction("Index");
		}

		// ================== Helper ==================
		private async Task LoadDropdownsAsync(int? majorId = null)
		{
			int? facultyId = null;
			if (majorId.HasValue)
			{
				facultyId = await _context.Majors
					.Where(m => m.MajorId == majorId)
					.Select(m => (int?)m.FacultyId)
					.FirstOrDefaultAsync();
			}

			ViewBag.FacultyList = new SelectList(await _context.Faculties.ToListAsync(), "Id", "FacultyName", facultyId);
			ViewBag.MajorList = new SelectList(await _context.Majors
				.Where(m => !facultyId.HasValue || m.FacultyId == facultyId)
				.ToListAsync(), "MajorId", "MajorName", majorId);
			ViewBag.Users = new SelectList(await _userManager.Users.ToListAsync(), "Id", "UserName");
		}

		private async Task<IActionResult> ReloadCreateView(LecturerModel lecturer)
		{
			await LoadDropdownsAsync();
			return View(lecturer);
		}

		private async Task<IActionResult> ReloadEditView(LecturerModel lecturer)
		{
			await LoadDropdownsAsync(lecturer.MajorId);
			ViewBag.Users = new SelectList(await _userManager.Users.ToListAsync(), "Id", "UserName", lecturer.UserId);
			return View(lecturer);
		}

		private async Task<string> SaveAvatarAsync(Microsoft.AspNetCore.Http.IFormFile avatarFile)
		{
			var folder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/lecturers");
			if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

			var fileName = Guid.NewGuid() + Path.GetExtension(avatarFile.FileName);
			var filePath = Path.Combine(folder, fileName);
			using var stream = new FileStream(filePath, FileMode.Create);
			await avatarFile.CopyToAsync(stream);
			return "/uploads/lecturers/" + fileName;
		}

		// ================== AJAX ==================
		[HttpGet]
		public async Task<IActionResult> GetMajorsByFaculty(int facultyId)
		{
			var majors = await _context.Majors
				.Where(m => m.FacultyId == facultyId)
				.Select(m => new { m.MajorId, m.MajorName })
				.ToListAsync();
			return Json(majors);
		}
	}
}
