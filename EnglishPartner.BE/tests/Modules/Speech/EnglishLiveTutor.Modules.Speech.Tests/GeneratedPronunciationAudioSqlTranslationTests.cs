using EnglishLiveTutor.Infrastructure.Persistence.Modules;
using EnglishLiveTutor.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

/// <summary>
/// Regression: EF Core 10 rewrote <c>AudioData.Length &gt; 0</c> to <c>Any()</c>, which SQL Server cannot
/// translate for varbinary, so /api/pronunciation/synthesize failed. EF InMemory never translates SQL, so this
/// test uses the SQL Server provider against an unreachable endpoint: translation happens before the
/// connection is opened, hence a translation bug surfaces as InvalidOperationException and a correct query
/// reaches the (failing) connection step instead.
/// </summary>
public class GeneratedPronunciationAudioSqlTranslationTests
{
    [Fact]
    public async Task GetValidAudioAsync_QueryTranslatesForSqlServer()
    {
        var options = new DbContextOptionsBuilder<SpeechDbContext>()
            .UseSqlServer("Server=tcp:127.0.0.1,1;Database=TranslationOnly;Connect Timeout=1;TrustServerCertificate=True;")
            .Options;
        await using var db = new SpeechDbContext(options);
        var repository = new GeneratedPronunciationAudioRepository(db);

        var ex = await Record.ExceptionAsync(() => repository.GetValidAudioAsync(
            "word", Guid.NewGuid(), "Aoede", 1.0m, "US", "gemini-3.8-live", "hash"));

        Assert.NotNull(ex);
        Assert.False(
            ex is InvalidOperationException && ex.Message.Contains("could not be translated", StringComparison.Ordinal),
            $"Query was not translatable for SQL Server: {ex.Message}");
    }
}
