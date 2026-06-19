CREATE TABLE [dbo].[HelpAvailability]
(
	[Id] uniqueidentifier NOT NULL
		constraint [Pk-HelpAvailability] PRIMARY KEY clustered
		constraint [DF-HelpAvailability-Id] default newid(),
	[HelperUserAccountId] uniqueidentifier NOT NULL
		constraint [FK-HelpAvailability-HelperUserAccountId]
		FOREIGN KEY REFERENCES [dbo].[UserAccount]([Id]) ON DELETE CASCADE,
	[IsAvailable] bit NOT NULL
		constraint [DF-HelpAvailability-IsAvailable] default 1,
	[LastSeenAtUtc] datetime2(0) NOT NULL
		constraint [DF-HelpAvailability-LastSeenAtUtc] default sysutcdatetime()
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-HelpAvailability-HelperUserAccountId]
	ON [dbo].[HelpAvailability]([HelperUserAccountId]);
GO

CREATE NONCLUSTERED INDEX [IX-HelpAvailability-Available-LastSeenAtUtc]
	ON [dbo].[HelpAvailability]([LastSeenAtUtc])
	WHERE [IsAvailable] = 1;
GO
