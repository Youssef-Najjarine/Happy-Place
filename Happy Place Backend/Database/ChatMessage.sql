CREATE TABLE [dbo].[ChatMessage]
(
	[Id] uniqueidentifier NOT NULL
		constraint [Pk-ChatMessage] PRIMARY KEY clustered
		constraint [DF-ChatMessage-Id] default newid(),
	[ChatGroupId] uniqueidentifier NOT NULL
		constraint [FK-ChatMessage-ChatGroupId]
		FOREIGN KEY REFERENCES [dbo].[ChatGroup]([Id]) ON DELETE CASCADE,
	[SenderUserAccountId] uniqueidentifier NULL
		constraint [FK-ChatMessage-SenderUserAccountId]
		FOREIGN KEY REFERENCES [dbo].[UserAccount]([Id]) ON DELETE SET NULL,
	[ClientMessageId] uniqueidentifier NOT NULL,
	[ReplyToChatMessageId] uniqueidentifier NULL,
	[Kind] tinyint NOT NULL
		constraint [CK-ChatMessage-Kind] CHECK ([Kind] IN (1, 2, 3, 4)),
	[BodyCipher] varbinary(MAX) NULL,
	[CipherVersion] tinyint NOT NULL
		constraint [DF-ChatMessage-CipherVersion] default 1,
	[Sequence] bigint NOT NULL,
	[ChangeSequence] bigint NOT NULL,
	[IsDeleted] bit NOT NULL
		constraint [DF-ChatMessage-IsDeleted] default 0,
	[CreatedAtUtc] datetime2(7) NOT NULL
		constraint [DF-ChatMessage-CreatedAtUtc] default sysutcdatetime()
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-ChatMessage-ChatGroupId-ClientMessageId]
	ON [dbo].[ChatMessage]([ChatGroupId], [ClientMessageId]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-ChatMessage-ChatGroupId-Sequence]
	ON [dbo].[ChatMessage]([ChatGroupId], [Sequence]);
GO

CREATE NONCLUSTERED INDEX [IX-ChatMessage-ChatGroupId-ChangeSequence]
	ON [dbo].[ChatMessage]([ChatGroupId], [ChangeSequence]);
GO
