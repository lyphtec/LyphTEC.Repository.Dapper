CREATE TABLE [Invoice](
	[Id] [uniqueidentifier] PRIMARY KEY NOT NULL,
	[CustomerId] [integer] NULL,
	[InvoiceDate] [datetime] NOT NULL,
	[BillingAddress] [nvarchar](600) NULL,
	[Total] [money] NOT NULL,
	[DateCreatedUtc] [datetime] NOT NULL,
	[DateUpdatedUtc] [datetime] NOT NULL
)