CREATE TABLE Customer (
		Id INTEGER PRIMARY KEY AUTOINCREMENT,  -- In SQLITE3, this is the alias for ROWID
        FirstName NVARCHAR(50),
        LastName NVARCHAR(50),
        Company NVARCHAR(100),
        Phone NVARCHAR(50),
        Email NVARCHAR(50),

		[Address] NVARCHAR(600),

        DateCreatedUtc DATETIME,
        DateUpdatedUtc DATETIME
)