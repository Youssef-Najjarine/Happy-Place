CREATE TABLE [dbo].[PendingUserAccount]
(
	[Id] uniqueidentifier NOT NULL 
		constraint [Pk-PendingUserAccount] PRIMARY KEY clustered
		constraint [DF-PendingUserAccount-Id] default newid(),
	[Username] nvarchar(20) NOT NULL,
	[HashedPassword] varchar(100) NOT NULL,
	[DisplayName] nvarchar(200) NOT NULL,
	[VerificationCode] varchar(6) NOT NULL,
	[EmailAddress] nvarchar(255) NULL,
	[PhoneNumber] varchar(20) NULL,
	[CreatedAtUtc] datetime2(0) NOT NULL
		constraint [DF-PendingUserAccount-CreatedAtUtc] default sysutcdatetime(),
)