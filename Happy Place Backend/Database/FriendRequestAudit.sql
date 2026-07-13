CREATE TABLE [dbo].[FriendRequestAudit]
(
	[Id] uniqueidentifier NOT NULL
		constraint [Pk-FriendRequestAudit] PRIMARY KEY clustered
		constraint [DF-FriendRequestAudit-Id] default newid(),
	[RequesterUserAccountId] uniqueidentifier NOT NULL,
	[AddresseeUserAccountId] uniqueidentifier NOT NULL,
	[RequestedAtUtc] datetime2(0) NOT NULL
		constraint [DF-FriendRequestAudit-RequestedAtUtc] default sysutcdatetime()
)
GO

CREATE NONCLUSTERED INDEX [IX-FriendRequestAudit-RequesterUserAccountId-RequestedAtUtc]
	ON [dbo].[FriendRequestAudit]([RequesterUserAccountId], [RequestedAtUtc]);
GO
