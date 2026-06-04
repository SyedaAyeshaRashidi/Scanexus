using LibrarySystem.Models;
using LibrarySystem.Services;
using Microsoft.AspNetCore.Mvc;
using LibrarySystem.Data;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Controllers
{
    public class StudentController : Controller
    {
        private readonly ILibraryService _service;
        private readonly LibraryDbContext _db;
        public StudentController(ILibraryService service, LibraryDbContext db)
        {
            _service = service;
            _db = db;
        }

        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var student = await _db.Students.FirstOrDefaultAsync(
                s => s.UniversityID == model.UniversityID && s.PasswordHash == model.Password);

            if (student == null)
            {
                TempData["Error"] = "Invalid University ID or password.";
                return View(model);
            }
            if (!student.IsActive)
            {
                TempData["Error"] = "Your account is inactive. Contact the library.";
                return View(model);
            }

            HttpContext.Session.SetString("UniversityID", student.UniversityID);
            HttpContext.Session.SetString("StudentName", student.FullName);
            HttpContext.Session.SetString("Role", student.Role ?? "Student");

            // Redirect admin to admin panel
            if (student.Role == "Admin")
                return RedirectToAction("Dashboard", "Admin");

            return RedirectToAction("Dashboard", "Student");
        }
        public async Task<IActionResult> Dashboard()
        {
            var uid = HttpContext.Session.GetString("UniversityID");
            if (string.IsNullOrEmpty(uid)) return RedirectToAction("Login");
            var vm = await _service.GetDashboardAsync(uid);
            if (vm == null) return RedirectToAction("Login");
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(string txnCode)
        {
            var uid = HttpContext.Session.GetString("UniversityID");
            if (string.IsNullOrEmpty(uid)) return RedirectToAction("Login");
            var (success, message) = await _service.ReturnBookAsync(txnCode);
            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction("Dashboard");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
