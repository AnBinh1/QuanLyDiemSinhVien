
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuanLyDiemSinhVien.Areas.Student.Controllers
{
    [Area("Student")]
    [Authorize(Roles = "Student")]
    public class TrainingPointController : Controller
    {
        // Trang điểm rèn luyện của sinh viên
        public IActionResult Index()
        {
            return View();
        }
    }
}
