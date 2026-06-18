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
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Admin check
            var admin = await _db.Admins.FirstOrDefaultAsync(
                a => a.AdminID == model.UniversityID && a.PasswordHash == hashedPassword);

            if (admin != null)
            {
                HttpContext.Session.SetString("UniversityID", admin.AdminID);
                HttpContext.Session.SetString("StudentName", "Library Admin");
                HttpContext.Session.SetString("Role", "Admin");

                await _service.LogActivityAsync("LOGIN", admin.AdminID, "Library Admin", "Admin login successful", ipAddress);

                return RedirectToAction("Dashboard", "Admin");
            }

            // Student check
            var student = await _db.Students.FirstOrDefaultAsync(
                s => s.UniversityID == model.UniversityID && s.PasswordHash == hashedPassword);

            if (student == null)
            {
                await _service.LogActivityAsync("FAILED_ATTEMPT", model.UniversityID, null, "Login failed: Invalid credentials", ipAddress);
                TempData["Error"] = "Invalid University ID or password.";
                return View(model);
            }

            if (!student.IsActive)
            {
                await _service.LogActivityAsync("FAILED_ATTEMPT", model.UniversityID, student.FullName, "Login failed: Inactive account", ipAddress);
                TempData["Error"] = "Your account is inactive. Contact the library.";
                return View(model);
            }

            HttpContext.Session.SetString("UniversityID", student.UniversityID);
            HttpContext.Session.SetString("StudentName", student.FullName);
            HttpContext.Session.SetString("Role", "Student");

            await _service.LogActivityAsync("LOGIN", student.UniversityID, student.FullName, "Student login successful", ipAddress);

            return RedirectToAction("Dashboard");
        }

        public async Task<IActionResult> Dashboard()
        {
            var uid = HttpContext.Session.GetString("UniversityID");
            if (string.IsNullOrEmpty(uid)) return RedirectToAction("Login");

            var vm = await _service.GetDashboardAsync(uid);
            if (vm == null) return RedirectToAction("Login");

            // 🎯 FORCE OVERDUE TRIGGER: Directly checking the IsOverdue property from your business logic
            var overdueBooksList = vm.ActiveBooks != null
                ? vm.ActiveBooks.Where(t => t.IsOverdue).ToList()
                : new List<LibrarySystem.Models.Transaction>(); // Match with your transaction model namespace

            ViewBag.OverdueNotifications = overdueBooksList.Select(t => new {
                Message = "Book '" + (t.Book != null ? t.Book.Title : "Library Book") + "' is Overdue! It was due on " + t.DueDate.ToString("dd MMM yyyy") + ". Please return it to stop fine accumulation."
            }).ToList();

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