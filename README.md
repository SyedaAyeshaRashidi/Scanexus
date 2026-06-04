# QR-Based Digital Library System
**Sir Syed University of Engineering & Technology**  
**CS & IT Department | Spring 2026 | Database Systems (CS-229T)**  
**Assignment #1 — PBL**

---

## Project Overview

A smart library system where students can request and receive books by scanning a **QR code**. Built with:

- **Backend**: ASP.NET Core 8 MVC + REST API
- **Database**: SQL Server (SSMS)
- **ORM**: Entity Framework Core
- **API Docs**: Swagger/OpenAPI

---

## ER Diagram

```
┌─────────────────────┐         ┌──────────────────────┐
│      STUDENTS        │         │        BOOKS          │
├─────────────────────┤         ├──────────────────────┤
│ PK  StudentID (INT) │         │ PK  BookID (INT)      │
│     UniversityID    │         │     ISBN (UNIQUE)     │
│     FullName        │         │     Title             │
│     FatherName      │         │     Author            │
│     Email (UNIQUE)  │         │     Publisher         │
│     PasswordHash    │         │     TotalCopies       │
│     Semester        │         │     AvailableCopies   │
│     Batch           │         │     QRCode (UNIQUE)   │
│     IsActive (BIT)  │         │     AddedAt           │
│     CreatedAt       │         └──────────┬───────────┘
└──────────┬──────────┘                    │
           │  1                            │  1
           │                               │
           │          TRANSACTIONS         │
           │    ┌──────────────────────┐   │
           │    │ PK  TransactionID    │   │
           └────┤ FK  StudentID        │   │
                │ FK  BookID           ├───┘
                │     TxnCode (UNIQUE) │
                │     IssueDate        │
                │     DueDate          │
                │     ReturnDate       │
                │     Status           │
                │     QRScanData       │
                │     Remarks          │
                └──────────────────────┘
```

**Relationships:**
- One Student → Many Transactions
- One Book → Many Transactions
- Transactions are NEVER deleted (permanent history)
- Active student constraint enforced at DB + application level

---

## Business Rules

| Rule | Implementation |
|------|---------------|
| Max 3 books per student | Checked in `sp_IssueBook` and `LibraryService.IssueBookAsync()` |
| Same book not issued to 2 students simultaneously | `AvailableCopies` decremented atomically; checked before issue |
| Only active students can borrow | `IsActive = 1` check in both SP and service |
| Old transactions never deleted | No DELETE on Transactions; only Status updates |
| Each scan generates unique Transaction ID | `TxnCode = "TXN-{date}-{sequence}"` |
| Due date = 14 days from issue | Calculated on insert |

---

## Project Structure

```
LibrarySystem/
├── Controllers/
│   ├── LibraryController.cs    ← REST API (8 endpoints)
│   ├── StudentController.cs    ← Login, Dashboard, Scan, Return
│   └── BooksController.cs      ← Book catalog + QR display
├── Models/
│   └── Models.cs               ← All models + ViewModels
├── Data/
│   └── LibraryDbContext.cs     ← EF Core DbContext
├── Services/
│   └── LibraryService.cs       ← Business logic layer
├── Views/
│   ├── Shared/_Layout.cshtml
│   ├── Student/
│   │   ├── Login.cshtml
│   │   ├── Dashboard.cshtml
│   │   └── Scan.cshtml
│   └── Books/
│       └── Index.cshtml
├── Scripts/
│   └── DatabaseSchema.sql      ← Full DB script with SPs + seed data
├── Program.cs
└── appsettings.json
```

---

## Setup Instructions

### Step 1 – Database (SSMS)

1. Open **SQL Server Management Studio**
2. Connect to your SQL Server instance (usually `.\SQLEXPRESS` or `.`)
3. Open `Scripts/DatabaseSchema.sql`
4. Press **F5** to execute — this creates:
   - `LibraryDB` database
   - All 3 tables with constraints
   - 2 stored procedures (`sp_IssueBook`, `sp_ReturnBook`, `sp_GetStudentDashboard`)
   - Seed data (4 students, 5 books)

