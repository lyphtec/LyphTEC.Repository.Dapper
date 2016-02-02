CREATE TABLE GuidEntity (
	[Id] uniqueidentifier ROWGUIDCOL PRIMARY KEY,
	[Name] nvarchar(50) not null,
	[DateField] datetime not null,
	[Address] nvarchar(600),
	DateCreatedUtc datetime,
	DateUpdatedUtc datetime
)