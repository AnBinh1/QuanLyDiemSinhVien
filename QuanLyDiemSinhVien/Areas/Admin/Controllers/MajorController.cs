using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Models;
using QuanLyDiemSinhVien.Repository;

namespace QuanLyDiemSinhVien.Areas.Admin.Controllers
{
	[Authorize(Roles = "Admin")]
	[Area("Admin")]
	public class MajorController : Controller
	{
		private readonly DataContext _context;

		public MajorController(DataContext context)
		{
			_context = context;
		}

		//Hiển thị danh sách ngành học
		public async Task<IActionResult> Index()
		{
			var majors = await _context.Majors
				.Include(m => m.Faculty)
				.OrderByDescending(m => m.MajorId)
				.ToListAsync();
			return View(majors);
		}

		//Tạo mới ngành học
		[HttpGet]
		public async Task<IActionResult> Create()
		{
			ViewBag.FacultyList = new SelectList(await _context.Faculties.ToListAsync(), "Id", "FacultyName");
			return View();
		}

		//Tạo mới ngành học
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(MajorModel major)
		{
			ViewBag.FacultyList = new SelectList(await _context.Faculties.ToListAsync(), "Id", "FacultyName", major.FacultyId);
			if (!ModelState.IsValid)
			{
				TempData["error"] = "Vui lòng nhập đầy đủ thông tin hợp lệ!";
				return View(major);
			}

			//Kiểm tra trùng mã Ngành Học
			var existCode = await _context.Majors
				.FirstOrDefaultAsync(f => f.MajorCode.ToLower().Trim() == major.MajorCode.ToLower().Trim());
			if (existCode != null)
			{
				TempData["error"] = $"Mã ngành học '{major.MajorCode}' đã tồn tại trong cơ sở dữ liệu, vui lòng chọn mã ngành học khác!";
				return View(major);
			}


			//Kiểm tra trùng tên ngành học
			var existName = await _context.Majors
				.FirstOrDefaultAsync(f => f.MajorName.ToLower().Trim() == major.MajorName.ToLower().Trim());
			if (existName != null)
			{
				TempData["error"] = $"Tên ngành học '{major.MajorName}' đã tồn tại trong cơ sở dữ liệu, vui lòng chọn tên ngành học khác!";
				return View(major);
			}
			try
			{
				major.MajorDescription = major.MajorDescription?.Trim();

				_context.Majors.Add(major);
				await _context.SaveChangesAsync();

				TempData["success"] = "Thêm ngành học thành công!";
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				TempData["error"] = $"Lỗi khi thêm ngành học: {ex.Message}";
				return View(major);
			}
		}
		//cập nhật ngành học
		[HttpGet]
		public async Task<IActionResult> Edit(int id)
		{
			var major = await _context.Majors.FindAsync(id);
			if (major == null)
			{
				TempData["error"] = "Không tìm thấy ngành học!";
				return RedirectToAction("Index");
			}

			ViewBag.FacultyList = new SelectList(await _context.Faculties.ToListAsync(), "Id", "FacultyName", major.FacultyId);
			return View(major);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, MajorModel major)
		{
			if (id != major.MajorId)
			{
				TempData["error"] = "ID ngành học không khớp!";
				return RedirectToAction("Index");
			}

			if (!ModelState.IsValid)
			{
				TempData["error"] = "Vui lòng nhập đầy đủ thông tin hợp lệ!";
				return View(major);
			}

			ViewBag.FacultyList = new SelectList(await _context.Faculties.ToListAsync(), "Id", "FacultyName", major.FacultyId);
			//Kiểm tra trùng mã Ngành học(trừ chính nó)
			var duplicateCode = await _context.Majors
				.FirstOrDefaultAsync(f => f.MajorCode.ToLower().Trim() == major.MajorCode.ToLower().Trim() && f.MajorId != major.MajorId);
			if (duplicateCode != null)
			{
				TempData["error"] = $"Mã ngành học '{major.MajorCode}' đã tồn tại trong cơ sở dữ liệu, vui lòng chọn mã ngành học khác!";
				return View(major);
			}
			// Kiểm tra trùng tên Ngành Học(trừ chính nó)
			var duplicateName = await _context.Majors
				.FirstOrDefaultAsync(m => m.MajorName.ToLower().Trim() == major.MajorName.ToLower().Trim() && m.MajorId != major.MajorId);

			if (duplicateName != null)
			{
				TempData["error"] = $"Tên ngành học '{major.MajorName}' đã tồn tại trong cơ sở dữ liệu, vui lòng nhập tên ngành học khác!";
				return View(major);
			}

			try
			{
				major.MajorDescription = major.MajorDescription?.Trim();

				_context.Majors.Update(major);
				await _context.SaveChangesAsync();

				TempData["success"] = "Cập nhật ngành học thành công!";
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				TempData["error"] = $"Lỗi khi cập nhật khoa-viện: {ex.Message}";
				return View(major);
			}
		}

		//xoá ngành học
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(int id)
		{
			var major = await _context.Majors.FindAsync(id);
			if (major == null)
			{
				TempData["error"] = "Không tìm thấy ngành học cần xóa!";
				return RedirectToAction("Index");
			}

			try
			{
				_context.Majors.Remove(major);
				await _context.SaveChangesAsync();

				TempData["success"] = "Đã xóa ngành học thành công!";
			}
			catch (Exception ex)
			{
				TempData["error"] = $"Lỗi khi xóa ngành học: {ex.Message}";
			}

			return RedirectToAction("Index");
		}
	}
}
