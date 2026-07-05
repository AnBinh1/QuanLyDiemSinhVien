using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace QuanLyDiemSinhVien.Controllers
{
	public class AccountController : Controller
	{
		public readonly UserManager<IdentityUser> _userManager;
		public readonly SignInManager<IdentityUser> _signInManager;
		public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
		{
			_userManager = userManager;
			_signInManager = signInManager;
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

				// KIỂM TRA ROLE VÀ ĐIỀU HƯỚNG
				// Nếu là Student → đi vào Student Area
				if (roles.Contains("Student"))
				{
					return RedirectToAction("Index", "Home", new { area = "Student" });
				}

				// Nếu là Teacher → đi vào Admin Area
				if (roles.Contains("Lecturer"))
				{
					return RedirectToAction("Index", "Home", new { area = "Admin" });
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

		public async Task<IActionResult> Logout(string returnUrl = "/")
		{
			await _signInManager.SignOutAsync();
			
			return Redirect("/account/login");
		}

	}
}
