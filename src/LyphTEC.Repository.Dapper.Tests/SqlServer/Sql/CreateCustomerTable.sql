IF (OBJECT_ID('Customer') IS NOT NULL)
BEGIN
    DROP TABLE Customer
END

CREATE TABLE [dbo].[Customer](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FirstName] [nvarchar](50) NOT NULL,
	[LastName] [nvarchar](50) NOT NULL,
	[Company] [nvarchar](100) NOT NULL,
	[Phone] [nvarchar](50) NULL,
	[Email] [nvarchar](50) NULL,
	[Address] [nvarchar](600) NULL,
	[DateCreatedUtc] [smalldatetime] NOT NULL,
	[DateUpdatedUtc] [smalldatetime] NOT NULL,
 CONSTRAINT [PK_Customer] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 50) ON [PRIMARY]
) ON [PRIMARY];

ALTER TABLE [dbo].[Customer] ADD  CONSTRAINT [DF_Customer_DateCreatedUtc]  DEFAULT (getutcdate()) FOR [DateCreatedUtc];

ALTER TABLE [dbo].[Customer] ADD  CONSTRAINT [DF_Customer_DateUpdatedUtc]  DEFAULT (getutcdate()) FOR [DateUpdatedUtc];
