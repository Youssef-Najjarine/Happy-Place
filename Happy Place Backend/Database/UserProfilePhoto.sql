CREATE TABLE [dbo].[UserProfilePhoto]
(
	[Id] uniqueidentifier NOT NULL 
		constraint [Pk-UserProfilePhoto] PRIMARY KEY clustered
		constraint [DF-UserProfilePhoto-Id] default newid(),
	[UserAccountId] uniqueidentifier NOT NULL
		constraint [FK-UserProfilePhoto-UserAccount] 
		FOREIGN KEY REFERENCES [dbo].[UserAccount]([Id]) ON DELETE CASCADE,
	[PhotoType] tinyint NOT NULL,
	[ImageBytes] varbinary(MAX) NOT NULL,
	[ContentType] varchar(50) NOT NULL,
	[FileSizeBytes] bigint NOT NULL,
	[WidthPixels] int NOT NULL,
	[HeightPixels] int NOT NULL,
	[UploadedAtUtc] datetime2(0) NOT NULL
		constraint [DF-UserProfilePhoto-UploadedAtUtc] default sysutcdatetime(),
	constraint [CK-UserProfilePhoto-PhotoType] CHECK ([PhotoType] IN (1, 2))
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ-UserProfilePhoto-UserAccountId-PhotoType]
	ON [dbo].[UserProfilePhoto]([UserAccountId], [PhotoType]);
GO
