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

            var hashedPassword = LibraryService.HashPassword(model.Password);

            // 1. Admin Login Check (Yeh pehle ki tarah database se direct check hoga)
            var admin = await _db.Admins.FirstOrDefaultAsync(
                a => a.AdminID == model.UniversityID && a.PasswordHash == hashedPassword);

            if (admin != null)
            {
                HttpContext.Session.SetString("UniversityID", admin.AdminID);
                HttpContext.Session.SetString("StudentName", "Library Admin");
                HttpContext.Session.SetString("Role", "Admin");

                // Admin ke liye direct service ka log call kar dete hain
                await _service.LogActivityAsync("LOGIN", admin.AdminID, "Library Admin", "Admin logged in successfully.");

                return RedirectToAction("Dashboard", "Admin");
            }

            // 2. Student Login Check (Ab yeh hamari updated service se call hoga)
            // Is ek line ke andar automatic valid/invalid/inactive har tarah ka log khud lag jayega!
            var (success, message, student) = await _service.AuthenticateAsync(model.UniversityID, model.Password);

            if (!success)
            {
                TempData["Error"] = message; // "Invalid credentials" ya "Inactive account" ka message khud aa jayega
                return View(model);
            }

            // 3. Student Login Success (Sessions set karenge aur dashboard par bhej denge)
            HttpContext.Session.SetString("UniversityID", student!.UniversityID);
            HttpContext.Session.SetString("StudentName", student.FullName);
            HttpContext.Session.SetString("Role", "Student");

            return RedirectToAction("Dashboard");
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