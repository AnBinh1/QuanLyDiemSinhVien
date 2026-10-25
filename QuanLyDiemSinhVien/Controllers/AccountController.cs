using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuanLyDiemSinhVien.Repository;

namespace QuanLyDiemSinhVien.Controllers
{
    public class AccountController : Controller
    {
        public readonly UserManager<IdentityUser> _userManager;
        public readonly SignInManager<IdentityUser> _signInManager;
        private readonly DataContext _context;

        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, DataContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(Models.ViewModel.LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Kiểm tra đăng nhập
            var result = await _signInManager.PasswordSignInAsync(
                model.UserName,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // Lấy User
                var user = await _userManager.FindByNameAsync(model.UserName);
                // Lấy danh sách role
                var roles = await _userManager.GetRolesAsync(user);

                // Nếu là Student → kiểm tra có hồ sơ Student không trước khi cho vào
                if (roles.Contains("Student"))
                {
                    var student = _context.Students.FirstOrDefault(x => x.UserId == user.Id);
                    if (student == null)
                    {
                        // Đăng xuất lại vì đã đăng nhập thành công ở bước trên nhưng thiếu hồ sơ
                        await _signInManager.SignOutAsync();
                        ModelState.AddModelError("", "Tài khoản của bạn chưa có hồ sơ sinh viên. Vui lòng liên hệ quản trị viên.");
                        return View(model);
                    }
                    return RedirectToAction("Index", "Home", new { area = "Student" });
                }

                // Nếu là Teacher → đi vào Admin Area
                if (roles.Contains("Lecturer"))
                {
                    return RedirectToAction("Index", "Home", new { area = "Lecturer" });
                }
                // Nếu là Admin → cũng đi vào Admin Area
                if (roles.Contains("Admin"))
                {
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                }
                // Nếu không có role nào → đưa về trang Home bình thường
                return RedirectToAction("Index", "Home");
            }

            // Nếu login FAILED
            ModelState.AddModelError("", "Đăng nhập không thành công. Vui lòng kiểm tra lại UserName và Password.");
            return View(model);
        }

        [HttpGet]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        public async Task<IActionResult> Logout(string returnUrl = "/")
        {
            await _signInManager.SignOutAsync();
            return Redirect("/account/login");
        }
    }
}