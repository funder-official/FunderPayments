-- Schema for FunderPaymentsDb – can be run directly on SQL Server instead of EF migrations

IF OBJECT_ID(N'dbo.BillingHistories', N'U') IS NOT NULL
    DROP TABLE dbo.BillingHistories;

IF OBJECT_ID(N'dbo.PaymentTokens', N'U') IS NOT NULL
    DROP TABLE dbo.PaymentTokens;

CREATE TABLE [dbo].[PaymentTokens] (
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [UserId] NVARCHAR(256) NOT NULL,
    [Token] NVARCHAR(256) NOT NULL,
    [ApproveNumber] NVARCHAR(50) NULL,
    [MonthlyAmount] DECIMAL(18,2) NULL,
    [CoinId] INT NOT NULL,
    [IsActive] BIT NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL,
    [UpdatedAt] DATETIME2 NOT NULL,
    CONSTRAINT [PK_PaymentTokens] PRIMARY KEY ([Id])
);

CREATE TABLE [dbo].[BillingHistories] (
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [UserId] NVARCHAR(256) NOT NULL,
    [TokenId] UNIQUEIDENTIFIER NOT NULL,
    [OrderId] NVARCHAR(128) NOT NULL,
    [Amount] DECIMAL(18,2) NOT NULL,
    [CoinId] INT NOT NULL,
    [ResponseCode] INT NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [ApproveNumber] NVARCHAR(50) NULL,
    [InternalDealNumber] NVARCHAR(100) NULL,
    [DealResponse] NVARCHAR(200) NULL,
    [Succeeded] BIT NOT NULL,
    [AttemptedAt] DATETIME2 NOT NULL,
    [RawRequest] NVARCHAR(MAX) NULL,
    [RawResponse] NVARCHAR(MAX) NULL,
    [Error] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_BillingHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BillingHistories_PaymentTokens_TokenId]
        FOREIGN KEY ([TokenId]) REFERENCES [dbo].[PaymentTokens]([Id])
        ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_PaymentTokens_UserId_Token]
    ON [dbo].[PaymentTokens]([UserId], [Token]);

CREATE UNIQUE INDEX [IX_BillingHistories_OrderId]
    ON [dbo].[BillingHistories]([OrderId]);

CREATE INDEX [IX_BillingHistories_TokenId]
    ON [dbo].[BillingHistories]([TokenId]);


