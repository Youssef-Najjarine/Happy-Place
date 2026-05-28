CREATE TABLE [dbo].[PendingPhoneChange]
(
	[Id] uniqueidentifier NOT NULL 
		constraint [Pk-PendingPhoneChange] PRIMARY KEY clustered
		constraint [DF-PendingPhoneChange-Id] default newid(),
	[UserAccountId] uniqueidentifier NOT NULL
		constraint [Fk-PendingPhoneChange-UserAccountId-UserAccount-Id]
		FOREIGN KEY REFERENCES [dbo].[UserAccount]([Id]) ON DELETE CASCADE,
	[NewPhoneNumber] varchar(20) NOT NULL,
	[VerificationCode] varchar(6) NOT NULL,
	[AttemptCount] int NOT NULL
		constraint [DF-PendingPhoneChange-AttemptCount] default 0,
	[CreatedAtUtc] datetime2(0) NOT NULL
		constraint [DF-PendingPhoneChange-CreatedAtUtc] default sysutcdatetime(),
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-PendingPhoneChange-UserAccountId]
	ON [dbo].[PendingPhoneChange]([UserAccountId]);
GO
