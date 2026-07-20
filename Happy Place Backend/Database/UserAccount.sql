CREATE TABLE [dbo].[UserAccount]
(
	[Id] uniqueidentifier NOT NULL 
		constraint [Pk-UserAccount] PRIMARY KEY clustered
		constraint [DF-UserAccount-Id] default newid(),
	[Username] nvarchar(20) NULL,
	[HashedPassword] varchar(100) NULL,
	[DisplayName] nvarchar(200) NOT NULL,
	[EmailAddress] nvarchar(255) NULL,
	[PhoneNumber] varchar(20) NULL,
	[Bio] nvarchar(MAX) NULL,
	[ProfilePhotoUrl] nvarchar(500) NULL,
    [BackgroundPhotoUrl] nvarchar(500) NULL,
	[IsAnonymous] bit NOT NULL
		constraint [DF-UserAccount-IsAnonymous] default 0,
	[GuestMessageCount] int NOT NULL
		constraint [DF-UserAccount-GuestMessageCount] default 0,
	[CreatedAtUtc] datetime2(0) NOT NULL
		constraint [DF-UserAccount-CreatedAtUtc] default sysutcdatetime(),
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-UserAccount-Username]
	ON [dbo].[UserAccount]([Username])
	WHERE [Username] IS NOT NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-UserAccount-EmailAddress]
	ON [dbo].[UserAccount]([EmailAddress])
	WHERE [EmailAddress] IS NOT NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-UserAccount-PhoneNumber]
	ON [dbo].[UserAccount]([PhoneNumber])
	WHERE [PhoneNumber] IS NOT NULL;
GO
