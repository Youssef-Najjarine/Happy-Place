CREATE TABLE [dbo].[NotificationChannel]
(
	[Id] uniqueidentifier NOT NULL
		constraint [Pk-NotificationChannel] PRIMARY KEY clustered
		constraint [DF-NotificationChannel-Id] default newid(),
	[RecipientUserAccountId] uniqueidentifier NOT NULL
		constraint [FK-NotificationChannel-RecipientUserAccountId]
		FOREIGN KEY REFERENCES [dbo].[UserAccount]([Id]) ON DELETE CASCADE,
	[Kind] tinyint NOT NULL
		constraint [CK-NotificationChannel-Kind] CHECK ([Kind] IN (1, 2, 3)),
	[ScopeChatGroupId] uniqueidentifier NULL
		constraint [FK-NotificationChannel-ScopeChatGroupId]
		FOREIGN KEY REFERENCES [dbo].[ChatGroup]([Id]) ON DELETE CASCADE,
	[LastSentCount] int NOT NULL
		constraint [DF-NotificationChannel-LastSentCount] default 0,
	[IsLive] bit NOT NULL
		constraint [DF-NotificationChannel-IsLive] default 0,
	[FirstDirtyAtUtc] datetime2(7) NULL,
	[LastEventAtUtc] datetime2(7) NULL,
	[DueAtUtc] datetime2(7) NULL,
	[LastSentAtUtc] datetime2(7) NULL,
	[ClaimToken] uniqueidentifier NULL,
	[ClaimExpiresAtUtc] datetime2(7) NULL
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-NotificationChannel-Waiting]
	ON [dbo].[NotificationChannel]([RecipientUserAccountId])
	WHERE [Kind] = 1;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-NotificationChannel-Offers]
	ON [dbo].[NotificationChannel]([RecipientUserAccountId], [ScopeChatGroupId])
	WHERE [Kind] = 2;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-NotificationChannel-JoinRequests]
	ON [dbo].[NotificationChannel]([ScopeChatGroupId])
	WHERE [Kind] = 3;
GO

CREATE NONCLUSTERED INDEX [IX-NotificationChannel-Due]
	ON [dbo].[NotificationChannel]([DueAtUtc])
	INCLUDE ([Kind])
	WHERE [DueAtUtc] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX-NotificationChannel-ScopeChatGroupId]
	ON [dbo].[NotificationChannel]([ScopeChatGroupId])
	WHERE [Kind] = 2;
GO

CREATE NONCLUSTERED INDEX [IX-NotificationChannel-Kind]
	ON [dbo].[NotificationChannel]([Kind])
	INCLUDE ([RecipientUserAccountId]);
GO
