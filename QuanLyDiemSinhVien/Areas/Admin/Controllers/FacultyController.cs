using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Models;
using QuanLyDiemSinhVien.Models.ViewModel;
using QuanLyDiemSinhVien.Repository;

namespace QuanLyDiemSinhVien.Areas.Admin.Controllers
{
	[Authorize(Roles = "Admin")]
	[Area("Admin")]
	public class FacultyController : Controller
	{
		private readonly DataContext _context;

		public FacultyController(DataContext context)
		{
			_context = context;
		}

		public IActionResult Index()
		{
			var faculties = _context.Faculties.ToList();
			return View(faculties);
		}

		[HttpGet]
		public IActionResult Create()
		{
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(FacultyModel faculty)
		{
			if (!ModelState.IsValid)
			{
				TempData["error"] = "Vui lòng nhập đầy đủ thông tin hợp lệ!";
				return View(faculty);
			}

			//Kiểm tra trùng mã khoa-viện
			var existCode = await _context.Faculties
				.FirstOrDefaultAsync(f => f.FacultyCode.ToLower().Trim() == faculty.FacultyCode.ToLower().Trim());
			if (existCode != null)
			{
				TempData["error"] = $"Mã khoa-viện '{faculty.FacultyCode}' đã tồn tại trong cơ sở dữ liệu, vui lòng chọn mã khoa-viện khác!";
				return View(faculty);
			}

			//Kiểm tra trùng tên khoa-viện
			var existName = await _context.Faculties
				.FirstOrDefaultAsync(f => f.FacultyName.ToLower().Trim() == faculty.FacultyName.ToLower().Trim());
			if (existName != null)
			{
				TempData["error"] = $"Tên khoa-viện '{faculty.FacultyName}' đã tồn tại trong cơ sở dữ liệu, vui lòng chọn tên khoa-viện khác!";
				return View(faculty);
			}

			try
			{
				faculty.FacultyDescription = faculty.FacultyDescription?.Trim();

				_context.Faculties.Add(faculty);
				await _context.SaveChangesAsync();

				TempData["success"] = "Thêm khoa-viện thành công!";
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				TempData["error"] = $"Lỗi khi thêm khoa-viện: {ex.Message}";
				return View(faculty);
			}
		}


		[HttpGet]
		public async Task<IActionResult> Edit(int id)
		{
			var faculty = await _context.Faculties.FindAsync(id);
			if (faculty == null)
			{
				TempData["error"] = "Không tìm thấy khoa-viện!";
				return RedirectToAction("Index");
			}
			return View(faculty);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, FacultyModel faculty)
		{
			if (id != faculty.Id)
			{
				TempData["error"] = "ID khoa-viện không khớp!";
				return RedirectToAction("Index");
			}

			if (!ModelState.IsValid)
			{
				TempData["error"] = "Vui lòng nhập đầy đủ thông tin hợp lệ!";
				return View(faculty);
			}

			//Kiểm tra trùng mã khoa-viện (trừ chính nó)
			var duplicateCode = await _context.Faculties
				.FirstOrDefaultAsync(f => f.FacultyCode.ToLower().Trim() == faculty.FacultyCode.ToLower().Trim() && f.Id != faculty.Id);
			if (duplicateCode != null)
			{
				TempData["error"] = $"Mã khoa-viện '{faculty.FacultyCode}' đã tồn tại trong cơ sở dữ liệu, vui lòng chọn mã khoa-viện khác!";
				return View(faculty);
			}

			//Kiểm tra trùng tên khoa-viện (trừ chính nó)
			var duplicateName = await _context.Faculties
				.FirstOrDefaultAsync(f => f.FacultyName.ToLower().Trim() == faculty.FacultyName.ToLower().Trim() && f.Id != faculty.Id);
			if (duplicateName != null)
			{
				TempData["error"] = $"Tên khoa-viện '{faculty.FacultyName}' đã tồn tại trong cơ sở dữ liệu, vui lòng chọn mã khoa-viện khác!";
				return View(faculty);
			}

			try
			{
				faculty.FacultyDescription = faculty.FacultyDescription?.Trim();

				_context.Faculties.Update(faculty);
				await _context.SaveChangesAsync();

				TempData["success"] = "Cập nhật khoa-viện thành công!";
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				TempData["error"] = $"Lỗi khi cập nhật khoa-viện: {ex.Message}";
				return View(faculty);
			}
		}


		public async Task<IActionResult> Delete(int Id)
		{
			var faculty = await _context.Faculties.FindAsync(Id);
			if (faculty == null)
			{
				TempData["error"] = "Không tìm thấy khoa-viện để xóa!";
				return RedirectToAction("Index");
			}

			_context.Faculties.Remove(faculty);
			await _context.SaveChangesAsync();
			TempData["success"] = "Khoa-viện đã được xóa thành công!";
			return RedirectToAction("Index");
		}
	}
}
