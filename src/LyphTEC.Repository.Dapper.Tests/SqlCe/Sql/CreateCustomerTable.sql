CREATE TABLE Customer (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FirstName NVARCHAR(50),
        LastName NVARCHAR(50),
        Company NVARCHAR(100),
        Phone NVARCHAR(50),
        Email NVARCHAR(50),

		[Address] NVARCHAR(600),

        DateCreatedUtc DATETIME,
        DateUpdatedUtc DATETIME
)