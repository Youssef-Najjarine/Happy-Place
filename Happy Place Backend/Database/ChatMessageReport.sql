CREATE TABLE [dbo].[ChatMessageReport]
(
	[Id] uniqueidentifier NOT NULL
		constraint [Pk-ChatMessageReport] PRIMARY KEY clustered
		constraint [DF-ChatMessageReport-Id] default newid(),
	[ChatMessageId] uniqueidentifier NOT NULL
		constraint [FK-ChatMessageReport-ChatMessageId]
		FOREIGN KEY REFERENCES [dbo].[ChatMessage]([Id]) ON DELETE CASCADE,
	[ReporterUserAccountId] uniqueidentifier NOT NULL,
	[ReportedUserAccountId] uniqueidentifier NULL,
	[Kind] tinyint NOT NULL
		constraint [CK-ChatMessageReport-Kind] CHECK ([Kind] IN (1, 2, 3, 4)),
	[BodySnapshotCipher] varbinary(MAX) NULL,
	[ReasonCipher] varbinary(MAX) NULL,
	[Status] tinyint NOT NULL
		constraint [DF-ChatMessageReport-Status] default 1
		constraint [CK-ChatMessageReport-Status] CHECK ([Status] IN (1, 2)),
	[CreatedAtUtc] datetime2(7) NOT NULL
		constraint [DF-ChatMessageReport-CreatedAtUtc] default sysutcdatetime()
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-ChatMessageReport-ChatMessageId-ReporterUserAccountId]
	ON [dbo].[ChatMessageReport]([ChatMessageId], [ReporterUserAccountId]);
GO

CREATE NONCLUSTERED INDEX [IX-ChatMessageReport-Status-CreatedAtUtc]
	ON [dbo].[ChatMessageReport]([Status], [CreatedAtUtc]);
GO
