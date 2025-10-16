using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuanLyDiemSinhVien.Models.ViewModel;

namespace QuanLyDiemSinhVien.Areas.Admin.Controllers
{
	[Authorize]
	[Area("Admin")]
	public class UserController : Controller
	{
		
		private readonly UserManager<IdentityUser> _userManager;
		public UserController(UserManager<IdentityUser> userManager)
		{
			_userManager = userManager;
		}
		public IActionResult Index()
		{
			var users = _userManager.Users.ToList();
			return View(users);
		}

		[HttpGet]
		public async Task<IActionResult> Create()
		{
			return View();
			//var roles = await _roleManager.Roles.ToListAsync();
			//ViewBag.Roles = new SelectList(roles, "Id", "Name");
			//return View(new UserWithRoleViewModel());
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(UserViewModel user)
		{
			if (ModelState.IsValid)
			{
				var User = new IdentityUser
				{
					UserName = user.UserName,
					Email = user.Email,
					PhoneNumber = user.PhoneNumber
				};
				var Result = await _userManager.CreateAsync(User, user.PasswordHash);

				if (Result.Succeeded)
				{
					//var role = await _roleManager.FindByIdAsync(user.RoleId);
					//if (role != null)
					//{
					//	await _userManager.AddToRoleAsync(appUser, role.Name);
					//}
					TempData["success"] = "Tạo người dùng thành công!";

					return RedirectToAction("Index");
				}
				else
				{
					foreach (var error in Result.Errors)
					{
						ModelState.AddModelError("", error.Description);
					}
				}
			}
			else
			{
				TempData["error"] = "có một vài thứ đang bị lỗi";
				List<string> errors = new List<string>();
				foreach (var value in ModelState.Values)
				{
					foreach (var error in value.Errors)
					{
						errors.Add(error.ErrorMessage);
					}
				}
				string ErrorMessage = string.Join("\n", errors);
				return BadRequest(ErrorMessage);
			}
			//var roles = await _roleManager.Roles.ToListAsync();
			//ViewBag.Roles = new SelectList(roles, "Id", "Name");
			return View(new UserViewModel());

		}

		[HttpGet]
		public async Task<IActionResult> Edit(string Id)
		{
			if (string.IsNullOrEmpty(Id))
			{
				return NotFound();
			}
			var user = await _userManager.FindByIdAsync(Id);
			if (user == null)
			{
				return NotFound();
			}

			//var roles = await _roleManager.Roles.ToListAsync();
			//var userRoles = await _userManager.GetRolesAsync(user);
			//var currentRoleName = userRoles.FirstOrDefault();

			//var role = await _roleManager.FindByNameAsync(currentRoleName ?? "");

			var viewModel = new UserEditViewModel
			{
				Id = user.Id,
				UserName = user.UserName,
				Email = user.Email,
				PhoneNumber = user.PhoneNumber,
				NewPassword = user.PasswordHash,
				//RoleId = role?.Id,
				//RoleName = currentRoleName
			};

			//ViewBag.Roles = new SelectList(roles, "Id", "Name", viewModel.RoleId);
			return View(viewModel);
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(UserEditViewModel model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}

			var user = await _userManager.FindByIdAsync(model.Id);
			if (user == null)
			{
				return NotFound();
			}

			user.UserName = model.UserName;
			user.Email = model.Email;
			user.PhoneNumber = model.PhoneNumber;

			IdentityResult result;

			if (!string.IsNullOrEmpty(model.NewPassword))
			{
				var token = await _userManager.GeneratePasswordResetTokenAsync(user);
				var resetResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
				if (!resetResult.Succeeded)
				{
					foreach (var error in resetResult.Errors)
					{
						ModelState.AddModelError("", error.Description);
					}
					return View(model);
				}
			}

			result = await _userManager.UpdateAsync(user);

			if (!result.Succeeded)
			{
				foreach (var error in result.Errors)
				{
					ModelState.AddModelError("", error.Description);
				}
				return View(model);
			}

			TempData["success"] = "Cập nhật người dùng thành công!";
			return RedirectToAction("Index");
		}



		[HttpPost]
		public async Task<IActionResult> Delete(string Id)
		{
			var currentUser = _userManager.GetUserId(User);
			if (Id == currentUser)
			{
				TempData["error"] = "Không thể xóa chính bạn!";
				return RedirectToAction("Index");
			}
			var user = await _userManager.FindByIdAsync(Id);
			if (user == null)
			{
				return NotFound();
			}
			var DeleteResult = await _userManager.DeleteAsync(user);
			if (DeleteResult.Succeeded)
			{
				TempData["success"] = "Xóa người dùng thành công!";
				return RedirectToAction("Index");
			}
			else
			{
				return View("error");
			}
		}
	}
}
