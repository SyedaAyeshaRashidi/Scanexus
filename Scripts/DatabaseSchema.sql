USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'LibraryDB')
    DROP DATABASE LibraryDB;
GO

CREATE DATABASE LibraryDB;
GO

USE LibraryDB;
GO

CREATE TABLE Students (
    StudentID    INT PRIMARY KEY IDENTITY(1,1),
    UniversityID VARCHAR(20)  NOT NULL UNIQUE,
    FullName     VARCHAR(100) NOT NULL,
    FatherName   VARCHAR(100) NOT NULL,
    Email        VARCHAR(100) NOT NULL,
    PasswordHash VARCHAR(100) NOT NULL,
    Semester     INT NOT NULL,
    Batch        VARCHAR(20)  NOT NULL,
    IsActive     BIT NOT NULL DEFAULT 1,
    CreatedAt    DATETIME NOT NULL DEFAULT GETDATE()
);
GO
Select * FROM Students

CREATE TABLE Admins (
    AdminID VARCHAR(20) NOT NULL UNIQUE,
    PasswordHash VARCHAR(256) NOT NULL,
    Email VARCHAR(100),
);

INSERT INTO Admins (AdminID, PasswordHash, Email)
VALUES ( 'ADMIN-001', 'Admin@123', 'admin@scanexus.com');

SELECT * FROM Admins;

CREATE TABLE Books (
    BookID          INT PRIMARY KEY IDENTITY(1,1),
    ISBN            VARCHAR(20)  NOT NULL,
    Title           VARCHAR(200) NOT NULL,
    Author          VARCHAR(100) NOT NULL,
    Publisher       VARCHAR(150),
    TotalCopies     INT NOT NULL DEFAULT 1,
    AvailableCopies INT NOT NULL DEFAULT 1,
    QRCode          VARCHAR(200) NOT NULL UNIQUE,
    AddedAt         DATETIME NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE Transactions (
    TransactionID INT PRIMARY KEY IDENTITY(1,1),
    TxnCode       VARCHAR(50)  NOT NULL UNIQUE,
    StudentID     INT NOT NULL FOREIGN KEY REFERENCES Students(StudentID),
    BookID        INT NOT NULL FOREIGN KEY REFERENCES Books(BookID),
    IssueDate     DATETIME NOT NULL DEFAULT GETDATE(),
    DueDate       DATETIME NOT NULL,
    ReturnDate    DATETIME NULL,
    Status        VARCHAR(20) NOT NULL DEFAULT 'Active',
    QRScanData    VARCHAR(200) NOT NULL DEFAULT ''
);
GO

INSERT INTO Students (UniversityID, FullName, FatherName, Email, PasswordHash, Semester, Batch, IsActive)
VALUES
('2024F-BS-0001', 'Ali Hassan',  'Hassan Khan', 'ali@ssuet.edu.pk',  'Pass@123', 4, '2024F', 1),
('2024F-BS-0002', 'Sara Ahmed',  'Ahmed Raza',  'sara@ssuet.edu.pk', 'Pass@123', 4, '2024F', 1),
('2024F-BS-0003', 'Usman Tariq', 'Tariq Mehmood','usman@ssuet.edu.pk','Pass@123', 4, '2024F', 0);
GO

INSERT INTO Books (ISBN, Title, Author, Publisher, TotalCopies, AvailableCopies, QRCode)
VALUES
('978-0-13-4685', 'Database System Concepts', 'Silberschatz', 'McGraw-Hill', 3, 3, 'BOOK-001'),
('978-0-13-1103', 'C Programming Language',   'Kernighan',    'Prentice Hall', 2, 2, 'BOOK-002'),
('978-0-20-1633', 'Design Patterns',           'Gang of Four', 'Addison-Wesley', 2, 2, 'BOOK-003'),
('978-0-13-2350', 'Computer Networking',       'Kurose Ross',  'Pearson', 4, 4, 'BOOK-004');
GO

USE LibraryDB;
ALTER TABLE Transactions ADD Remarks VARCHAR(300) NULL;


USE LibraryDB;
UPDATE Books SET QRCode = 'SSUET-LIB-BOOK-001' WHERE BookID = 1;
UPDATE Books SET QRCode = 'SSUET-LIB-BOOK-002' WHERE BookID = 2;
UPDATE Books SET QRCode = 'SSUET-LIB-BOOK-003' WHERE BookID = 3;
UPDATE Books SET QRCode = 'SSUET-LIB-BOOK-004' WHERE BookID = 4;


UPDATE Students SET IsActive = 1 WHERE UniversityID = '2024F-BS-0003';


ALTER TABLE Transactions ADD Remarks VARCHAR(300) NULL;

USE LibraryDB;
ALTER TABLE Transactions 
ADD FineAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
    FinePaid BIT NOT NULL DEFAULT 0;