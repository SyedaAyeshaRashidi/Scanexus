using LibrarySystem.Services;
using LibrarySystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Controllers
{
    public class BooksController : Controller
    {
        private readonly ILibraryService _service;
        public BooksController(ILibraryService service) => _service = service;

        // GET: /Books/Index
        public async Task<IActionResult> Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UniversityID")))
                return RedirectToAction("Login", "Student");

            var books = await _service.GetAllBooksAsync();
            return View(books);
        }

        // GET: /Books/Borrow?qr=SSUET-LIB-BOOK-001
        // Yeh page tab khulta hai jab phone se QR scan karo
        public async Task<IActionResult> Borrow(string qr)
        {
            var book = await _service.GetBookByQRAsync(qr);
            if (book == null) return NotFound("Book not found.");

            ViewBag.BookTitle  = book.Title;
            ViewBag.BookAuthor = book.Author;
            ViewBag.QRCode     = book.QRCode;
            ViewBag.Available  = book.AvailableCopies > 0;
            return View();
        }

        // POST: /Books/Issue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Issue(string qrCode)
        {
            var uid = HttpContext.Session.GetString("UniversityID");
            if (string.IsNullOrEmpty(uid))
                return RedirectToAction("Login", "Student");

            var (success, message, txn) = await _service.IssueBookAsync(uid, qrCode);

            if (success)
            {
                TempData["Receipt"] = System.Text.Json.JsonSerializer.Serialize(new {
                    txn!.TxnCode,
                    BookTitle   = txn.Book?.Title,
                    BookAuthor  = txn.Book?.Author,
                    StudentName = txn.Student?.FullName,
                    UniversityID = uid,
                    IssueDate   = txn.IssueDate.ToString("dd MMM yyyy, hh:mm tt"),
                    DueDate     = txn.DueDate.ToString("dd MMM yyyy")
                });
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction("Index");
        }
    }
}
