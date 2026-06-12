using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibrarySystem.Models
{
    public class Student
    {
        [Key]
        public int StudentID { get; set; }

        [Required, StringLength(20)]
        [Display(Name = "University ID")]
        public string UniversityID { get; set; } = string.Empty;

        [Required, StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        [Display(Name = "Father Name")]
        public string FatherName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(256)]
        public string PasswordHash { get; set; } = string.Empty;

        [Range(1, 8)]
        public int Semester { get; set; }

        [StringLength(20)]
        public string Batch { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;


        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }

    public class Book
    {
        [Key]
        public int BookID { get; set; }

        [Required, StringLength(20)]
        public string ISBN { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string Author { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Publisher { get; set; }

        public int TotalCopies { get; set; } = 1;
        public int AvailableCopies { get; set; } = 1;

        [Required, StringLength(500)]
        public string QRCode { get; set; } = string.Empty;

        public DateTime AddedAt { get; set; } = DateTime.Now;

        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }

    public class Transaction
    {
        [Key]
        public int TransactionID { get; set; }

        [Required, StringLength(50)]
        public string TxnCode { get; set; } = string.Empty;

        [ForeignKey("Student")]
        public int StudentID { get; set; }
        public Student? Student { get; set; }

        [ForeignKey("Book")]
        public int BookID { get; set; }
        public Book? Book { get; set; }

        public DateTime IssueDate { get; set; } = DateTime.Now;
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        [Required, StringLength(500)]
        public string QRScanData { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Remarks { get; set; }

        [NotMapped]
        public bool IsOverdue => Status == "Active" && DueDate < DateTime.Now;
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "University ID is required")]
        [Display(Name = "University ID")]
        public string UniversityID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public class DashboardViewModel
    {
        public Student Student { get; set; } = new Student();
        public List<Transaction> ActiveBooks { get; set; } = new();
        public List<Transaction> History { get; set; } = new();
        public int BooksRemaining => 3 - ActiveBooks.Count;
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    public class IssueBookRequest
    {
        [Required] public string UniversityID { get; set; } = string.Empty;
        [Required] public string QRCode { get; set; } = string.Empty;
    }

    public class ReturnBookRequest
    {
        [Required] public string TxnCode { get; set; } = string.Empty;
    }
    public class QRScanViewModel
    {
        [Required]
        public string QRCode { get; set; } = string.Empty;
    }
}
