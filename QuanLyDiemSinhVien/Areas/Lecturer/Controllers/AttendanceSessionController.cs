using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuanLyDiemSinhVien.Areas.Lecturer.Controllers
{
	[Area("Lecturer")]
	[Authorize(Roles = "Lecturer")]
	public class AttendanceSessionController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
