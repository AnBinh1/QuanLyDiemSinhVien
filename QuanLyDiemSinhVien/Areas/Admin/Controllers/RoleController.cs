using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuanLyDiemSinhVien.Models.ViewModel;
using QuanLyDiemSinhVien.Repository;

namespace QuanLyDiemSinhVien.Areas.Admin.Controllers
{
	[Authorize(Roles = "Admin")]
	[Area("Admin")]
	public class RoleController : Controller
	{
		public readonly RoleManager<IdentityRole> _roleManager;
		public readonly DataContext _context;
		public RoleController(RoleManager<IdentityRole> roleManager, DataContext context)
		{
			_roleManager = roleManager;
			_context = context;
		}
		public IActionResult Index()
		{
			var roles = _roleManager.Roles.ToList();
			return View(roles);
		}
		[HttpGet]
		public IActionResult Create()
		{
			return View();
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(string Name)
		{
			if (!string.IsNullOrWhiteSpace(Name))
			{
				var result = await _roleManager.CreateAsync(new IdentityRole(Name));
				if(result.Succeeded)
				{
					TempData["success"] = "Tạo vai trò thành công!";
					return RedirectToAction("Index");
				}
				else
				{
					foreach (var error in result.Errors)
					{
						ModelState.AddModelError("", error.Description);
					}
				}
			}
			return View();
		}
		[HttpGet]
		public async Task<IActionResult> Edit(string Id)
		{
			
			var role = await _roleManager.FindByIdAsync(Id);
			return View(role);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(string id,string name)
		{
			var role = await _roleManager.FindByIdAsync(id);
			if (role != null)
			{
				role.Name = name;
				var result = await _roleManager.UpdateAsync(role);
				if (result.Succeeded)
					return RedirectToAction("Index");
			}
			return View(role);
		}


		[HttpPost]
		public async Task<IActionResult> Delete(string id)
		{
			var role = await _roleManager.FindByIdAsync(id);
			if(role != null)
			{
				TempData["success"] = "Xoá vai trò thành công!";
				await _roleManager.DeleteAsync(role);
			}
			return RedirectToAction("Index");
		}
	}
}
