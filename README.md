# Scanexus - Smart Library System

## Project Overview

Scanexus is a QR-Based Digital Library System developed for the Database System (CS-229T) course. 

Initially focused on an automated issuing module, the system has now been upgraded to achieve full circulation automation. The platform allows students to request, receive, and return books on demand by scanning a QR code. Built using the ASP.NET Core MVC architecture, the system verifies availability, handles digital history tracking, and automatically calculates fines for overdue items. Librarians can monitor all circulation activities, inventory, and users through a comprehensive admin dashboard.

---

## Technology Stack

### Frontend
* ASP.NET Core MVC (Razor Views)
* HTML5 & CSS3

### Backend & Logic
* ASP.NET Core Web App (Controllers & Models)
* C# 
* ngrok (Implemented to allow global cross-device access for the QR scanning logic)

### Database
* Microsoft SQL Server (SSMS 2022)

---

## API Modules

Swagger UI available at: `https://localhost:5001/swagger`

### Endpoints

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| POST | `/api/library/login` | Student authentication |
| POST | `/api/library/issue` | Issue book via QR code |
| POST | `/api/library/return` | Return a book |
| GET | `/api/library/books` | Get all books with availability |
| GET | `/api/library/dashboard/{universityId}` | Student dashboard data |
| GET | `/api/library/scan?qr={code}` | Scan QR — get book info |
| GET | `/api/library/transactions` | All transactions (admin) |

---

## Features Implemented (Assignments 1 & 2)

### Student Panel
* **Authentication:** Login using university ID.
* **QR Borrowing:** Scan QR code using a mobile device via ngrok remote access. The system prevents issuing unavailable books or exceeding the 3-book limit.
* **QR Returns:** Scan QR to return books, automatically updating the return date and changing the book's availability status.
* **Dashboard & History:** View issued books, due dates, and track borrowing history digitally.
* **Notifications:** Receive overdue warnings and automated fine calculations.

### Librarian / Admin Dashboard
Librarians can monitor all circulation activities using the dedicated dashboard:
* **General Metrics:** View total issued books, overdue books, and active borrowers.
* **Financial Reporting:** Access fine reports and a fine collection summary.
* **Inventory & Analytics:** Monitor daily transactions, active inventory, and track the most borrowed books.
* **User Management:** Monitor active borrowers and view the defaulters list.

### System Features
* Generate unique transaction IDs and store issue history permanently.
* Notification engine for overdue warnings.
* Automated fine calculation engine.

---

## Database Schema

The complete relational schema of the database is available in the project file.

Refer to this document for table structures, keys, and relationships used in the project.


## Borrowing & Return Workflows

### Borrowing Workflow
1. Student logs in using their university ID.
2. Student accesses the application on their mobile device via the provided ngrok link.
3. Student scans the QR code provided in the book catalog.
4. Backend validates student eligibility, active status, book availability, and borrowing limits.
5. A unique transaction ID is generated, and the book is issued digitally without manual intervention.
6. Transaction records are permanently stored in the database.

### Return Workflow
1. Student scans the QR code to return the borrowed book.
2. The system registers the action and updates the return date in the database.
3. The availability status of the book is immediately changed to available.
4. The system calculates any overdue fine automatically.
5. If applicable, the student is notified of overdue warnings or fines.


## Project Scope & Deliverables

### Implemented Modules
* Database schema and SQL scripts
* Student authentication module and dashboard
* QR issuing and return module
* Transaction management and reporting system
* Fine system and notification engine
* Admin/Librarian dashboard
* API documentation


## Authors

**Syeda Ayesha Rashidi** --> 2024F-BCS-057  
**Yousuf Khan** --> 2024F-BCS-106  
**Hafsa Fatima** --> 2024F-BCS-075  
**Syed Farzan Ali Anvery** --> 2024F-BCS-095

Database System (CS-229T) Assignment 2
QR-Based Digital Library System
