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
        Task<(bool Success, string Message)> ReturnBookByQRAsync(string returnQrCode, string universityId);
        Task<(Book? Book, Transaction? ActiveTxn)> GetBookWithTxnAsync(string qrCode, string universityId);
        Task<(bool Success, string Message)> UpdateDueDateAsync(int transactionId, DateTime newDueDate); 
        Task<DashboardViewModel?> GetDashboardAsync(string universityId);
        Task<List<Book>> GetAllBooksAsync();
        Task<Book?> GetBookByQRAsync(string qrCode);
        Task<List<Transaction>> GetAllTransactionsAsync();
        Task<string> GenerateQRCodeBase64Async(string content);
        Task<int> GetActiveBorrowersCountAsync();
        Task<List<Transaction>> GetTodayTransactionsAsync();
        Task<List<Transaction>> GetDefaultersAsync();
        Task<(decimal TotalCalculated, decimal TotalPaid, decimal TotalOutstanding)> GetFineSummaryAsync();
        Task<List<Transaction>> GetFineDetailsAsync();
        Task<List<(string Title, string Author, int Count)>> GetMostBorrowedBooksAsync();
        Task<List<Transaction>> GetCirculationLogAsync(DateTime? from, DateTime? to);
        Task<(int TotalCopies, int AvailableCopies, int IssuedCopies)> GetInventoryStatusAsync();
    }

    public class LibraryService : ILibraryService
    {
        private readonly LibraryDbContext _db;

        public LibraryService(LibraryDbContext db) => _db = db;

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

        public async Task<(bool, string, Transaction?)> IssueBookAsync(string universityId, string qrCode)
        {
            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.UniversityID == universityId);

            if (student == null) return (false, "Student not found.", null);
            if (!student.IsActive) return (false, "Inactive students cannot borrow books.", null);

            var book = await _db.Books.FirstOrDefaultAsync(b => b.QRCode == qrCode);
            if (book == null) return (false, "Invalid QR code. Book not found.", null);

            var alreadyIssued = await _db.Transactions
                .AnyAsync(t => t.StudentID == student.StudentID
                            && t.BookID == book.BookID
                            && t.Status == "Active");
            if (alreadyIssued) return (false, "You already have this book issued.", null);

            if (book.AvailableCopies <= 0)
                return (false, "All copies of this book are currently issued.", null);

            var activeCount = await _db.Transactions
                .CountAsync(t => t.StudentID == student.StudentID && t.Status == "Active");
            if (activeCount >= 3)
                return (false, "Borrowing limit reached. Return a book before issuing another.", null);

            using var txn = await _db.Database.BeginTransactionAsync();
            try
            {
                var newTxn = new Transaction
                {
                    TxnCode = GenerateTxnCode(),
                    StudentID = student.StudentID,
                    BookID = book.BookID,
                    IssueDate = DateTime.Now,
                    DueDate = DateTime.Now.AddDays(14),
                    Status = "Active",
                    QRScanData = qrCode
                };

                _db.Transactions.Add(newTxn);
                book.AvailableCopies--;

                await _db.SaveChangesAsync();
                await txn.CommitAsync();

                newTxn.Student = student;
                newTxn.Book = book;

                return (true, "Book issued successfully! Due in 14 days.", newTxn);
            }
            catch (Exception ex)
            {
                await txn.RollbackAsync();
                return (false, $"An error occurred: {ex.Message}", null);
            }
        }

        public async Task<(bool, string)> ReturnBookAsync(string txnCode)
        {
            var txn = await _db.Transactions
                .Include(t => t.Book)
                .FirstOrDefaultAsync(t => t.TxnCode == txnCode);

            if (txn == null) return (false, "Transaction not found.");
            if (txn.Status == "Returned") return (false, "Book already returned.");

            txn.ReturnDate = DateTime.Now;

            if (txn.DueDate < DateTime.Now)
            {
                int overdueDays = (DateTime.Now - txn.DueDate).Days;
                txn.FineAmount = overdueDays * 20; 
                txn.Status = "Returned";
                txn.Book!.AvailableCopies++;
                await _db.SaveChangesAsync();

                return (true, $"Book returned successfully. Overdue by {overdueDays} day(s) — Fine: Rs. {txn.FineAmount}");
            }

            txn.Status = "Returned";
            txn.Book!.AvailableCopies++;
            await _db.SaveChangesAsync();

            return (true, "Book returned successfully.");
        }

        public async Task<(bool, string)> ReturnBookByQRAsync(string returnQrCode, string universityId)
        {
            if (string.IsNullOrEmpty(returnQrCode) || !returnQrCode.StartsWith("RETURN-"))
                return (false, "Invalid return QR code.");

            var txnCode = returnQrCode.Substring("RETURN-".Length);

            var txn = await _db.Transactions
                .Include(t => t.Book)
                .Include(t => t.Student)
                .FirstOrDefaultAsync(t => t.TxnCode == txnCode);

            if (txn == null) return (false, "Transaction not found.");
            if (txn.Status == "Returned") return (false, "Book already returned.");
            if (txn.Student!.UniversityID != universityId) return (false, "This isn't your book to return.");

            txn.ReturnDate = DateTime.Now;

            if (txn.DueDate < DateTime.Now)
            {
                int overdueDays = (DateTime.Now - txn.DueDate).Days;
                txn.FineAmount = overdueDays * 20;
                txn.Status = "Returned";
                txn.Book!.AvailableCopies++;
                await _db.SaveChangesAsync();
                return (true, $"Book returned! Overdue by {overdueDays} day(s) — Fine: Rs. {txn.FineAmount}");
            }

            txn.Status = "Returned";
            txn.Book!.AvailableCopies++;
            await _db.SaveChangesAsync();
            return (true, "Book returned successfully.");
        }

        public async Task<(Book? Book, Transaction? ActiveTxn)> GetBookWithTxnAsync(string qrCode, string universityId)
        {
            var book = await _db.Books.FirstOrDefaultAsync(b => b.QRCode == qrCode);
            if (book == null) return (null, null);

            if (string.IsNullOrEmpty(universityId))
                return (book, null);

            var student = await _db.Students.FirstOrDefaultAsync(s => s.UniversityID == universityId);
            if (student == null) return (book, null);

            var activeTxn = await _db.Transactions
                .FirstOrDefaultAsync(t => t.BookID == book.BookID
                                        && t.StudentID == student.StudentID
                                        && (t.Status == "Active" || t.Status == "Overdue"));

            return (book, activeTxn);
        }

        public async Task<DashboardViewModel?> GetDashboardAsync(string universityId)
        {
            var student = await _db.Students.FirstOrDefaultAsync(s => s.UniversityID == universityId);
            if (student == null) return null;

            var allTxns = await _db.Transactions
                .Include(t => t.Book)
                .Where(t => t.StudentID == student.StudentID)
                .OrderByDescending(t => t.IssueDate)
                .ToListAsync();

            foreach (var t in allTxns.Where(t => t.Status == "Active" && t.DueDate < DateTime.Now))
                t.Status = "Overdue";
            await _db.SaveChangesAsync();

            return new DashboardViewModel
            {
                Student = student,
                ActiveBooks = allTxns.Where(t => t.Status is "Active" or "Overdue").ToList(),
                History = allTxns.Where(t => t.Status == "Returned").ToList()
            };
        }

        public async Task<int> GetActiveBorrowersCountAsync() =>
            await _db.Transactions
                .Where(t => t.Status == "Active" || t.Status == "Overdue")
                .Select(t => t.StudentID)
                .Distinct()
                .CountAsync();

        public async Task<List<Transaction>> GetTodayTransactionsAsync()
        {
            var today = DateTime.Today;
            return await _db.Transactions
                .Include(t => t.Student)
                .Include(t => t.Book)
                .Where(t => t.IssueDate.Date == today || (t.ReturnDate.HasValue && t.ReturnDate.Value.Date == today))
                .OrderByDescending(t => t.IssueDate)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetDefaultersAsync() =>
            await _db.Transactions
                .Include(t => t.Student)
                .Include(t => t.Book)
                .Where(t => (t.Status == "Overdue" || (t.Status == "Active" && t.DueDate < DateTime.Now))
                            || (t.FineAmount > 0 && !t.FinePaid))
                .OrderByDescending(t => t.DueDate)
                .ToListAsync();


        public async Task<(decimal TotalCalculated, decimal TotalPaid, decimal TotalOutstanding)> GetFineSummaryAsync()
        {
            var fineTxns = await _db.Transactions.Where(t => t.FineAmount > 0).ToListAsync();
            var total = fineTxns.Sum(t => t.FineAmount);
            var paid = fineTxns.Where(t => t.FinePaid).Sum(t => t.FineAmount);
            var outstanding = total - paid;
            return (total, paid, outstanding);
        }

        public async Task<List<Transaction>> GetFineDetailsAsync() =>
            await _db.Transactions
                .Include(t => t.Student)
                .Include(t => t.Book)
                .Where(t => t.FineAmount > 0)
                .OrderByDescending(t => t.IssueDate)
                .ToListAsync();

        public async Task<List<(string Title, string Author, int Count)>> GetMostBorrowedBooksAsync()
        {
            return await _db.Transactions
                .GroupBy(t => new { t.Book!.Title, t.Book.Author })
                .Select(g => new { g.Key.Title, g.Key.Author, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(10)
                .Select(g => ValueTuple.Create(g.Title, g.Author, g.Count))
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetCirculationLogAsync(DateTime? from, DateTime? to)
        {
            var query = _db.Transactions
                .Include(t => t.Student)
                .Include(t => t.Book)
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(t => t.IssueDate.Date >= from.Value.Date);

            if (to.HasValue)
                query = query.Where(t => t.IssueDate.Date <= to.Value.Date);

            return await query.OrderByDescending(t => t.IssueDate).ToListAsync();
        }

        public async Task<(int TotalCopies, int AvailableCopies, int IssuedCopies)> GetInventoryStatusAsync()
        {
            var books = await _db.Books.ToListAsync();
            var total = books.Sum(b => b.TotalCopies);
            var available = books.Sum(b => b.AvailableCopies);
            return (total, available, total - available);
        }
        public async Task<(bool, string)> UpdateDueDateAsync(int transactionId, DateTime newDueDate)
        {
            var txn = await _db.Transactions.FindAsync(transactionId);
            if (txn == null) return (false, "Transaction not found.");
            if (txn.Status == "Returned") return (false, "Cannot edit due date of a returned book.");

            txn.DueDate = newDueDate;

            if (newDueDate >= DateTime.Now && txn.Status == "Overdue")
                txn.Status = "Active";
            else if (newDueDate < DateTime.Now && txn.Status == "Active")
                txn.Status = "Overdue";

            await _db.SaveChangesAsync();
            return (true, "Due date updated successfully.");
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


        public Task<string> GenerateQRCodeBase64Async(string content)
        {
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"QR:{content}"));
            return Task.FromResult(base64);
        }

 
        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }

        private static string GenerateTxnCode()
        {
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var rand = new Random().Next(100000, 999999);
            return $"TXN-{datePart}-{rand}";
        }

    }
}