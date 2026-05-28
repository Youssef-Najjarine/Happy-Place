CREATE TABLE [dbo].[ContactChangeAudit]
(
	[Id] uniqueidentifier NOT NULL 
		constraint [Pk-ContactChangeAudit] PRIMARY KEY clustered
		constraint [DF-ContactChangeAudit-Id] default newid(),
	[UserAccountId] uniqueidentifier NOT NULL
		constraint [Fk-ContactChangeAudit-UserAccountId-UserAccount-Id]
		FOREIGN KEY REFERENCES [dbo].[UserAccount]([Id]) ON DELETE CASCADE,
	[EventType] varchar(40) NOT NULL,
	[OldValue] nvarchar(255) NULL,
	[NewValue] nvarchar(255) NOT NULL,
	[EventAtUtc] datetime2(0) NOT NULL
		constraint [DF-ContactChangeAudit-EventAtUtc] default sysutcdatetime(),
)
GO

CREATE NONCLUSTERED INDEX [IX-ContactChangeAudit-UserAccountId-EventType-EventAtUtc]
	ON [dbo].[ContactChangeAudit]([UserAccountId], [EventType], [EventAtUtc] DESC);
GO
