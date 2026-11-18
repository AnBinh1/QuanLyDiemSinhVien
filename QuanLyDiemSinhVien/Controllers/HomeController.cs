using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Models;
using QuanLyDiemSinhVien.Models.ViewModel;
using QuanLyDiemSinhVien.Repository;

namespace QuanLyDiemSinhVien.Controllers;

public class HomeController : Controller
{

    private const string AnnouncementCategory = "THÔNG BÁO";
    private readonly DataContext _context;

    public HomeController(DataContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var published = _context.News
            .AsNoTracking()
            .Where(n => n.IsPublished);

        // Tin tức: tất cả chuyên mục trừ THÔNG BÁO
        var mainNews = published.Where(n => n.Category != AnnouncementCategory);

        var featured = await mainNews
            .OrderByDescending(n => n.IsFeatured)
            .ThenByDescending(n => n.PublishedDate)
            .ThenByDescending(n => n.Id)
            .FirstOrDefaultAsync();

        var featuredId = featured?.Id ?? 0;

        var latest = await mainNews
            .Where(n => n.Id != featuredId)
            .OrderByDescending(n => n.PublishedDate)
            .ThenByDescending(n => n.Id)
            .Take(3)
            .ToListAsync();

        // Thông báo: cột bên phải
        var announcements = await published
            .Where(n => n.Category == AnnouncementCategory)
            .OrderByDescending(n => n.PublishedDate)
            .ThenByDescending(n => n.Id)
            .Take(7)
            .ToListAsync();

        return View(new NewViewModel
        {
            FeaturedNews = featured,
            LatestNews = latest,
            Announcements = announcements
        });
    }
}