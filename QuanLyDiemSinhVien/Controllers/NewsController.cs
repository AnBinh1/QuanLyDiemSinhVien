using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Models.ViewModel;
using QuanLyDiemSinhVien.Repository;

namespace QuanLyDiemSinhVien.Controllers
{

    public class NewsController: Controller
    {

        private readonly DataContext _context;

        public NewsController(DataContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            if (page < 1) page = 1;

            var query = _context.News
                .AsNoTracking()
                .Where(n => n.IsPublished)
                .OrderByDescending(n => n.PublishedDate)
                .ThenByDescending(n => n.Id);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            return View(items);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var article = await _context.News
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id && n.IsPublished);

            if (article == null) return NotFound();

            var category = article.Category;

            // Cùng chuyên mục: 5 tin mới nhất, trừ bài đang xem
            var sameCategory = await _context.News
                .AsNoTracking()
                .Where(n => n.IsPublished && n.Id != id && n.Category == category)
                .OrderByDescending(n => n.PublishedDate)
                .ThenByDescending(n => n.Id)
                .Take(5)
                .ToListAsync();

            // Tin liên quan: tin mới nhất còn lại, không trùng bài đang xem và danh sách bên trên
            var excludeIds = sameCategory.Select(n => n.Id).Append(id).ToList();

            var related = await _context.News
                .AsNoTracking()
                .Where(n => n.IsPublished && !excludeIds.Contains(n.Id))
                .OrderByDescending(n => n.PublishedDate)
                .ThenByDescending(n => n.Id)
                .Take(5)
                .ToListAsync();

            return View(new NewsDetailViewModel
            {
                Article = article,
                SameCategory = sameCategory,
                Related = related
            });
        }
    }
}
