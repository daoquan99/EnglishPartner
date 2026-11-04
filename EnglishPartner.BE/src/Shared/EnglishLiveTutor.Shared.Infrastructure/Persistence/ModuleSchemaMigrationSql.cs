namespace EnglishLiveTutor.Infrastructure.Persistence;

public static class ModuleSchemaMigrationSql
{
    public const string PreModularBaselineMigrationId = "20261029040750_AddGrammarRoadmap";

    public static string BuildGuardLegacySql(string schema, params string[] tableNames)
    {
        return BuildGuardLegacySql(schema, (IReadOnlyList<string>)tableNames);
    }

    public static string BuildGuardLegacySql(string schema, IReadOnlyList<string> tableNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        ArgumentNullException.ThrowIfNull(tableNames);

        if (tableNames.Count == 0)
        {
            return string.Empty;
        }

        var tableList = string.Join(", ", tableNames.Select(t => $"N'{t}'"));

        return $"""
            IF EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID(N'dbo') AND name IN ({tableList}))
            BEGIN
                IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NULL
                BEGIN
                    THROW 50000, N'{schema}: database has legacy tables in dbo without dbo.__EFMigrationsHistory. Schema version cannot be verified.', 1;
                END

                IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'{PreModularBaselineMigrationId}')
                BEGIN
                    THROW 50000, N'{schema}: database is on an older pre-modular schema. Upgrade with the release before P8 (commit 2bc98e4) first.', 1;
                END
            END
            """;
    }

    public static string BuildTransferTableSql(string schema, string tableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        return $"IF OBJECT_ID(N'[dbo].[{tableName}]', N'U') IS NOT NULL AND OBJECT_ID(N'[{schema}].[{tableName}]', N'U') IS NULL ALTER SCHEMA [{schema}] TRANSFER [dbo].[{tableName}];";
    }
}
