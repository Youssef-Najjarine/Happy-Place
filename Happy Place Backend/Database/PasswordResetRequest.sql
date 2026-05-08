CREATE TABLE [dbo].[PasswordResetRequest]
(
    [Id]               INT            IDENTITY (1, 1) NOT NULL,
    [EmailAddress]     NVARCHAR (255) NULL,
    [PhoneNumber]      NVARCHAR (20)  NULL,
    [VerificationCode] NVARCHAR (6)   NOT NULL,
    [ResetToken]       NVARCHAR (MAX) NULL,
    [CreatedAt]        DATETIME2 (7)  NOT NULL,
    [ExpiresAt]        DATETIME2 (7)  NOT NULL,
    [VerifiedAt]       DATETIME2 (7)  NULL,
    [UsedAt]           DATETIME2 (7)  NULL,
    CONSTRAINT [PK_PasswordResetRequest] PRIMARY KEY CLUSTERED ([Id] ASC)
);
