using System.Text.RegularExpressions;
using System.Reflection;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Modules;
using EnglishLiveTutor.Application.Modules.Catalog;
using EnglishLiveTutor.Application.Modules.Grammar;
using EnglishLiveTutor.Application.Modules.Identity;
using EnglishLiveTutor.Application.Modules.Lessons;
using EnglishLiveTutor.Application.Modules.LiveTutor;
using EnglishLiveTutor.Application.Modules.Speech;
using EnglishLiveTutor.Application.Modules.Vocabulary;
using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Infrastructure;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Entities;
using EnglishLiveTutor.Infrastructure.Persistence.Modules;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnglishLiveTutor.Domain.Tests.Architecture;

public sealed class ModulePersistenceGuardTests
{
    private const string ModelOnlyConnectionString =
        "Server=.;Database=EnglishLiveTutor_P4a_ModelGuard;Trusted_Connection=True;TrustServerCertificate=True;";

    private static readonly ModuleContextSpec[] ModuleContexts =
    [
        new(typeof(IdentityDbContext), typeof(IIdentityDbContext), ModuleSchemas.Identity),
        new(typeof(CatalogDbContext), typeof(ICatalogDbContext), ModuleSchemas.Catalog),
        new(typeof(SpeechDbContext), typeof(ISpeechDbContext), ModuleSchemas.Speech),
        new(typeof(LessonsDbContext), typeof(ILessonsDbContext), ModuleSchemas.Lessons),
        new(typeof(GrammarDbContext), typeof(IGrammarDbContext), ModuleSchemas.Grammar),
        new(typeof(VocabularyDbContext), typeof(IVocabularyDbContext), ModuleSchemas.Vocabulary),
        new(typeof(LiveTutorDbContext), typeof(ILiveTutorDbContext), ModuleSchemas.LiveTutor),
        new(typeof(SharedDbContext), typeof(SharedDbContext), ModuleSchemas.Shared)
    ];

