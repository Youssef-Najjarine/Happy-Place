CREATE TABLE [dbo].[HelpOffer]
(
	[Id] uniqueidentifier NOT NULL
		constraint [Pk-HelpOffer] PRIMARY KEY clustered
		constraint [DF-HelpOffer-Id] default newid(),
	[ChatGroupId] uniqueidentifier NOT NULL
		constraint [FK-HelpOffer-ChatGroupId]
		FOREIGN KEY REFERENCES [dbo].[ChatGroup]([Id]) ON DELETE CASCADE,
	[HelperUserAccountId] uniqueidentifier NOT NULL
		constraint [FK-HelpOffer-HelperUserAccountId]
		FOREIGN KEY REFERENCES [dbo].[UserAccount]([Id]),
	[Status] tinyint NOT NULL
		constraint [DF-HelpOffer-Status] default 1
		constraint [CK-HelpOffer-Status] CHECK ([Status] IN (1, 2, 3, 4)),
	[CreatedAtUtc] datetime2(7) NOT NULL
		constraint [DF-HelpOffer-CreatedAtUtc] default sysutcdatetime(),
	[LastSeenAtUtc] datetime2(0) NOT NULL
		constraint [DF-HelpOffer-LastSeenAtUtc] default sysutcdatetime()
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-HelpOffer-ChatGroupId-HelperUserAccountId]
	ON [dbo].[HelpOffer]([ChatGroupId], [HelperUserAccountId]);
GO

CREATE NONCLUSTERED INDEX [IX-HelpOffer-ChatGroupId-Status-CreatedAtUtc]
	ON [dbo].[HelpOffer]([ChatGroupId], [Status], [CreatedAtUtc])
	INCLUDE ([HelperUserAccountId]);
GO
