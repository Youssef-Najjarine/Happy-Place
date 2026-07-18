CREATE TABLE [dbo].[ChatGroup]
(
	[Id] uniqueidentifier NOT NULL
		constraint [Pk-ChatGroup] PRIMARY KEY clustered
		constraint [DF-ChatGroup-Id] default newid(),
	[Name] nvarchar(100) NOT NULL,
	[OwnerUserAccountId] uniqueidentifier NULL
		constraint [FK-ChatGroup-OwnerUserAccountId]
		FOREIGN KEY REFERENCES [dbo].[UserAccount]([Id]),
	[IsPublic] bit NOT NULL
		constraint [DF-ChatGroup-IsPublic] default 0,
	[Status] tinyint NOT NULL
		constraint [DF-ChatGroup-Status] default 2
		constraint [CK-ChatGroup-Status] CHECK ([Status] IN (1, 2, 3)),
	[CreatedAtUtc] datetime2(0) NOT NULL
		constraint [DF-ChatGroup-CreatedAtUtc] default sysutcdatetime(),
	[LastSeenAtUtc] datetime2(0) NOT NULL
		constraint [DF-ChatGroup-LastSeenAtUtc] default sysutcdatetime(),
	[LastMessageSequence] bigint NOT NULL
		constraint [DF-ChatGroup-LastMessageSequence] default 0,
	[LastChangeSequence] bigint NOT NULL
		constraint [DF-ChatGroup-LastChangeSequence] default 0,
	[DirectPairLowId] uniqueidentifier NULL,
	[DirectPairHighId] uniqueidentifier NULL,
	constraint [CK-ChatGroup-DirectPair] CHECK (([DirectPairLowId] IS NULL AND [DirectPairHighId] IS NULL) OR ([DirectPairLowId] IS NOT NULL AND [DirectPairHighId] IS NOT NULL AND [DirectPairLowId] < [DirectPairHighId])),
	constraint [CK-ChatGroup-DirectNotPublic] CHECK ([DirectPairLowId] IS NULL OR [IsPublic] = 0),
	constraint [CK-ChatGroup-DirectUnowned] CHECK ([DirectPairLowId] IS NULL OR [OwnerUserAccountId] IS NULL)
)
GO

CREATE NONCLUSTERED INDEX [IX-ChatGroup-OwnerUserAccountId]
	ON [dbo].[ChatGroup]([OwnerUserAccountId]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-ChatGroup-OwnerUserAccountId-Provisional]
	ON [dbo].[ChatGroup]([OwnerUserAccountId])
	WHERE [Status] = 1;
GO

CREATE NONCLUSTERED INDEX [IX-ChatGroup-Public-Active-CreatedAtUtc]
	ON [dbo].[ChatGroup]([CreatedAtUtc])
	WHERE [IsPublic] = 1 AND [Status] = 2;
GO

CREATE NONCLUSTERED INDEX [IX-ChatGroup-Provisional-LastSeenAtUtc]
	ON [dbo].[ChatGroup]([LastSeenAtUtc])
	WHERE [Status] = 1;
GO

CREATE NONCLUSTERED INDEX [IX-ChatGroup-Status-CreatedAtUtc]
	ON [dbo].[ChatGroup]([Status], [CreatedAtUtc])
	INCLUDE ([Name], [OwnerUserAccountId], [IsPublic], [LastSeenAtUtc]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-ChatGroup-DirectPairLowId-DirectPairHighId]
	ON [dbo].[ChatGroup]([DirectPairLowId], [DirectPairHighId])
	WHERE [DirectPairLowId] IS NOT NULL;
GO
