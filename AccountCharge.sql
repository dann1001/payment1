IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Invoices] (
    [Id] uniqueidentifier NOT NULL,
    [InvoiceNumber] nvarchar(64) NOT NULL,
    [CustomerId] nvarchar(64) NULL,
    [Status] nvarchar(32) NOT NULL,
    [ExpectedAmount] decimal(38,18) NOT NULL,
    [ExpectedCurrency] nvarchar(16) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ExpiresAt] datetime2 NULL,
    CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [InvoiceAddresses] (
    [Id] uniqueidentifier NOT NULL,
    [InvoiceId] uniqueidentifier NOT NULL,
    [DepositAddress] nvarchar(128) NOT NULL,
    [DepositTag] nvarchar(128) NULL,
    [DepositNetwork] nvarchar(32) NOT NULL,
    [WalletId] int NOT NULL,
    [WalletCurrency] nvarchar(16) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_InvoiceAddresses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InvoiceAddresses_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [InvoiceAppliedDeposits] (
    [Id] uniqueidentifier NOT NULL,
    [InvoiceId] uniqueidentifier NOT NULL,
    [TxHash] nvarchar(128) NOT NULL,
    [DepositAddress] nvarchar(128) NOT NULL,
    [DepositTag] nvarchar(128) NULL,
    [DepositNetwork] nvarchar(32) NOT NULL,
    [Amount] decimal(38,18) NOT NULL,
    [Currency] nvarchar(16) NOT NULL,
    [ObservedAt] datetime2 NOT NULL,
    [WasConfirmed] bit NOT NULL,
    [Confirmations] int NOT NULL,
    [RequiredConfirmations] int NOT NULL,
    CONSTRAINT [PK_InvoiceAppliedDeposits] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InvoiceAppliedDeposits_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_InvoiceAddresses_InvoiceId] ON [InvoiceAddresses] ([InvoiceId]);
GO

CREATE INDEX [IX_InvoiceAppliedDeposits_InvoiceId] ON [InvoiceAppliedDeposits] ([InvoiceId]);
GO

CREATE INDEX [IX_InvoiceAppliedDeposits_ObservedAt] ON [InvoiceAppliedDeposits] ([ObservedAt]);
GO

CREATE UNIQUE INDEX [IX_InvoiceAppliedDeposits_TxHash] ON [InvoiceAppliedDeposits] ([TxHash]);
GO

CREATE INDEX [IX_Invoices_CreatedAt] ON [Invoices] ([CreatedAt]);
GO

CREATE INDEX [IX_Invoices_CustomerId] ON [Invoices] ([CustomerId]);
GO

CREATE UNIQUE INDEX [IX_Invoices_InvoiceNumber] ON [Invoices] ([InvoiceNumber]);
GO

CREATE INDEX [IX_Invoices_Status] ON [Invoices] ([Status]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250917121409_InitialCreate', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250917152058_Init_AccountChargeDb', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP INDEX [IX_InvoiceAppliedDeposits_TxHash] ON [InvoiceAppliedDeposits];
GO

DROP INDEX [IX_InvoiceAppliedDeposits_ObservedAt] ON [InvoiceAppliedDeposits];
DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceAppliedDeposits]') AND [c].[name] = N'ObservedAt');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceAppliedDeposits] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [InvoiceAppliedDeposits] ALTER COLUMN [ObservedAt] datetimeoffset NOT NULL;
CREATE INDEX [IX_InvoiceAppliedDeposits_ObservedAt] ON [InvoiceAppliedDeposits] ([ObservedAt]);
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceAddresses]') AND [c].[name] = N'DepositNetwork');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceAddresses] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [InvoiceAddresses] ALTER COLUMN [DepositNetwork] nvarchar(32) NULL;
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceAddresses]') AND [c].[name] = N'DepositAddress');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceAddresses] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [InvoiceAddresses] ALTER COLUMN [DepositAddress] nvarchar(256) NOT NULL;
GO

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceAddresses]') AND [c].[name] = N'CreatedAt');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceAddresses] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [InvoiceAddresses] ALTER COLUMN [CreatedAt] datetimeoffset NOT NULL;
GO

CREATE INDEX [IX_InvoiceAddresses_CreatedAt] ON [InvoiceAddresses] ([CreatedAt]);
GO

CREATE UNIQUE INDEX [IX_InvoiceAddresses_InvoiceId_DepositAddress_DepositNetwork_DepositTag_WalletId] ON [InvoiceAddresses] ([InvoiceId], [DepositAddress], [DepositNetwork], [DepositTag], [WalletId]) WHERE [DepositNetwork] IS NOT NULL AND [DepositTag] IS NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250920122429_Unify_DateTimeOffset_And_AddressSchema', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250921130829_MakeInvoiceAddressExplicitFk', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE UNIQUE INDEX [IX_InvoiceAppliedDeposits_InvoiceId_TxHash] ON [InvoiceAppliedDeposits] ([InvoiceId], [TxHash]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250922133046_AddUniqueIndex_On_InvoiceAppliedDeposits_TxHash', N'8.0.8');
GO

COMMIT;
GO

