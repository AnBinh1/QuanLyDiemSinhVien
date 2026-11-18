using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Models;
using QuanLyDiemSinhVien.Repository;

namespace QuanLyDiemSinhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class NewsController : Controller
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] AllowedExtensions =
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        private static readonly string[] AllowedFileExtensions =
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".zip"
        };

        private const long MaxImageSize = 5 * 1024 * 1024;   // 5MB
        private const long MaxFileSize = 20 * 1024 * 1024;   // 20MB

        public NewsController(DataContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // =========================
        // DANH SÁCH TIN TỨC
        // =========================
        public async Task<IActionResult> Index(string? keyword)
        {
            var query = _context.News.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(n => n.Title.Contains(keyword));
            }

            ViewBag.Keyword = keyword;

            var list = await query
                .OrderByDescending(n => n.PublishedDate)
                .ThenByDescending(n => n.Id)
                .ToListAsync();

            return View(list);
        }

        public IActionResult Create()
        {
            return View(new News());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(News model, IFormFile? imageFile)
        {
            ValidateImage(imageFile);

            if (!ModelState.IsValid) return View(model);

            model.ImageUrl = await SaveImageAsync(imageFile);
            model.CreatedAt = DateTime.UtcNow;

            if (model.IsFeatured) await ClearOtherFeaturedAsync(null);

            _context.News.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã thêm tin tức.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null) return NotFound();
            return View(news);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, News model, IFormFile? imageFile)
        {
            if (id != model.Id) return BadRequest();

            var news = await _context.News.FindAsync(id);
            if (news == null) return NotFound();

            ValidateImage(imageFile);
            if (!ModelState.IsValid)
            {
                model.ImageUrl = news.ImageUrl; // giữ ảnh cũ để form hiển thị lại
                return View(model);
            }

            news.Title = model.Title;
            news.Summary = model.Summary;
            news.Content = model.Content;
            news.Category = model.Category;
            news.PublishedDate = model.PublishedDate;
            news.IsPublished = model.IsPublished;
            news.IsFeatured = model.IsFeatured;

            if (imageFile != null && imageFile.Length > 0)
            {
                DeleteImage(news.ImageUrl);
                news.ImageUrl = await SaveImageAsync(imageFile);
            }

            if (news.IsFeatured) await ClearOtherFeaturedAsync(news.Id);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật tin tức.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null) return NotFound();

            DeleteImage(news.ImageUrl);
            _context.News.Remove(news);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa tin tức.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null) return NotFound();

            news.IsPublished = !news.IsPublished;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Upload ảnh chèn trong nội dung bài viết (gọi từ trình soạn thảo)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Chưa chọn ảnh." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return BadRequest(new { error = "Chỉ cho phép ảnh .jpg, .jpeg, .png, .webp, .gif" });

            if (file.Length > MaxImageSize)
                return BadRequest(new { error = "Ảnh tối đa 5MB." });

            var saved = await SaveImageAsync(file, "content");
            return Json(new { location = Url.Content(saved!) });
        }

        // Upload file đính kèm (PDF, Word, Excel...) để gắn link trong nội dung
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(25 * 1024 * 1024)]
        public async Task<IActionResult> UploadFile(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Chưa chọn file." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedFileExtensions.Contains(ext))
                return BadRequest(new { error = "Chỉ cho phép file .pdf, .doc, .docx, .xls, .xlsx, .ppt, .pptx, .zip" });

            if (file.Length > MaxFileSize)
                return BadRequest(new { error = "File tối đa 20MB." });

            var folder = Path.Combine(_env.WebRootPath, "uploads", "news", "files");
            Directory.CreateDirectory(folder);

            // Giữ tên gốc (bỏ dấu, ký tự đặc biệt) + mã ngắn để không bị trùng
            var baseName = MakeSafeFileName(Path.GetFileNameWithoutExtension(file.FileName));
            var fileName = $"{Guid.NewGuid():N}".Substring(0, 8) + "-" + baseName + ext;

            var fullPath = Path.Combine(folder, fileName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Json(new
            {
                location = Url.Content($"~/uploads/news/files/{fileName}"),
                name = file.FileName
            });
        }

        // ---------- helpers ----------

        private bool ValidateImage(IFormFile? file)
        {
            if (file == null || file.Length == 0) return true;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                ModelState.AddModelError("imageFile", "Chỉ cho phép ảnh .jpg, .jpeg, .png, .webp, .gif");
                return false;
            }

            if (file.Length > MaxImageSize)
            {
                ModelState.AddModelError("imageFile", "Ảnh tối đa 5MB");
                return false;
            }

            return true;
        }

        private async Task<string?> SaveImageAsync(IFormFile? file, string? subFolder = null)
        {
            if (file == null || file.Length == 0) return null;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            var relative = string.IsNullOrEmpty(subFolder)
                ? "uploads/news"
                : $"uploads/news/{subFolder}";

            var folder = Path.Combine(_env.WebRootPath, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(folder, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"~/{relative}/{fileName}";
        }

        private void DeleteImage(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl) || !imageUrl.StartsWith("~/uploads/news/")) return;

            var path = Path.Combine(_env.WebRootPath, imageUrl.Replace("~/", "").Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }

        // "Quyết định học bổng (2026).pdf" -> "Quyet-dinh-hoc-bong-2026"
        private static string MakeSafeFileName(string name)
        {
            var normalized = name.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }

            var result = sb.ToString()
                .Replace('đ', 'd')
                .Replace('Đ', 'D')
                .Normalize(NormalizationForm.FormC);

            result = Regex.Replace(result, @"[^a-zA-Z0-9]+", "-").Trim('-');

            if (result.Length > 60) result = result.Substring(0, 60).Trim('-');
            return string.IsNullOrEmpty(result) ? "tai-lieu" : result;
        }

        // Chỉ giữ 1 tin nổi bật tại một thời điểm
        private async Task ClearOtherFeaturedAsync(int? exceptId)
        {
            var others = await _context.News
                .Where(n => n.IsFeatured && (exceptId == null || n.Id != exceptId))
                .ToListAsync();

            foreach (var o in others) o.IsFeatured = false;
        }
    }
}