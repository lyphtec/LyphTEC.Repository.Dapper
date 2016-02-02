IF (OBJECT_ID('Invoice') IS NOT NULL)
BEGIN
    DROP TABLE Invoice
END

CREATE TABLE [dbo].[Invoice](
	[Id] [uniqueidentifier] NOT NULL,
	[CustomerId] [int] NULL,
	[InvoiceDate] [smalldatetime] NOT NULL,
	[BillingAddress] [nvarchar](600) NULL,
	[Total] [money] NOT NULL,
	[DateCreatedUtc] [smalldatetime] NOT NULL,
	[DateUpdatedUtc] [smalldatetime] NOT NULL,
 CONSTRAINT [PK_Invoice] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 50) ON [PRIMARY]
) ON [PRIMARY];

ALTER TABLE [dbo].[Invoice] ADD  CONSTRAINT [DF_Invoice_Id]  DEFAULT (newid()) FOR [Id];

ALTER TABLE [dbo].[Invoice] ADD  CONSTRAINT [DF_Invoice_DateCreatedUtc]  DEFAULT (getutcdate()) FOR [DateCreatedUtc];

ALTER TABLE [dbo].[Invoice] ADD  CONSTRAINT [DF_Invoice_DateUpdatedUtc]  DEFAULT (getutcdate()) FOR [DateUpdatedUtc];
