using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Domain.Vocabulary;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FakeTimeProvider(DateTimeOffset initialUtcNow)
    {
        _utcNow = initialUtcNow;
    }

    public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);

    public override DateTimeOffset GetUtcNow() => _utcNow;
}

public sealed class FakeCurrentUser : ICurrentUser
{
    public string? UserId { get; set; } = "user-alpha";
}

public class PersistenceAndAuditingTests
{
    private static AppDbContext CreateInMemoryContext(
        string dbName,
        TimeProvider timeProvider,
        ICurrentUser currentUser)
    {
        var interceptor = new AuditingSaveChangesInterceptor(timeProvider, currentUser);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(interceptor)
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task AuditingInterceptor_ShouldSetCreatedAuditOnAdd_AndUpdatedAuditOnModifyWithoutOverwritingCreated()
    {
        var t0 = new DateTimeOffset(2026, 10, 15, 2, 0, 0, TimeSpan.Zero);
        var clock = new FakeTimeProvider(t0);
        var user = new FakeCurrentUser { UserId = "creator-user" };
        string dbName = Guid.NewGuid().ToString("N");

        await using var db = CreateInMemoryContext(dbName, clock, user);

        var vocab = VocabularyEntity.Create("resilient", "en", "Seeded", 2650, topicCategory: "Engineering");
        vocab.AddSense("adjective", "Able to recover quickly", "Kiên cường", "B2");
        db.Vocabularies.Add(vocab);
        await db.SaveChangesAsync();

        Assert.Equal(t0, vocab.CreatedAtUtc);
        Assert.Equal("creator-user", vocab.CreatedBy);
        Assert.Null(vocab.UpdatedAtUtc);
        Assert.Null(vocab.UpdatedBy);

        clock.Advance(TimeSpan.FromMinutes(15));
        user.UserId = "editor-user";

        vocab.Deactivate();
        await db.SaveChangesAsync();

        Assert.Equal(t0, vocab.CreatedAtUtc);
        Assert.Equal("creator-user", vocab.CreatedBy);
        Assert.Equal(t0.AddMinutes(15), vocab.UpdatedAtUtc);
        Assert.Equal("editor-user", vocab.UpdatedBy);
    }

    [Fact]
    public async Task SoftDelete_QueryFilter_ShouldExcludeDeletedAggregatesByDefault_AndPreserveChildEntities()
    {
        var t0 = new DateTimeOffset(2026, 10, 15, 3, 0, 0, TimeSpan.Zero);
        var clock = new FakeTimeProvider(t0);
        var user = new FakeCurrentUser { UserId = "moderator-1" };
        string dbName = Guid.NewGuid().ToString("N");

        await using var db = CreateInMemoryContext(dbName, clock, user);

        var vocabActive = VocabularyEntity.Create("scalable");
        vocabActive.AddSense("adjective", "Able to scale", "Dễ mở rộng", "B2");

        var vocabToDelete = VocabularyEntity.Create("itinerary");
        var childSense = vocabToDelete.AddSense("noun", "Travel plan", "Lịch trình", "B1");

        db.Vocabularies.AddRange(vocabActive, vocabToDelete);
        await db.SaveChangesAsync();

        clock.Advance(TimeSpan.FromMinutes(5));
        db.Vocabularies.Remove(vocabToDelete);
        await db.SaveChangesAsync();

        var visibleVocabs = await db.Vocabularies.ToListAsync();
        Assert.Single(visibleVocabs);
        Assert.Equal("scalable", visibleVocabs[0].NormalizedWord);

        var deletedVocab = await db.QueryIncludingDeleted<VocabularyEntity>()
            .Include(v => v.Senses)
            .FirstOrDefaultAsync(v => v.Id == vocabToDelete.Id);

        Assert.NotNull(deletedVocab);
        Assert.True(deletedVocab!.IsDeleted);
        Assert.Equal(t0.AddMinutes(5), deletedVocab.DeletedAtUtc);
        Assert.Equal("moderator-1", deletedVocab.DeletedBy);
        Assert.Single(deletedVocab.Senses);
        Assert.Equal(childSense.Id, deletedVocab.Senses.First().Id);
    }

    [Fact]
    public void EfCoreModelMappings_ShouldPreserveExistingCreatedAtAndUpdatedAtColumnNames()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var user = new FakeCurrentUser();
        using var db = CreateInMemoryContext(Guid.NewGuid().ToString("N"), clock, user);

        var vocabType = db.Model.FindEntityType(typeof(VocabularyEntity))!;
        Assert.Equal("CreatedAt", vocabType.FindProperty(nameof(VocabularyEntity.CreatedAtUtc))!.GetColumnName());
        Assert.Equal("UpdatedAt", vocabType.FindProperty(nameof(VocabularyEntity.UpdatedAtUtc))!.GetColumnName());

        var auditType = db.Model.FindEntityType(typeof(VocabularyGenerationAuditEntity))!;
        Assert.Equal("CreatedAt", auditType.FindProperty(nameof(VocabularyGenerationAuditEntity.CreatedAtUtc))!.GetColumnName());

        var convType = db.Model.FindEntityType(typeof(ConversationEntity))!;
        Assert.Equal("CreatedAt", convType.FindProperty(nameof(ConversationEntity.CreatedAtUtc))!.GetColumnName());
        Assert.Equal("UpdatedAt", convType.FindProperty(nameof(ConversationEntity.UpdatedAtUtc))!.GetColumnName());
    }
}
