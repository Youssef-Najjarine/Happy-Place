CREATE TABLE [dbo].[DeviceToken]
(
	[Id] uniqueidentifier NOT NULL
		constraint [Pk-DeviceToken] PRIMARY KEY clustered
		constraint [DF-DeviceToken-Id] default newid(),
	[UserAccountId] uniqueidentifier NOT NULL
		constraint [FK-DeviceToken-UserAccountId]
		FOREIGN KEY REFERENCES [dbo].[UserAccount]([Id]) ON DELETE CASCADE,
	[Token] nvarchar(512) NOT NULL,
	[Platform] nvarchar(20) NOT NULL,
	[CreatedAtUtc] datetime2(7) NOT NULL
		constraint [DF-DeviceToken-CreatedAtUtc] default sysutcdatetime(),
	[LastSeenAtUtc] datetime2(0) NOT NULL
		constraint [DF-DeviceToken-LastSeenAtUtc] default sysutcdatetime()
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-DeviceToken-Token]
	ON [dbo].[DeviceToken]([Token]);
GO

CREATE NONCLUSTERED INDEX [IX-DeviceToken-UserAccountId]
	ON [dbo].[DeviceToken]([UserAccountId]);
GO
