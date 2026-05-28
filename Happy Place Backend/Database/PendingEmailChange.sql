CREATE TABLE [dbo].[PendingEmailChange]
(
	[Id] uniqueidentifier NOT NULL 
		constraint [Pk-PendingEmailChange] PRIMARY KEY clustered
		constraint [DF-PendingEmailChange-Id] default newid(),
	[UserAccountId] uniqueidentifier NOT NULL
		constraint [Fk-PendingEmailChange-UserAccountId-UserAccount-Id]
		FOREIGN KEY REFERENCES [dbo].[UserAccount]([Id]) ON DELETE CASCADE,
	[NewEmailAddress] nvarchar(255) NOT NULL,
	[VerificationCode] varchar(6) NOT NULL,
	[AttemptCount] int NOT NULL
		constraint [DF-PendingEmailChange-AttemptCount] default 0,
	[CreatedAtUtc] datetime2(0) NOT NULL
		constraint [DF-PendingEmailChange-CreatedAtUtc] default sysutcdatetime(),
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-PendingEmailChange-UserAccountId]
	ON [dbo].[PendingEmailChange]([UserAccountId]);
GO
