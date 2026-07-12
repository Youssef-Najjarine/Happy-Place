CREATE TABLE [dbo].[ChatMediaAsset]
(
	[Id] uniqueidentifier NOT NULL
		constraint [Pk-ChatMediaAsset] PRIMARY KEY clustered
		constraint [DF-ChatMediaAsset-Id] default newid(),
	[ChatGroupId] uniqueidentifier NOT NULL
		constraint [FK-ChatMediaAsset-ChatGroupId]
		FOREIGN KEY REFERENCES [dbo].[ChatGroup]([Id]) ON DELETE CASCADE,
	[UploaderUserAccountId] uniqueidentifier NULL
		constraint [FK-ChatMediaAsset-UploaderUserAccountId]
		FOREIGN KEY REFERENCES [dbo].[UserAccount]([Id]) ON DELETE SET NULL,
	[AttachedMessageId] uniqueidentifier NULL,
	[Kind] tinyint NOT NULL
		constraint [CK-ChatMediaAsset-Kind] CHECK ([Kind] IN (2, 3, 4)),
	[StorageMode] tinyint NOT NULL
		constraint [CK-ChatMediaAsset-StorageMode] CHECK ([StorageMode] IN (1, 2)),
	[ContentBytes] varbinary(MAX) NULL,
	[FilePath] nvarchar(400) NULL,
	[ContentType] varchar(100) NOT NULL,
	[ByteCount] bigint NOT NULL,
	[Width] int NULL,
	[Height] int NULL,
	[DurationSeconds] int NULL,
	[CipherVersion] tinyint NOT NULL
		constraint [DF-ChatMediaAsset-CipherVersion] default 1
		constraint [CK-ChatMediaAsset-CipherVersion] CHECK ([CipherVersion] IN (0, 1)),
	[CreatedAtUtc] datetime2(7) NOT NULL
		constraint [DF-ChatMediaAsset-CreatedAtUtc] default sysutcdatetime()
)
GO

CREATE NONCLUSTERED INDEX [IX-ChatMediaAsset-ChatGroupId]
	ON [dbo].[ChatMediaAsset]([ChatGroupId]);
GO

CREATE NONCLUSTERED INDEX [IX-ChatMediaAsset-AttachedMessageId]
	ON [dbo].[ChatMediaAsset]([AttachedMessageId])
	WHERE [AttachedMessageId] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX-ChatMediaAsset-Orphans-CreatedAtUtc]
	ON [dbo].[ChatMediaAsset]([CreatedAtUtc])
	WHERE [AttachedMessageId] IS NULL;
GO
