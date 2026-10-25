using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuanLyDiemSinhVien.Repository;

namespace QuanLyDiemSinhVien.Areas.Student.Controllers
{
    [Area("Student")]
    [Authorize(Roles = "Student")]
    public class HomeController : Controller
    {
        private readonly DataContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(DataContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy user hiện tại
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Không xác định được user đăng nhập -> quay lại trang đăng nhập
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // Tìm sinh viên theo UserId
            var student = _context.Students
                                  .FirstOrDefault(x => x.UserId == user.Id);

            if (student == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            return View(student); // <-- View của bạn nhận StudentModel
        }
    }
}