### Step 2 – Connection String

Edit `appsettings.json`:
```json
"ConnectionStrings": {
    "LibraryDB": "Server=.;Database=LibraryDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

> If your server name is different (e.g. `LAPTOP-XYZ\SQLEXPRESS`), update `Server=` accordingly.

### Step 3 – Run the Application

```bash
cd LibrarySystem
dotnet restore
dotnet run
```

Open: `https://localhost:5001`

---

## API Documentation

Swagger UI available at: `https://localhost:5001/swagger`

### Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/library/login` | Student authentication |
| POST | `/api/library/issue` | Issue book via QR code |
| POST | `/api/library/return` | Return a book |
| GET | `/api/library/books` | Get all books with availability |
| GET | `/api/library/dashboard/{universityId}` | Student dashboard data |
| GET | `/api/library/scan?qr={code}` | Scan QR — get book info |
| GET | `/api/library/transactions` | All transactions (admin) |

### Sample API Calls

**Login:**
```http
POST /api/library/login
Content-Type: application/json

{
  "universityID": "2024F-BS-0001",
  "password": "Pass@123"
}
```

**Issue Book:**
```http
POST /api/library/issue
Content-Type: application/json

{
  "universityID": "2024F-BS-0001",
  "qrCode": "SSUET-LIB-BOOK-978-0-13-468599-1"
}
```

**Return Book:**
```http
POST /api/library/return
Content-Type: application/json

{
  "txnCode": "TXN-20260524-123456"
}
```

**Response Format:**
```json
{
  "success": true,
  "message": "Book issued successfully! Due in 14 days.",
  "data": {
    "txnCode": "TXN-20260524-847392",
    "issueDate": "2026-05-24T10:30:00",
    "dueDate": "2026-06-07T10:30:00",
    "bookTitle": "Database System Concepts",
    "studentName": "Ali Hassan"
  }
}
```

---

## QR Code Format

Each book's QR code follows this format:
```
SSUET-LIB-BOOK-{ISBN}
```

Example: `SSUET-LIB-BOOK-978-0-13-468599-1`

The QR scan page supports:
1. **Camera scanning** via `html5-qrcode` library
2. **Manual entry** (for testing)
3. **Quick-fill buttons** for demo

---

## Demo Credentials

| University ID | Password | Status |
|---------------|----------|--------|
| 2024F-BS-0001 | Pass@123 | Active |
| 2024F-BS-0002 | Pass@123 | Active |
| 2024F-BS-0003 | Pass@123 | Active |
| 2024F-BS-0099 | Pass@123 | **Inactive** (cannot borrow) |

---

## Test Scenarios

1. **Normal issue** → Login, scan QR, book issued, transaction ID generated
2. **Borrow limit** → Issue 3 books, try 4th → "Limit reached" error
3. **Unavailable book** → Set AvailableCopies=0 in DB, scan → "Unavailable" error
4. **Inactive student** → Login as 2024F-BS-0099 → "Inactive account" error
5. **Duplicate issue** → Scan same book QR twice → "Already issued" error
6. **Return** → Click Return on dashboard → book status updated, copies incremented
7. **History preserved** → Returned transactions still visible in history

---

## SQL Scripts Summary

| Object | Type | Purpose |
|--------|------|---------|
| `Students` | Table | Student records with IsActive flag |
| `Books` | Table | Book catalog with QR codes & availability |
| `Transactions` | Table | Issue/return records — never deleted |
| `sp_IssueBook` | Stored Proc | Atomic issue with all 5 validations |
| `sp_ReturnBook` | Stored Proc | Return with copy increment |
| `sp_GetStudentDashboard` | Stored Proc | Student's book history |
| `SeqTransaction` | Sequence | Auto-increment for TxnCode |

---

*Submitted by: [Your Name] | Roll No: [Your Roll No] | Section: [Your Section]*
