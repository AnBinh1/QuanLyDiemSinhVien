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
			{ 
				return View(model);
			}
			var result = await _signInManager.PasswordSignInAsync(
				model.UserName, 
				model.Password, 
				model.RememberMe, 
				lockoutOnFailure: false);

			if (result.Succeeded)
			{
				TempData["success"] = "Đăng nhập thành công!";
				return RedirectToAction("Index", "Home");
			}
			ModelState.AddModelError(string.Empty, "Đăng nhập không thành công. Vui lòng kiểm tra lại UserName và PassWord.");
			
			return View();
		}

	}
}
