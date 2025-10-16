using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuanLyDiemSinhVien.Models;
using QuanLyDiemSinhVien.Models.ViewModel;
using QuanLyDiemSinhVien.Repository;

namespace QuanLyDiemSinhVien.Areas.Admin.Controllers
{
	[Authorize(Roles = "Admin")]
	[Area("Admin")]
	public class UserRoleController : Controller
	{
		private readonly UserManager<IdentityUser> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;
		public readonly DataContext  _context;
		public UserRoleController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, DataContext context)
		{
			_userManager = userManager;
			_roleManager = roleManager;
			_context = context;
		}

		
		public async Task<IActionResult> Index()
		{
			var users = _userManager.Users.ToList();
			var model = new List<UserWithRoleViewModel>();
			foreach (var user in users)
			{
				var userRoles = _userManager.GetRolesAsync(user).Result;
				model.Add(new UserWithRoleViewModel
				{
					User = user,
					Roles = userRoles.ToList()
				});
			}
			return View(model);
		}
		[HttpGet]
		public async Task<IActionResult> Manage(string UserId)
		{
			var user = await _userManager.FindByIdAsync(UserId);
			var roles = _roleManager.Roles.ToList();
			var userRoles = await _userManager.GetRolesAsync(user);
			var model = new UserRoleViewModel
			{
				UserId = user.Id,
				UserName = user.UserName,
				Roles = roles.Select(role => new RoleSelection
				{
					RoleName = role.Name,
					IsSelected = userRoles.Contains(role.Name)
				}).ToList()
			};
			
			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Manage(UserRoleViewModel model)
		{
			var user = await _userManager.FindByIdAsync(model.UserId);
			if (user == null)
			{
				return NotFound();
			}
			var userRoles = await _userManager.GetRolesAsync(user);

			var selectedRoles = model.Roles.Where(r => r.IsSelected).Select(r => r.RoleName).ToList();

			var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));
			if (!result.Succeeded)
			{
				ModelState.AddModelError("", "Không thể thêm vai trò cho người dùng");
				return View(model);
			}
			result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));
			if (!result.Succeeded)
			{
				ModelState.AddModelError("", "Không thể xóa vai trò cho người dùng");
				return View(model);
			}
			return RedirectToAction("Index");
		}

	}
}
