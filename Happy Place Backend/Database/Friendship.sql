CREATE TABLE [dbo].[Friendship]
(
	[Id] uniqueidentifier NOT NULL
		constraint [Pk-Friendship] PRIMARY KEY clustered
		constraint [DF-Friendship-Id] default newid(),
	[RequesterUserAccountId] uniqueidentifier NOT NULL
		constraint [FK-Friendship-RequesterUserAccountId]
		FOREIGN KEY REFERENCES [dbo].[UserAccount]([Id]),
	[AddresseeUserAccountId] uniqueidentifier NOT NULL
		constraint [FK-Friendship-AddresseeUserAccountId]
		FOREIGN KEY REFERENCES [dbo].[UserAccount]([Id]),
	[Status] tinyint NOT NULL
		constraint [CK-Friendship-Status] CHECK ([Status] IN (1, 2)),
	[CreatedAtUtc] datetime2(0) NOT NULL
		constraint [DF-Friendship-CreatedAtUtc] default sysutcdatetime(),
	[RespondedAtUtc] datetime2(0) NULL,
	[PairLowId] AS (CASE WHEN [RequesterUserAccountId] <= [AddresseeUserAccountId] THEN [RequesterUserAccountId] ELSE [AddresseeUserAccountId] END) PERSISTED NOT NULL,
	[PairHighId] AS (CASE WHEN [RequesterUserAccountId] <= [AddresseeUserAccountId] THEN [AddresseeUserAccountId] ELSE [RequesterUserAccountId] END) PERSISTED NOT NULL,
	constraint [CK-Friendship-NoSelfFriendship] CHECK ([RequesterUserAccountId] <> [AddresseeUserAccountId])
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-Friendship-PairLowId-PairHighId]
	ON [dbo].[Friendship]([PairLowId], [PairHighId]);
GO

CREATE NONCLUSTERED INDEX [IX-Friendship-AddresseeUserAccountId-Status]
	ON [dbo].[Friendship]([AddresseeUserAccountId], [Status]);
GO

CREATE NONCLUSTERED INDEX [IX-Friendship-RequesterUserAccountId-Status]
	ON [dbo].[Friendship]([RequesterUserAccountId], [Status]);
GO
