BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261017171520_AddUserLearningSettingsAndPlanEnhancements'
)
BEGIN
    ALTER TABLE [VocabularyReviewAttempts] ADD [ClientRequestId] nvarchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261017171520_AddUserLearningSettingsAndPlanEnhancements'
)
BEGIN
    ALTER TABLE [DailyVocabularyPlans] ADD [TargetCefrLevel] nvarchar(8) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261017171520_AddUserLearningSettingsAndPlanEnhancements'
)
BEGIN
    CREATE TABLE [UserLearningSettings] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] nvarchar(64) NOT NULL,
        [DailyVocabularyTarget] int NOT NULL,
        [CefrLevel] nvarchar(8) NOT NULL DEFAULT N'B1',
        [RowVersion] rowversion NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedBy] nvarchar(64) NULL,
        CONSTRAINT [PK_UserLearningSettings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261017171520_AddUserLearningSettingsAndPlanEnhancements'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_VocabularyReviewAttempts_UserId_ClientRequestId] ON [VocabularyReviewAttempts] ([UserId], [ClientRequestId]) WHERE [ClientRequestId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261017171520_AddUserLearningSettingsAndPlanEnhancements'
)
BEGIN
    CREATE INDEX [IX_DailyVocabularyItems_PlanId_VocabularyId] ON [DailyVocabularyItems] ([PlanId], [VocabularyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261017171520_AddUserLearningSettingsAndPlanEnhancements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserLearningSettings_UserId] ON [UserLearningSettings] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261017171520_AddUserLearningSettingsAndPlanEnhancements'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261017171520_AddUserLearningSettingsAndPlanEnhancements', N'10.0.13');
END;

COMMIT;
GO

