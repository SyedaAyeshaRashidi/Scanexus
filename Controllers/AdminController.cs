using LibrarySystem.Data;
using LibrarySystem.Models;
using LibrarySystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ILibraryService _service;
        private readonly LibraryDbContext _db;

        public AdminController(ILibraryService service, LibraryDbContext db)
        {
            _service = service;
            _db = db;
        }

        private bool IsAdmin() => HttpContext.Session.GetString("Role") == "Admin";

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Student");

            ViewBag.TotalBooks = await _db.Books.CountAsync();
            //ViewBag.TotalStudents = await _db.Students.Where(s => s.Role == "Student").CountAsync();
            ViewBag.ActiveIssues = await _db.Transactions.CountAsync(t => t.Status == "Active");
            ViewBag.OverdueIssues = await _db.Transactions.CountAsync(t => t.Status == "Overdue");
            ViewBag.TotalTransactions = await _db.Transactions.CountAsync();

            var recentTxns = await _db.Transactions
                .Include(t => t.Student)
                .Include(t => t.Book)
                .OrderByDescending(t => t.IssueDate)
                .Take(8)
                .ToListAsync();

            return View(recentTxns);
        }

        // GET: /Admin/Books
        public async Task<IActionResult> Books()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Student");
            var books = await _db.Books.OrderBy(b => b.Title).ToListAsync();
            return View("AdminBooks", books);
        }

        // GET: /Admin/AddBook
        public IActionResult AddBook()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Student");
            return View("AdminAddBook");
        }

        // POST: /Admin/AddBook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBook(Book book)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Student");

            // Duplicate check — Title + Author se match karo
            var existing = await _db.Books.FirstOrDefaultAsync(b =>
                b.Title.ToLower() == book.Title.ToLower() &&
                b.Author.ToLower() == book.Author.ToLower());

            if (existing != null)
            {
                existing.TotalCopies += book.TotalCopies;
                existing.AvailableCopies += book.TotalCopies;
                await _db.SaveChangesAsync();
                TempData["Success"] = $"'{existing.Title}' already exists — {book.TotalCopies} copies added!";
                return RedirectToAction("Books");
            }

            // Sequential QR + ISBN generate karo
            var lastBook = await _db.Books
                .OrderByDescending(b => b.BookID)
                .FirstOrDefaultAsync();

            int nextNum = (lastBook != null) ? lastBook.BookID + 1 : 1;
            book.QRCode = "SSUET-LIB-BOOK-" + nextNum.ToString("D3");
            book.ISBN = $"978-0-13-{nextNum:D4}";
            book.AvailableCopies = book.TotalCopies;
            book.AddedAt = DateTime.Now;

            _db.Books.Add(book);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Book '{book.Title}' added successfully!";
            return RedirectToAction("Books");
        }

        // GET: /Admin/LastBookISBN — Auto ISBN for JS
        public async Task<IActionResult> LastBookISBN()
        {
            var lastBook = await _db.Books
                .OrderByDescending(b => b.BookID)
                .FirstOrDefaultAsync();

            int nextNum = (lastBook != null) ? lastBook.BookID + 1 : 1;
            string isbn = $"978-0-13-{nextNum:D4}";
            string qr = "SSUET-LIB-BOOK-" + nextNum.ToString("D3");
            return Json(new { isbn, qr });
        }

        // POST: /Admin/DeleteBook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBook(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Student");

            var hasActive = await _db.Transactions.AnyAsync(t => t.BookID == id && t.Status == "Active");
            if (hasActive)
            {
                TempData["Error"] = "Cannot delete — this book has active issues.";
                return RedirectToAction("Books");
            }

            var relatedTxns = await _db.Transactions.Where(t => t.BookID == id).ToListAsync();
            if (relatedTxns.Any())
                _db.Transactions.RemoveRange(relatedTxns);

            var book = await _db.Books.FindAsync(id);
            if (book != null)
                _db.Books.Remove(book);

            await _db.SaveChangesAsync();
            TempData["Success"] = "Book deleted successfully.";
            return RedirectToAction("Books");
        }

        // GET: /Admin/Students
        public async Task<IActionResult> Students()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Student");
            var students = await _db.Students
                //.Where(s => s.Role == "Student")
                .OrderBy(s => s.FullName)
                .ToListAsync();
            return View("AdminStudents", students);
        }

        // GET: /Admin/AddStudent
        public IActionResult AddStudent()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Student");
            return View("AdminAddStudent");
        }

        // POST: /Admin/AddStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudent(Student student)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Student");

            var existing = await _db.Students.FirstOrDefaultAsync(s =>
                s.UniversityID == student.UniversityID);

            if (existing != null)
            {
                TempData["Error"] = $"Student '{student.UniversityID}' already exists!";
                return View("AdminAddStudent", student);
            }

            // Password hashing — agar service mein hash hota hai toh woh use karo
            // Warna plain text store karo same jaise login karta hai
            //student.Role = "Student";
            student.IsActive = true;
            student.CreatedAt = DateTime.Now;

            _db.Students.Add(student);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Student '{student.FullName}' added successfully!";
            return RedirectToAction("Students");
        }

        // GET: /Admin/Transactions
        public async Task<IActionResult> Transactions()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Student");
            var txns = await _db.Transactions
                .Include(t => t.Student)
                .Include(t => t.Book)
                .OrderByDescending(t => t.IssueDate)
                .ToListAsync();
            return View("AdminTransactions", txns);
        }

        // POST: /Admin/ToggleStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStudent(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Student");
            var student = await _db.Students.FindAsync(id);
            if (student != null)
            {
                student.IsActive = !student.IsActive;
                await _db.SaveChangesAsync();
                TempData["Success"] = $"{student.FullName} is now {(student.IsActive ? "Active" : "Inactive")}.";
            }
            return RedirectToAction("Students");
        }
    }
}