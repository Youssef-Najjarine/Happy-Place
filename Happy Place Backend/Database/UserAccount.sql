CREATE TABLE [dbo].[UserAccount]
(
	[Id] uniqueidentifier NOT NULL 
		constraint [Pk-UserAccount] PRIMARY KEY clustered
		constraint [DF-UserAccount-Id] default newid(),
	[Username] nvarchar(20) NOT NULL
		constraint [UQ-UserAccount-Username] UNIQUE,
	[HashedPassword] varchar(100) NOT NULL,
	[DisplayName] nvarchar(200) NOT NULL,
	[EmailAddress] nvarchar(255) NULL,
	[PhoneNumber] varchar(20) NULL,
	[Bio] nvarchar(500) NULL,
	[ProfilePhotoUrl] nvarchar(500) NULL,
    [BackgroundPhotoUrl] nvarchar(500) NULL,
	[CreatedAtUtc] datetime2(0) NOT NULL
		constraint [DF-UserAccount-CreatedAtUtc] default sysutcdatetime(),
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-UserAccount-EmailAddress]
	ON [dbo].[UserAccount]([EmailAddress])
	WHERE [EmailAddress] IS NOT NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-UserAccount-PhoneNumber]
	ON [dbo].[UserAccount]([PhoneNumber])
	WHERE [PhoneNumber] IS NOT NULL;
GO
