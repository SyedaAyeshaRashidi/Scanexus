using LibrarySystem.Data;
using LibrarySystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace LibrarySystem.Services
{
    public interface ILibraryService
    {
        Task<(bool Success, string Message, Student? Student)> AuthenticateAsync(string universityId, string password);
        Task<(bool Success, string Message, Transaction? Txn)> IssueBookAsync(string universityId, string qrCode);
        Task<(bool Success, string Message)> ReturnBookAsync(string txnCode);
        Task<DashboardViewModel?> GetDashboardAsync(string universityId);
        Task<List<Book>> GetAllBooksAsync();
        Task<Book?> GetBookByQRAsync(string qrCode);
        Task<List<Transaction>> GetAllTransactionsAsync();
        Task<string> GenerateQRCodeBase64Async(string content);
    }

    public class LibraryService : ILibraryService
    {
        private readonly LibraryDbContext _db;

        public LibraryService(LibraryDbContext db) => _db = db;

        // ──────────────────────────────────────────────
        // Authentication
        // ──────────────────────────────────────────────
        public async Task<(bool, string, Student?)> AuthenticateAsync(string universityId, string password)
        {
            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.UniversityID == universityId && s.PasswordHash == password);

            if (student == null)
                return (false, "Invalid University ID or password.", null);

            if (!student.IsActive)
                return (false, "Your account is inactive. Contact the library.", null);

            return (true, "Login successful.", student);
        }

        // ──────────────────────────────────────────────
        // Issue Book (core logic with all validations)
        // ──────────────────────────────────────────────
        public async Task<(bool, string, Transaction?)> IssueBookAsync(string universityId, string qrCode)
        {
            // 1. Verify student
            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.UniversityID == universityId);

            if (student == null) return (false, "Student not found.", null);
            if (!student.IsActive) return (false, "Inactive students cannot borrow books.", null);

            // 2. Verify book via QR
            var book = await _db.Books.FirstOrDefaultAsync(b => b.QRCode == qrCode);
            if (book == null) return (false, "Invalid QR code. Book not found.", null);

            // 3. Already issued to this student?
            var alreadyIssued = await _db.Transactions
                .AnyAsync(t => t.StudentID == student.StudentID
                            && t.BookID   == book.BookID
                            && t.Status   == "Active");
            if (alreadyIssued) return (false, "You already have this book issued.", null);

            // 4. Availability check
            if (book.AvailableCopies <= 0)
                return (false, "All copies of this book are currently issued.", null);

            // 5. Borrowing limit (max 3)
            var activeCount = await _db.Transactions
                .CountAsync(t => t.StudentID == student.StudentID && t.Status == "Active");
            if (activeCount >= 3)
                return (false, "Borrowing limit reached. Return a book before issuing another.", null);

            // 6. Create transaction
            using var txn = await _db.Database.BeginTransactionAsync();
            try
            {
                var newTxn = new Transaction
                {
                    TxnCode    = GenerateTxnCode(),
                    StudentID  = student.StudentID,
                    BookID     = book.BookID,
                    IssueDate  = DateTime.Now,
                    DueDate    = DateTime.Now.AddDays(14),
                    Status     = "Active",
                    QRScanData = qrCode
                };

                _db.Transactions.Add(newTxn);
                book.AvailableCopies--;

                await _db.SaveChangesAsync();
                await txn.CommitAsync();

                // Load navigation props for response
                newTxn.Student = student;
                newTxn.Book    = book;

                return (true, "Book issued successfully! Due in 14 days.", newTxn);
            }
            catch (Exception ex)
            {
                await txn.RollbackAsync();
                return (false, $"An error occurred: {ex.Message}", null);
            }
        }

        // ──────────────────────────────────────────────
        // Return Book
        // ──────────────────────────────────────────────
        public async Task<(bool, string)> ReturnBookAsync(string txnCode)
        {
            var txn = await _db.Transactions
                .Include(t => t.Book)
                .FirstOrDefaultAsync(t => t.TxnCode == txnCode);

            if (txn == null) return (false, "Transaction not found.");
            if (txn.Status == "Returned") return (false, "Book already returned.");

            txn.ReturnDate = DateTime.Now;
            txn.Status     = "Returned";
            txn.Book!.AvailableCopies++;

            await _db.SaveChangesAsync();
            return (true, "Book returned successfully.");
        }

        // ──────────────────────────────────────────────
        // Student Dashboard
        // ──────────────────────────────────────────────
        public async Task<DashboardViewModel?> GetDashboardAsync(string universityId)
        {
            var student = await _db.Students.FirstOrDefaultAsync(s => s.UniversityID == universityId);
            if (student == null) return null;

            var allTxns = await _db.Transactions
                .Include(t => t.Book)
                .Where(t => t.StudentID == student.StudentID)
                .OrderByDescending(t => t.IssueDate)
                .ToListAsync();

            // Auto-mark overdue
            foreach (var t in allTxns.Where(t => t.Status == "Active" && t.DueDate < DateTime.Now))
                t.Status = "Overdue";
            await _db.SaveChangesAsync();

            return new DashboardViewModel
            {
                Student     = student,
                ActiveBooks = allTxns.Where(t => t.Status is "Active" or "Overdue").ToList(),
                History     = allTxns.Where(t => t.Status == "Returned").ToList()
            };
        }

        public async Task<List<Book>> GetAllBooksAsync() =>
            await _db.Books.OrderBy(b => b.Title).ToListAsync();

        public async Task<Book?> GetBookByQRAsync(string qrCode) =>
            await _db.Books.FirstOrDefaultAsync(b => b.QRCode == qrCode);

        public async Task<List<Transaction>> GetAllTransactionsAsync() =>
            await _db.Transactions
                .Include(t => t.Student)
                .Include(t => t.Book)
                .OrderByDescending(t => t.IssueDate)
                .ToListAsync();

        // ──────────────────────────────────────────────
        // QR Code generation (returns base64 PNG)
        // ──────────────────────────────────────────────
        public Task<string> GenerateQRCodeBase64Async(string content)
        {
            // Using QRCoder library
            // In production this would use QRCodeGenerator from QRCoder package
            // Returning placeholder base64 for structure; wire up QRCoder NuGet
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"QR:{content}"));
            return Task.FromResult(base64);
        }

        // ──────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────
        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }

        private static string GenerateTxnCode()
        {
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var rand     = new Random().Next(100000, 999999);
            return $"TXN-{datePart}-{rand}";
        }
    }
}
