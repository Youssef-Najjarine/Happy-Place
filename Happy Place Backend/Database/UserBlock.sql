CREATE TABLE [dbo].[UserBlock]
(
	[Id] uniqueidentifier NOT NULL
		constraint [Pk-UserBlock] PRIMARY KEY clustered
		constraint [DF-UserBlock-Id] default newid(),
	[BlockerUserAccountId] uniqueidentifier NOT NULL
		constraint [FK-UserBlock-BlockerUserAccountId]
		FOREIGN KEY REFERENCES [dbo].[UserAccount]([Id]),
	[BlockedUserAccountId] uniqueidentifier NOT NULL
		constraint [FK-UserBlock-BlockedUserAccountId]
		FOREIGN KEY REFERENCES [dbo].[UserAccount]([Id]),
	[CreatedAtUtc] datetime2(0) NOT NULL
		constraint [DF-UserBlock-CreatedAtUtc] default sysutcdatetime(),
	constraint [CK-UserBlock-NoSelfBlock] CHECK ([BlockerUserAccountId] <> [BlockedUserAccountId])
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-UserBlock-BlockerUserAccountId-BlockedUserAccountId]
	ON [dbo].[UserBlock]([BlockerUserAccountId], [BlockedUserAccountId]);
GO

CREATE NONCLUSTERED INDEX [IX-UserBlock-BlockedUserAccountId]
	ON [dbo].[UserBlock]([BlockedUserAccountId]);
GO