    [Fact]
    public void AppAndModuleModels_MapTheSameEntitiesTablesAndColumns()
    {
        using var app = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(ModelOnlyConnectionString)
                .Options);
        var moduleContexts = CreateSqlModelContexts();
        try
        {
            var appEntities = app.Model.GetEntityTypes()
                .ToDictionary(entityType => entityType.ClrType, EntityMapping.From);
            var moduleEntities = moduleContexts
                .SelectMany(pair => pair.Context.Model.GetEntityTypes())
                .ToDictionary(entityType => entityType.ClrType, EntityMapping.From);

            Assert.Equal(49, appEntities.Count);
            Assert.Equal(49, moduleEntities.Count);
            Assert.Contains(typeof(IdempotencyRecordEntity), moduleEntities.Keys);

            foreach (var (clrType, appMapping) in appEntities)
            {
                Assert.True(
                    moduleEntities.TryGetValue(clrType, out var moduleMapping),
                    $"Module contexts do not map {clrType.FullName}.");
                Assert.Equal(moduleMapping.Schema, appMapping.Schema);
                Assert.Equal(moduleMapping.Table, appMapping.Table);
                Assert.Equal(moduleMapping.Properties, appMapping.Properties);
                Assert.Equal(moduleMapping.Keys, appMapping.Keys);
                Assert.Equal(moduleMapping.Indexes, appMapping.Indexes);
                Assert.Equal(moduleMapping.ForeignKeys, appMapping.ForeignKeys);
            }
        }
        finally
        {
            foreach (var pair in moduleContexts)
            {
                pair.Context.Dispose();
            }
        }
    }

    [Fact]
    public void EachModuleModel_ContainsOnlyItsEntitiesInItsSchema()
    {
        foreach (var pair in CreateSqlModelContexts())
        {
            using var _ = pair.Context;
            Assert.Equal(pair.Spec.Schema, pair.Context.Model.GetDefaultSchema());

            var expectedEntityTypes = pair.Spec.InterfaceType
                .GetProperties()
                .Where(property =>
                    property.PropertyType.IsGenericType &&
                    property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .Select(property => property.PropertyType.GetGenericArguments()[0])
                .ToHashSet();
            var actualEntityTypes = pair.Context.Model.GetEntityTypes()
                .Select(entityType => entityType.ClrType)
                .ToHashSet();

            Assert.Equal(expectedEntityTypes, actualEntityTypes);
            Assert.All(
                pair.Context.Model.GetEntityTypes(),
                entityType => Assert.Equal(
                    pair.Spec.Schema,
                    entityType.GetSchema()));

        }
    }

    [Fact]
    public void EachModuleContext_MatchesItsMigrationSnapshot()
    {
        foreach (var (spec, context) in CreateSqlModelContexts())
        {
            using var _ = context;
            var migrationsAssembly = context.GetService<IMigrationsAssembly>();
            var snapshot = Assert.IsAssignableFrom<ModelSnapshot>(
                migrationsAssembly.ModelSnapshot);
            var snapshotModel = context
                .GetService<IModelRuntimeInitializer>()
                .Initialize(snapshot.Model, designTime: true);
            var designTimeModel = context.GetService<IDesignTimeModel>().Model;
            var differ = context.GetService<IMigrationsModelDiffer>();

            Assert.False(
                differ.HasDifferences(
                    snapshotModel.GetRelationalModel(),
                    designTimeModel.GetRelationalModel()),
                $"{spec.ContextType.Name} has model differences from its migration snapshot.");
        }
    }



        [Fact]
    public void ModuleMigrations_AreSelfContainedAndGuarded()
    {
        foreach (var (spec, migration) in GetModuleMigrations())
        {
            using var context = CreateSqlModelContext(spec);
            var migrationsAssembly = context.GetService<IMigrationsAssembly>();
            var snapshot = Assert.IsAssignableFrom<ModelSnapshot>(
                migrationsAssembly.ModelSnapshot);
            var snapshotModel = context
                .GetService<IModelRuntimeInitializer>()
                .Initialize(snapshot.Model, designTime: true);
            var expectedTableNames = snapshotModel.GetEntityTypes()
                .Select(entityType => entityType.GetTableName()
                    ?? throw new InvalidOperationException(
                        $"Entity {entityType.ClrType.FullName} is not mapped to a table."))
                .Distinct()
                .ToArray();

            var upOperations = RunMigration(migration, up: true);
            var downOperations = RunMigration(migration, up: false);
            var ensureSchema = Assert.Single(upOperations.OfType<EnsureSchemaOperation>());
            var sqlOperations = upOperations.OfType<SqlOperation>().ToArray();

            Assert.Equal(spec.Schema, ensureSchema.Name);
            Assert.Equal(upOperations.Count, 1 + sqlOperations.Length);
            Assert.All(
                upOperations,
                operation => Assert.True(
                    operation is EnsureSchemaOperation or SqlOperation,
                    $"{spec.ContextType.Name} generated unsupported operation " +
                    $"{operation.GetType().Name}."));

            var allUpSql = string.Join("\n", sqlOperations.Select(op => op.Sql));

            foreach (var table in expectedTableNames)
            {
                string transferPattern = $@"\bALTER\s+SCHEMA\s+\[{spec.Schema}\]\s+TRANSFER\s+\[dbo\]\.\[{table}\]";
                int transferMatches = Regex.Matches(allUpSql, transferPattern, RegexOptions.IgnoreCase).Count;
                Assert.True(
                    transferMatches == 1,
                    $"{spec.ContextType.Name} does not contain exactly one transfer for table {table} (found {transferMatches}).");

                string createTablePattern = $@"\bCREATE\s+TABLE\s+\[{spec.Schema}\]\.\[{table}\]";
                int createTableMatches = Regex.Matches(allUpSql, createTablePattern, RegexOptions.IgnoreCase).Count;
                Assert.True(
                    createTableMatches == 1,
                    $"{spec.ContextType.Name} does not contain exactly one CREATE TABLE for table {table} (found {createTableMatches}).");
            }

            Assert.All(
                sqlOperations,
                operation =>
                {
                    string sql = operation.Sql.Trim();
                    Assert.StartsWith("IF ", sql, StringComparison.OrdinalIgnoreCase);

                    if (Regex.IsMatch(sql, @"CREATE\s+TABLE\s+\[", RegexOptions.IgnoreCase))
                    {
                        Assert.Matches(@"^IF\s+OBJECT_ID\(N'\[[^\]]+\]\.\[[^\]]+\]',\s*N'U'\)\s+IS\s+NULL\s+BEGIN\s+EXEC\(N'CREATE TABLE", sql);
                    }
                    else if (Regex.IsMatch(sql, @"CREATE\s+(?:UNIQUE\s+)?INDEX\s+\[", RegexOptions.IgnoreCase))
                    {
                        Assert.Matches(@"^IF\s+NOT\s+EXISTS\s*\(SELECT\s+1\s+FROM\s+sys\.indexes\s+WHERE\s+\[name\]\s*=\s*N'[^']+'\s+AND\s+\[object_id\]\s*=\s*OBJECT_ID\(N'\[[^\]]+\]\.\[[^\]]+\]'\)\)\s+EXEC\(N'CREATE\s+(?:UNIQUE\s+)?INDEX", sql);
                    }
                    else if (Regex.IsMatch(sql, @"FOREIGN\s+KEY\s*\(", RegexOptions.IgnoreCase))
                    {
                        Assert.Matches(@"^IF\s+OBJECT_ID\(N'\[[^\]]+\]\.\[[^\]]+\]',\s*N'F'\)\s+IS\s+NULL\s+EXEC\(N'ALTER\s+TABLE", sql);
                    }
                });

            Assert.Equal(expectedTableNames.Length, downOperations.Count);
            Assert.All(
                downOperations,
                operation => Assert.IsType<RenameTableOperation>(operation));
            Assert.All(
                downOperations.Cast<RenameTableOperation>(),
                rename =>
                {
                    Assert.Equal(spec.Schema, rename.Schema);
                    Assert.Equal("dbo", rename.NewSchema);
                    Assert.Equal(rename.Name, rename.NewName);
                });
            Assert.Equal(
                expectedTableNames.Order(StringComparer.Ordinal),
                downOperations
                    .Cast<RenameTableOperation>()
                    .Select(rename => rename.Name)
                    .Order(StringComparer.Ordinal));
        }
    }

    [Fact]
    public void ModuleMigrationTables_EqualFrozenAppSnapshotTablesExceptIdempotency()
    {
        string[] expectedTables =
        [
            "AiModelCatalog",
            "AudioRecordings",
            "CefrLevelPrompts",
            "ConversationModePrompts",
            "Conversations",
            "Corrections",
            "DailyVocabularyItems",
            "DailyVocabularyPlans",
            "ExerciseAnswers",
            "Exercises",
            "FallingWordGameRounds",
            "FallingWordGameSessions",
            "GeneratedPronunciationAudios",
            "GrammarAttemptAnswers",
            "GrammarAttempts",
            "GrammarExercises",
            "GrammarSteps",
            "IdempotencyRecords",
            "LessonAttempts",
            "LessonRolePlaySessions",
            "LessonRolePlayTurns",
            "Lessons",
            "Messages",
            "Permissions",
            "PronunciationAssessments",
            "PronunciationCriterionScores",
            "RefreshTokens",
            "RolePermissions",
            "Roles",
            "ScoringRubricCriteria",
            "ScoringRubrics",
            "SpeechSpeeds",
            "TeacherVoices",
            "Topics",
            "UserGrammarStepProgress",
            "UserLearningSettings",
            "UserRoles",
            "Users",
            "UserVocabularyProgresses",
            "Vocabularies",
            "VocabularyCollocations",
            "VocabularyExamples",
            "VocabularyGenerationAudits",
            "VocabularyGrammarPatterns",
            "VocabularyPronunciations",
            "VocabularyRelations",
            "VocabularyReviewAttempts",
            "VocabularySenses",
            "VocabularyWordFamilies"
        ];
        var migrationTables = GetModuleMigrations()
            .SelectMany(pair => RunMigration(pair.Migration, up: false))
            .OfType<RenameTableOperation>()
            .Select(rename => rename.Name)
            .ToArray();

        Assert.Equal(49, expectedTables.Length);
        Assert.Equal(expectedTables.Length, migrationTables.Length);
        Assert.Equal(
            expectedTables.Order(StringComparer.Ordinal),
            migrationTables.Order(StringComparer.Ordinal));
        Assert.Equal(migrationTables.Length, migrationTables.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task DatabaseMigrationRunner_MigratesSharedThenEveryModuleInOrder()
    {
        var calls = new List<Type>();
        using var cancellation = new CancellationTokenSource();
        var expectedToken = cancellation.Token;
        var runner = new DatabaseMigrationRunner(
            new RecordingContextMigrator<SharedDbContext>(calls),
            new RecordingContextMigrator<IdentityDbContext>(calls),
            new RecordingContextMigrator<CatalogDbContext>(calls),
            new RecordingContextMigrator<SpeechDbContext>(calls),
            new RecordingContextMigrator<LessonsDbContext>(calls),
            new RecordingContextMigrator<GrammarDbContext>(calls),
            new RecordingContextMigrator<VocabularyDbContext>(calls),
            new RecordingContextMigrator<LiveTutorDbContext>(calls));

        await runner.MigrateAsync(expectedToken);

        Assert.Equal(
            [
                typeof(SharedDbContext),
                typeof(IdentityDbContext),
                typeof(CatalogDbContext),
                typeof(SpeechDbContext),
                typeof(LessonsDbContext),
                typeof(GrammarDbContext),
                typeof(VocabularyDbContext),
                typeof(LiveTutorDbContext)
            ],
            calls);
    }

    [Fact]
    public async Task EnsureSchemaAndSeedAsync_SeedsAfterMigrationRunnerCompletes()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        var catalogOptions = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        var migrationRunner = new PreSeedMigrationRunner(catalogOptions);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDatabaseMigrationRunner>(migrationRunner);
        services.AddDbContext<CatalogDbContext>(opt => opt.UseInMemoryDatabase(databaseName));
        services.AddDbContext<IdentityDbContext>(opt => opt.UseInMemoryDatabase(databaseName));
        services.AddDbContext<SpeechDbContext>(opt => opt.UseInMemoryDatabase(databaseName));
        services.AddDbContext<VocabularyDbContext>(opt => opt.UseInMemoryDatabase(databaseName));
        services.AddDbContext<GrammarDbContext>(opt => opt.UseInMemoryDatabase(databaseName));
        services.AddScoped<IUnitOfWork>(sp => new UnitOfWork(sp.GetRequiredService<GrammarDbContext>()));

        using var provider = services.BuildServiceProvider();
        using var cancellation = new CancellationTokenSource();
        var expectedToken = cancellation.Token;

        await DatabaseSeeder.EnsureSchemaAndSeedAsync(
            provider,
            migrationRunner,
            migrate: true,
            seed: true,
            cancellationToken: expectedToken);

        Assert.True(migrationRunner.WasCalled);
        Assert.Equal(expectedToken, migrationRunner.LastToken);
        await using var verification = new CatalogDbContext(catalogOptions);
        var models = await verification.AiModelCatalog.ToListAsync();
        Assert.Contains(models, model => model.ModelCode == PreSeedMigrationRunner.MarkerModelCode);
        Assert.DoesNotContain(models, model => model.ModelCode == "gemini-3.8-flash");
    }

    [Fact]
    public async Task ScopedContextsShareOneConnectionWhileClosedFactoriesUseIndependentConnections()
    {
        using var provider = CreateInfrastructureProvider();
        using var scope = provider.CreateScope();
        var sharedConnection = scope.ServiceProvider.GetRequiredService<SqlConnection>();
        var moduleInterfaces = ModuleContexts
            .Where(spec => spec.ContextType != typeof(SharedDbContext))
            .Select(spec => spec.InterfaceType)
            .ToArray();
        var scopedContexts = moduleInterfaces
            .Select(type => (DbContext)scope.ServiceProvider.GetRequiredService(type))
            .ToArray();
        var sharedContext = scope.ServiceProvider.GetRequiredService<SharedDbContext>();

        Assert.Equal(7, scopedContexts.Distinct().Count());
        Assert.All(
            scopedContexts,
            context => Assert.Same(sharedConnection, context.Database.GetDbConnection()));
        Assert.All(
            scopedContexts,
            context => Assert.NotNull(context.GetService<ILoggerFactory>()));
        Assert.Same(
            sharedConnection,
            sharedContext.Database.GetDbConnection());
        Assert.All(
            scopedContexts,
            context =>
            {
                var relationalOptions = context.GetService<IDbContextOptions>()
                    .Extensions
                    .OfType<RelationalOptionsExtension>()
                    .Single();
                string expectedSchema = ModuleContexts
                    .Single(spec => context.GetType() == spec.ContextType)
                    .Schema;
                Assert.Equal("__EFMigrationsHistory", relationalOptions.MigrationsHistoryTableName);
                Assert.Equal(expectedSchema, relationalOptions.MigrationsHistoryTableSchema);
            });

        Assert.IsType<ModuleDbContextFactory<IIdentityDbContext, IdentityDbContext>>(
            provider.GetRequiredService<IModuleDbContextFactory<IIdentityDbContext>>());

        var factoryContexts = new List<DbContext>();
        try
        {
            foreach (var spec in ModuleContexts.Where(spec => spec.ContextType != typeof(SharedDbContext)))
            {
                var factoryType = typeof(IModuleDbContextFactory<>)
                    .MakeGenericType(spec.InterfaceType);
                object factory = provider.GetRequiredService(factoryType);
                var createMethod = factoryType.GetMethod(
                    nameof(IModuleDbContextFactory<IModuleDbContext>.CreateDbContextAsync),
                    [typeof(CancellationToken)])
                    ?? throw new InvalidOperationException("Factory method was not found.");
                var task = (Task)createMethod.Invoke(factory, [CancellationToken.None])!;
                await task;
                object context = task.GetType()
                    .GetProperty("Result")!
                    .GetValue(task)!;
                factoryContexts.Add((DbContext)context);
            }

            Assert.All(
                factoryContexts,
                context => Assert.NotSame(
                    sharedConnection,
                    context.Database.GetDbConnection()));
            Assert.Equal(
                factoryContexts.Count,
                factoryContexts
                    .Select(context => context.Database.GetDbConnection())
                    .Distinct()
                    .Count());
        }
        finally
        {
            foreach (var context in factoryContexts)
            {
                await context.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task UnitOfWork_InMemory_SavesChangesFromMultipleModuleContexts()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        await using var identity = new IdentityDbContext(
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options);
        await using var catalog = new CatalogDbContext(
            new DbContextOptionsBuilder<CatalogDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options);
        var unitOfWork = new UnitOfWork([identity, catalog]);

        identity.Users.Add(UserEntity.Create(
            "p4a-user",
            "p4a-user@example.com",
            "P4a User",
            "hash"));
        catalog.AiModelCatalog.Add(new AiModelConfigEntity(
            "P4a Model",
            "p4a-model",
            supportsTextGeneration: true));

        int affectedRows = await unitOfWork.CommitChangesAsync();

        Assert.Equal(2, affectedRows);
        await using var verifyIdentity = new IdentityDbContext(
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options);
        await using var verifyCatalog = new CatalogDbContext(
            new DbContextOptionsBuilder<CatalogDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options);
        Assert.True(await verifyIdentity.Users.AnyAsync(user => user.Username == "p4a-user"));
        Assert.True(await verifyCatalog.AiModelCatalog.AnyAsync(model => model.ModelCode == "p4a-model"));
    }

    [Fact]
    public void AddInfrastructureServices_RegistersEightUnitOfWorkParticipantsInCorrectOrder()
    {
        using var provider = CreateInfrastructureProvider();
        using var scope = provider.CreateScope();

        var participants = scope.ServiceProvider
            .GetRequiredService<IEnumerable<IUnitOfWorkParticipant>>()
            .ToArray();

        Assert.Equal(8, participants.Length);

        // SharedDbContext must be first
        Assert.IsType<SharedDbContext>(participants[0].DbContext);

        // Remaining 7 module DbContexts in registration order
        Assert.IsType<IdentityDbContext>(participants[1].DbContext);
        Assert.IsType<CatalogDbContext>(participants[2].DbContext);
        Assert.IsType<SpeechDbContext>(participants[3].DbContext);
        Assert.IsType<LiveTutorDbContext>(participants[4].DbContext);
        Assert.IsType<LessonsDbContext>(participants[5].DbContext);
        Assert.IsType<GrammarDbContext>(participants[6].DbContext);
        Assert.IsType<VocabularyDbContext>(participants[7].DbContext);

        // UnitOfWork is resolvable from scope
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        Assert.IsType<UnitOfWork>(uow);
    }

    [Fact]
    public void AddModuleDbContext_RegistersExpectedServicesStandalone()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ModelOnlyConnectionString
            })
            .Build();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<EnglishLiveTutor.Infrastructure.Persistence.Interceptors.AuditingSaveChangesInterceptor>();
        services.AddLogging();
        services.AddScoped(_ => new SqlConnection(ModelOnlyConnectionString));

        services.AddModuleDbContext<CatalogDbContext, ICatalogDbContext>(
            configuration,
            ModuleSchemas.Catalog);

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var sharedConnection = scope.ServiceProvider.GetRequiredService<SqlConnection>();
        var scopedContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var scopedInterface = scope.ServiceProvider.GetRequiredService<ICatalogDbContext>();
        var participant = scope.ServiceProvider.GetRequiredService<IUnitOfWorkParticipant>();
        var factory = scope.ServiceProvider.GetRequiredService<IModuleDbContextFactory<ICatalogDbContext>>();

        Assert.Same(scopedContext, scopedInterface);
        Assert.Same(scopedContext, participant.DbContext);
        Assert.Same(sharedConnection, scopedContext.Database.GetDbConnection());
        Assert.IsType<ModuleDbContextFactory<ICatalogDbContext, CatalogDbContext>>(factory);
    }

    [Fact]
    public async Task UnitOfWork_WithParticipants_InMemory_SavesChangesFromMultipleContexts()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        await using var identity = new IdentityDbContext(
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options);
        await using var catalog = new CatalogDbContext(
            new DbContextOptionsBuilder<CatalogDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options);

        IUnitOfWorkParticipant[] participants =
        [
            new UnitOfWorkParticipant<IdentityDbContext>(identity),
            new UnitOfWorkParticipant<CatalogDbContext>(catalog)
        ];

        var unitOfWork = new UnitOfWork(participants);

        identity.Users.Add(UserEntity.Create(
            "p7a-user",
            "p7a-user@example.com",
            "P7a User",
            "hash"));
        catalog.AiModelCatalog.Add(new AiModelConfigEntity(
            "P7a Model",
            "p7a-model",
            supportsTextGeneration: true));

        int affectedRows = await unitOfWork.CommitChangesAsync();

        Assert.Equal(2, affectedRows);
        await using var verifyIdentity = new IdentityDbContext(
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options);
        await using var verifyCatalog = new CatalogDbContext(
            new DbContextOptionsBuilder<CatalogDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options);
        Assert.True(await verifyIdentity.Users.AnyAsync(user => user.Username == "p7a-user"));
        Assert.True(await verifyCatalog.AiModelCatalog.AnyAsync(model => model.ModelCode == "p7a-model"));
    }

    private static IReadOnlyList<(ModuleContextSpec Spec, DbContext Context)> CreateSqlModelContexts()
    {
        return
        [
            (ModuleContexts[0], new IdentityDbContext(
                new DbContextOptionsBuilder<IdentityDbContext>().UseSqlServer(ModelOnlyConnectionString).Options)),
            (ModuleContexts[1], new CatalogDbContext(
                new DbContextOptionsBuilder<CatalogDbContext>().UseSqlServer(ModelOnlyConnectionString).Options)),
            (ModuleContexts[2], new SpeechDbContext(
                new DbContextOptionsBuilder<SpeechDbContext>().UseSqlServer(ModelOnlyConnectionString).Options)),
            (ModuleContexts[3], new LessonsDbContext(
                new DbContextOptionsBuilder<LessonsDbContext>().UseSqlServer(ModelOnlyConnectionString).Options)),
            (ModuleContexts[4], new GrammarDbContext(
                new DbContextOptionsBuilder<GrammarDbContext>().UseSqlServer(ModelOnlyConnectionString).Options)),
            (ModuleContexts[5], new VocabularyDbContext(
                new DbContextOptionsBuilder<VocabularyDbContext>().UseSqlServer(ModelOnlyConnectionString).Options)),
            (ModuleContexts[6], new LiveTutorDbContext(
                new DbContextOptionsBuilder<LiveTutorDbContext>().UseSqlServer(ModelOnlyConnectionString).Options)),
            (ModuleContexts[7], new SharedDbContext(
                new DbContextOptionsBuilder<SharedDbContext>().UseSqlServer(ModelOnlyConnectionString).Options))
        ];
    }

    private static DbContext CreateSqlModelContext(ModuleContextSpec spec)
    {
        return spec.ContextType switch
        {
            Type type when type == typeof(SharedDbContext) => new SharedDbContext(
                new DbContextOptionsBuilder<SharedDbContext>()
                    .UseSqlServer(ModelOnlyConnectionString)
                    .Options),
            Type type when type == typeof(IdentityDbContext) => new IdentityDbContext(
                new DbContextOptionsBuilder<IdentityDbContext>()
                    .UseSqlServer(ModelOnlyConnectionString)
                    .Options),
            Type type when type == typeof(CatalogDbContext) => new CatalogDbContext(
                new DbContextOptionsBuilder<CatalogDbContext>()
                    .UseSqlServer(ModelOnlyConnectionString)
                    .Options),
            Type type when type == typeof(SpeechDbContext) => new SpeechDbContext(
                new DbContextOptionsBuilder<SpeechDbContext>()
                    .UseSqlServer(ModelOnlyConnectionString)
                    .Options),
            Type type when type == typeof(LessonsDbContext) => new LessonsDbContext(
                new DbContextOptionsBuilder<LessonsDbContext>()
                    .UseSqlServer(ModelOnlyConnectionString)
                    .Options),
            Type type when type == typeof(GrammarDbContext) => new GrammarDbContext(
                new DbContextOptionsBuilder<GrammarDbContext>()
                    .UseSqlServer(ModelOnlyConnectionString)
                    .Options),
            Type type when type == typeof(VocabularyDbContext) => new VocabularyDbContext(
                new DbContextOptionsBuilder<VocabularyDbContext>()
                    .UseSqlServer(ModelOnlyConnectionString)
                    .Options),
            Type type when type == typeof(LiveTutorDbContext) => new LiveTutorDbContext(
                new DbContextOptionsBuilder<LiveTutorDbContext>()
                    .UseSqlServer(ModelOnlyConnectionString)
                    .Options),
            _ => throw new InvalidOperationException(
                $"Could not create module context {spec.ContextType.Name}.")
        };
    }

    [Fact]
    public void LaterModuleMigrations_OnlyAddOrDropIndexes()
    {
        // Migrations added after the baseline MoveToModuleSchema / InitialCreate migration
        // are currently restricted to creating or dropping indexes.
        // If a future task intentionally requires other operations (e.g. adding columns/tables),
        // this test should be intentionally relaxed.
        foreach (var spec in ModuleContexts)
        {
            using var context = CreateSqlModelContext(spec);
            var migrationsAssembly = context.GetService<IMigrationsAssembly>();
            string suffix = spec.ContextType == typeof(SharedDbContext)
                ? "_InitialCreate_Shared"
                : $"_MoveToModuleSchema_{spec.ContextType.Name.Replace("DbContext", "")}";

            foreach (var (migrationId, migrationType) in migrationsAssembly.Migrations)
            {
                if (migrationId.EndsWith(suffix, StringComparison.Ordinal))
                {
                    continue;
                }

                var migration = Activator.CreateInstance(migrationType) as Migration
                    ?? throw new InvalidOperationException($"Could not create migration {migrationId}.");

                var upOperations = RunMigration(migration, up: true);
                var downOperations = RunMigration(migration, up: false);

                Assert.NotEmpty(upOperations);
                Assert.All(upOperations, op => Assert.True(
                    op is CreateIndexOperation or DropIndexOperation || (op is AddColumnOperation addCol && addCol.IsNullable),
                    $"Migration {migrationId} in {spec.ContextType.Name} contains non-index Up operation {op.GetType().Name}."));

                Assert.NotEmpty(downOperations);
                Assert.All(downOperations, op => Assert.True(
                    op is CreateIndexOperation or DropIndexOperation or DropColumnOperation || (op is AddColumnOperation addCol && addCol.IsNullable),
                    $"Migration {migrationId} in {spec.ContextType.Name} contains invalid Down operation {op.GetType().Name}."));
            }
        }
    }

    [Fact]
    public void AddCorrectionFeedbackFields_LiveTutor_OnlyContainsNullableAddColumnOperations()
    {
        var spec = ModuleContexts.Single(s => s.ContextType == typeof(LiveTutorDbContext));
        using var context = CreateSqlModelContext(spec);
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();

        var migrationPair = Assert.Single(
            migrationsAssembly.Migrations,
            m => m.Key.EndsWith("_AddCorrectionFeedbackFields_LiveTutor", StringComparison.Ordinal));

        var migration = Activator.CreateInstance(migrationPair.Value) as Migration
            ?? throw new InvalidOperationException("Could not create AddCorrectionFeedbackFields_LiveTutor migration.");

        var upOperations = RunMigration(migration, up: true);
        var downOperations = RunMigration(migration, up: false);

        Assert.NotEmpty(upOperations);
        Assert.All(upOperations, op =>
        {
            var addCol = Assert.IsType<AddColumnOperation>(op);
            Assert.True(addCol.IsNullable, $"Column {addCol.Name} must be nullable.");
        });

        Assert.NotEmpty(downOperations);
        Assert.All(downOperations, op => Assert.IsType<DropColumnOperation>(op));
    }

    private static IReadOnlyList<(ModuleContextSpec Spec, Migration Migration)> GetModuleMigrations()
    {
        var result = new List<(ModuleContextSpec Spec, Migration Migration)>();
        foreach (var spec in ModuleContexts)
        {
            using var context = CreateSqlModelContext(spec);
            var migrationsAssembly = context.GetService<IMigrationsAssembly>();
            string suffix = spec.ContextType == typeof(SharedDbContext)
                ? "_InitialCreate_Shared"
                : $"_MoveToModuleSchema_{spec.ContextType.Name.Replace("DbContext", "")}";

            var baseMigrationPair = Assert.Single(
                migrationsAssembly.Migrations,
                m => m.Key.EndsWith(suffix, StringComparison.Ordinal));
            var migrationType = baseMigrationPair.Value;
            var migration = Activator.CreateInstance(migrationType) as Migration
                ?? throw new InvalidOperationException(
                    $"Could not create migration for {spec.ContextType.Name}.");
            result.Add((spec, migration));
        }

        return result;
    }

    private static IReadOnlyList<MigrationOperation> RunMigration(
        Migration migration,
        bool up)
    {
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        var method = typeof(Migration).GetMethod(
            up ? "Up" : "Down",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                $"Could not find migration {up switch { true => "Up", false => "Down" }} method.");
        method.Invoke(migration, [migrationBuilder]);

        return migrationBuilder.Operations;
    }

    private sealed class RecordingContextMigrator<TContext>(ICollection<Type> calls)
        : IDatabaseContextMigrator<TContext>
        where TContext : DbContext
    {
        public Task MigrateAsync(CancellationToken cancellationToken = default)
        {
            calls.Add(typeof(TContext));
            return Task.CompletedTask;
        }
    }

    private sealed class TestAppDbContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext()
        {
            return new AppDbContext(options);
        }

        public Task<AppDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDbContext());
        }
    }

    private sealed class PreSeedMigrationRunner(DbContextOptions<CatalogDbContext> options)
        : IDatabaseMigrationRunner
    {
        public const string MarkerModelCode = "p4b-preseed-marker";
        public bool WasCalled { get; private set; }
        public CancellationToken LastToken { get; private set; }

        public async Task MigrateAsync(CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            LastToken = cancellationToken;
            await using var db = new CatalogDbContext(options);
            db.AiModelCatalog.Add(new AiModelConfigEntity(
                "P4b Preseed Marker",
                MarkerModelCode,
                supportsTextGeneration: true));
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static ServiceProvider CreateInfrastructureProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ModelOnlyConnectionString
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructureServices(configuration);
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }

    private sealed record ModuleContextSpec(Type ContextType, Type InterfaceType, string Schema);

    private sealed record PropertyMapping(string Column, string? StoreType);

    private sealed record KeyMapping(string Name, string Columns);

    private sealed record IndexMapping(string Name, string Columns, bool IsUnique);

    private sealed record ForeignKeyMapping(
        string Columns,
        string? PrincipalSchema,
        string PrincipalTable,
        string OnDelete);

    private sealed record EntityMapping(
        string? Schema,
        string Table,
        PropertyMapping[] Properties,
        KeyMapping[] Keys,
        IndexMapping[] Indexes,
        ForeignKeyMapping[] ForeignKeys)
    {
        public static EntityMapping From(IEntityType entityType)
        {
            string? schema = entityType.GetSchema();
            string table = entityType.GetTableName()
                ?? throw new InvalidOperationException(
                    $"Entity {entityType.ClrType.FullName} is not mapped to a table.");
            var tableIdentifier = StoreObjectIdentifier.Table(table, schema);
            var properties = entityType.GetProperties()
                .Select(property => new PropertyMapping(
                    property.GetColumnName(tableIdentifier)
                        ?? throw new InvalidOperationException(
                            $"Property {entityType.ClrType.FullName}.{property.Name} is not mapped to a column."),
                    property.GetColumnType(tableIdentifier)))
                .OrderBy(property => property.Column, StringComparer.Ordinal)
                .ToArray();
            var keys = entityType.GetKeys()
                .Select(key => new KeyMapping(
                    key.GetName() ?? string.Empty,
                    string.Join(
                        ",",
                        key.Properties.Select(property =>
                            property.GetColumnName(tableIdentifier)
                            ?? throw new InvalidOperationException(
                                $"Key property {property.Name} is not mapped to a column.")))))
                .OrderBy(key => key.Name, StringComparer.Ordinal)
                .ToArray();
            var indexes = entityType.GetIndexes()
                .Select(index => new IndexMapping(
                    index.GetDatabaseName(tableIdentifier)
                        ?? index.Name
                        ?? string.Empty,
                    string.Join(
                        ",",
                        index.Properties.Select(property =>
                            property.GetColumnName(tableIdentifier)
                            ?? throw new InvalidOperationException(
                                $"Index property {property.Name} is not mapped to a column."))),
                    index.IsUnique))
                .OrderBy(index => index.Name, StringComparer.Ordinal)
                .ThenBy(index => index.Columns, StringComparer.Ordinal)
                .ToArray();
            var foreignKeys = entityType.GetForeignKeys()
                .Select(foreignKey => new ForeignKeyMapping(
                    string.Join(
                        ",",
                        foreignKey.Properties.Select(property =>
                            property.GetColumnName(tableIdentifier)
                            ?? throw new InvalidOperationException(
                                $"Foreign-key property {property.Name} is not mapped to a column."))),
                    foreignKey.PrincipalEntityType.GetSchema(),
                    foreignKey.PrincipalEntityType.GetTableName()
                        ?? throw new InvalidOperationException(
                            $"Principal entity {foreignKey.PrincipalEntityType.ClrType.FullName} " +
                            "is not mapped to a table."),
                    foreignKey.DeleteBehavior.ToString()))
                .OrderBy(foreignKey => foreignKey.Columns, StringComparer.Ordinal)
                .ThenBy(foreignKey => foreignKey.PrincipalSchema, StringComparer.Ordinal)
                .ThenBy(foreignKey => foreignKey.PrincipalTable, StringComparer.Ordinal)
                .ThenBy(foreignKey => foreignKey.OnDelete, StringComparer.Ordinal)
                .ToArray();

            return new EntityMapping(
                schema,
                table,
                properties,
                keys,
                indexes,
                foreignKeys);
        }
    }
}
