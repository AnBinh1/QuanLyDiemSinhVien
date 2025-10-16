using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuanLyDiemSinhVien.Areas.Admin.Controllers
{
	[Authorize(Roles = "Admin,Teacher")]
	[Area("Admin")]
	public class HomeController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
