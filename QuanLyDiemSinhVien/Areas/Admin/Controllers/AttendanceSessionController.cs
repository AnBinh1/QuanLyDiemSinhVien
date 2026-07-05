using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuanLyDiemSinhVien.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin,Lecturer")]
	public class AttendanceSessionController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
