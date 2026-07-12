CREATE TABLE [dbo].[ChatGroupMember]
(
	[Id] uniqueidentifier NOT NULL
		constraint [Pk-ChatGroupMember] PRIMARY KEY clustered
		constraint [DF-ChatGroupMember-Id] default newid(),
	[ChatGroupId] uniqueidentifier NOT NULL
		constraint [FK-ChatGroupMember-ChatGroupId]
		FOREIGN KEY REFERENCES [dbo].[ChatGroup]([Id]) ON DELETE CASCADE,
	[UserAccountId] uniqueidentifier NOT NULL
		constraint [FK-ChatGroupMember-UserAccountId]
		FOREIGN KEY REFERENCES [dbo].[UserAccount]([Id]),
	[MemberRole] tinyint NOT NULL
		constraint [CK-ChatGroupMember-MemberRole] CHECK ([MemberRole] IN (1, 2)),
	[Status] tinyint NOT NULL
		constraint [CK-ChatGroupMember-Status] CHECK ([Status] IN (1, 2)),
	[JoinedAtUtc] datetime2(0) NOT NULL
		constraint [DF-ChatGroupMember-JoinedAtUtc] default sysutcdatetime(),
	[LastReadSequence] bigint NOT NULL
		constraint [DF-ChatGroupMember-LastReadSequence] default 0,
	[LastTypingAtUtc] datetime2(7) NULL
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-ChatGroupMember-ChatGroupId-UserAccountId]
	ON [dbo].[ChatGroupMember]([ChatGroupId], [UserAccountId]);
GO

CREATE NONCLUSTERED INDEX [IX-ChatGroupMember-UserAccountId]
	ON [dbo].[ChatGroupMember]([UserAccountId]);
GO
