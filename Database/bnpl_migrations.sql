-- جداول و دادهٔ اولیهٔ BNPL — اسکریپت idempotent همهٔ Migrationها؛ اجرای دوباره بی‌اثر است.
-- تولید: UPDATE_SQL=1 dotnet test --filter DatabaseScriptTests  (دستی ویرایش نکن)

IF OBJECT_ID(N'[bnpl].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'bnpl') IS NULL EXEC(N'CREATE SCHEMA [bnpl];');
    CREATE TABLE [bnpl].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    IF SCHEMA_ID(N'bnpl') IS NULL EXEC(N'CREATE SCHEMA [bnpl];');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    CREATE TABLE [bnpl].[DiscountCodes] (
        [Id] int NOT NULL IDENTITY,
        [Code] varchar(30) NOT NULL,
        [Title] nvarchar(100) NOT NULL,
        [FeePercent] decimal(7,4) NULL,
        [FixedAmount] bigint NULL,
        [MaxAmount] bigint NULL,
        [ValidFrom] datetime2 NULL,
        [ValidTo] datetime2 NULL,
        [UsageLimit] int NULL,
        [UsedCount] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [Version] int NOT NULL,
        CONSTRAINT [PK_DiscountCodes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    CREATE TABLE [bnpl].[Plans] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(100) NOT NULL,
        [InstallmentCount] int NOT NULL,
        [FeePercent] decimal(7,4) NOT NULL,
        [UpfrontFeePercent] decimal(7,4) NOT NULL,
        [LatePenaltyDailyPercent] decimal(7,4) NOT NULL,
        [IsActive] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [Version] int NOT NULL,
        CONSTRAINT [PK_Plans] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    CREATE TABLE [bnpl].[Settings] (
        [Id] int NOT NULL,
        [IsEnabled] bit NOT NULL,
        [MinAmount] bigint NOT NULL,
        [MaxAmount] bigint NOT NULL,
        [AmountStep] bigint NOT NULL,
        [CompletionHours] int NOT NULL,
        [ActivationMinutes] int NOT NULL,
        [GatewayPaymentMinutes] int NOT NULL,
        [MaxActiveContractsPerCustomer] int NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(300) NULL,
        [Version] int NOT NULL,
        CONSTRAINT [PK_Settings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    CREATE TABLE [bnpl].[Requests] (
        [Id] bigint NOT NULL IDENTITY,
        [ContractNumber] varchar(20) NOT NULL,
        [CustomerId] varchar(64) NOT NULL,
        [MobileNumber] varchar(11) NOT NULL,
        [CustomerName] nvarchar(150) NULL,
        [PlanId] int NOT NULL,
        [PlanTitle] nvarchar(100) NOT NULL,
        [Amount] bigint NOT NULL,
        [InstallmentCount] int NOT NULL,
        [FeePercent] decimal(7,4) NOT NULL,
        [UpfrontFeePercent] decimal(7,4) NOT NULL,
        [LatePenaltyDailyPercent] decimal(7,4) NOT NULL,
        [UpfrontFee] bigint NOT NULL,
        [InstallmentAmount] bigint NOT NULL,
        [TotalFee] bigint NOT NULL,
        [TotalRepayment] bigint NOT NULL,
        [DiscountCodeId] int NULL,
        [DiscountCode] varchar(30) NULL,
        [DiscountAmount] bigint NOT NULL,
        [TokenPrice] bigint NOT NULL,
        [TokenAmount] decimal(18,3) NOT NULL,
        [Status] tinyint NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [TermsAcceptedAt] datetime2 NULL,
        [SignedAt] datetime2 NULL,
        [ActivationDeadline] datetime2 NULL,
        [ActivatedAt] datetime2 NULL,
        [SettledAt] datetime2 NULL,
        [ClosedAt] datetime2 NULL,
        [CloseReason] nvarchar(1000) NULL,
        [TokensLocked] bit NOT NULL,
        [TokensReleased] bit NOT NULL,
        [Version] int NOT NULL,
        CONSTRAINT [PK_Requests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Requests_DiscountCodes_DiscountCodeId] FOREIGN KEY ([DiscountCodeId]) REFERENCES [bnpl].[DiscountCodes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Requests_Plans_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [bnpl].[Plans] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    CREATE TABLE [bnpl].[Installments] (
        [Id] bigint NOT NULL IDENTITY,
        [RequestId] bigint NOT NULL,
        [Number] int NOT NULL,
        [DueDate] date NOT NULL,
        [Amount] bigint NOT NULL,
        [Status] tinyint NOT NULL,
        [PaidAt] datetime2 NULL,
        [PaymentId] bigint NULL,
        [PenaltyPaid] bigint NOT NULL,
        [PenaltyWaived] bit NOT NULL,
        CONSTRAINT [PK_Installments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Installments_Requests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [bnpl].[Requests] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    CREATE TABLE [bnpl].[Payments] (
        [Id] bigint NOT NULL IDENTITY,
        [RequestId] bigint NOT NULL,
        [ContractNumber] varchar(20) NOT NULL,
        [CustomerId] varchar(64) NOT NULL,
        [InstallmentNumber] int NOT NULL,
        [Method] tinyint NOT NULL,
        [Status] tinyint NOT NULL,
        [Amount] bigint NOT NULL,
        [Penalty] bigint NOT NULL,
        [GatewayId] int NULL,
        [GatewayTitle] nvarchar(100) NULL,
        [ProviderReference] varchar(100) NULL,
        [PayUrl] varchar(1000) NULL,
        [ReturnUrl] varchar(500) NULL,
        [TrackingNumber] varchar(100) NULL,
        [TokenAmount] decimal(18,3) NULL,
        [TokenPrice] bigint NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [FailureReason] nvarchar(500) NULL,
        [Description] nvarchar(300) NOT NULL,
        [By] nvarchar(300) NOT NULL,
        [RefundTrackingNumber] varchar(100) NULL,
        [RefundedAt] datetime2 NULL,
        [RefundedBy] nvarchar(300) NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_Requests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [bnpl].[Requests] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    CREATE TABLE [bnpl].[RequestHistory] (
        [Id] bigint NOT NULL IDENTITY,
        [RequestId] bigint NOT NULL,
        [FromStatus] tinyint NULL,
        [ToStatus] tinyint NOT NULL,
        [At] datetime2 NOT NULL,
        [By] nvarchar(300) NOT NULL,
        [Description] nvarchar(1200) NULL,
        CONSTRAINT [PK_RequestHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RequestHistory_Requests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [bnpl].[Requests] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DiscountCodes_Code] ON [bnpl].[DiscountCodes] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Installments_RequestId_Number] ON [bnpl].[Installments] ([RequestId], [Number]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    CREATE INDEX [IX_Installments_Status_DueDate] ON [bnpl].[Installments] ([Status], [DueDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    CREATE INDEX [IX_Payments_CustomerId_Id] ON [bnpl].[Payments] ([CustomerId], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    CREATE INDEX [IX_Payments_ProviderReference] ON [bnpl].[Payments] ([ProviderReference]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    CREATE INDEX [IX_Payments_RequestId_Status] ON [bnpl].[Payments] ([RequestId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    CREATE INDEX [IX_Payments_Status_ExpiresAt] ON [bnpl].[Payments] ([Status], [ExpiresAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    CREATE INDEX [IX_RequestHistory_RequestId] ON [bnpl].[RequestHistory] ([RequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Requests_ContractNumber] ON [bnpl].[Requests] ([ContractNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    CREATE INDEX [IX_Requests_CustomerId_Status] ON [bnpl].[Requests] ([CustomerId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    CREATE INDEX [IX_Requests_DiscountCodeId] ON [bnpl].[Requests] ([DiscountCodeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    CREATE INDEX [IX_Requests_MobileNumber] ON [bnpl].[Requests] ([MobileNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    CREATE INDEX [IX_Requests_PlanId] ON [bnpl].[Requests] ([PlanId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    CREATE INDEX [IX_Requests_Status_ExpiresAt] ON [bnpl].[Requests] ([Status], [ExpiresAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'IsEnabled', N'MinAmount', N'MaxAmount', N'AmountStep', N'CompletionHours', N'ActivationMinutes', N'GatewayPaymentMinutes', N'MaxActiveContractsPerCustomer', N'Version') AND [object_id] = OBJECT_ID(N'[bnpl].[Settings]'))
        SET IDENTITY_INSERT [bnpl].[Settings] ON;
    EXEC(N'INSERT INTO [bnpl].[Settings] ([Id], [IsEnabled], [MinAmount], [MaxAmount], [AmountStep], [CompletionHours], [ActivationMinutes], [GatewayPaymentMinutes], [MaxActiveContractsPerCustomer], [Version])
    VALUES (1, CAST(1 AS bit), CAST(50000000 AS bigint), CAST(5000000000 AS bigint), CAST(10000000 AS bigint), 24, 30, 20, 3, 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'IsEnabled', N'MinAmount', N'MaxAmount', N'AmountStep', N'CompletionHours', N'ActivationMinutes', N'GatewayPaymentMinutes', N'MaxActiveContractsPerCustomer', N'Version') AND [object_id] = OBJECT_ID(N'[bnpl].[Settings]'))
        SET IDENTITY_INSERT [bnpl].[Settings] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Title', N'InstallmentCount', N'FeePercent', N'UpfrontFeePercent', N'LatePenaltyDailyPercent', N'IsActive', N'SortOrder', N'Version') AND [object_id] = OBJECT_ID(N'[bnpl].[Plans]'))
        SET IDENTITY_INSERT [bnpl].[Plans] ON;
    EXEC(N'INSERT INTO [bnpl].[Plans] ([Title], [InstallmentCount], [FeePercent], [UpfrontFeePercent], [LatePenaltyDailyPercent], [IsActive], [SortOrder], [Version])
    VALUES (N''۱۲ قسط'', 12, 22.0, 4.0, 1.0, CAST(1 AS bit), 1, 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Title', N'InstallmentCount', N'FeePercent', N'UpfrontFeePercent', N'LatePenaltyDailyPercent', N'IsActive', N'SortOrder', N'Version') AND [object_id] = OBJECT_ID(N'[bnpl].[Plans]'))
        SET IDENTITY_INSERT [bnpl].[Plans] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [bnpl].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029142909_InitialBnpl'
)
BEGIN
    INSERT INTO [bnpl].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261029142909_InitialBnpl', N'8.0.10');
END;
GO

COMMIT;
GO

