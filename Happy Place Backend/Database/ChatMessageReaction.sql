CREATE TABLE [dbo].[ChatMessageReaction]
(
	[Id] uniqueidentifier NOT NULL
		constraint [Pk-ChatMessageReaction] PRIMARY KEY clustered
		constraint [DF-ChatMessageReaction-Id] default newid(),
	[ChatMessageId] uniqueidentifier NOT NULL
		constraint [FK-ChatMessageReaction-ChatMessageId]
		FOREIGN KEY REFERENCES [dbo].[ChatMessage]([Id]) ON DELETE CASCADE,
	[UserAccountId] uniqueidentifier NOT NULL
		constraint [FK-ChatMessageReaction-UserAccountId]
		FOREIGN KEY REFERENCES [dbo].[UserAccount]([Id]),
	[Emoji] nvarchar(20) NOT NULL,
	[CreatedAtUtc] datetime2(7) NOT NULL
		constraint [DF-ChatMessageReaction-CreatedAtUtc] default sysutcdatetime()
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-ChatMessageReaction-ChatMessageId-UserAccountId]
	ON [dbo].[ChatMessageReaction]([ChatMessageId], [UserAccountId]);
GO

CREATE NONCLUSTERED INDEX [IX-ChatMessageReaction-UserAccountId]
	ON [dbo].[ChatMessageReaction]([UserAccountId]);
GO
