using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Repositories;

/// <summary>
/// Persists failed vocabulary generation audit records using an isolated DbContext instance
/// so that recording a failure never flushes partially staged vocabulary entities on the caller's unit of work.
/// </summary>
public sealed class VocabularyGenerationFailureAuditWriter : IVocabularyGenerationFailureAuditWriter
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public VocabularyGenerationFailureAuditWriter(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task RecordFailureAsync(VocabularyGenerationAuditEntity failedAudit, CancellationToken ct = default)
    {
        await using var auditDb = await _dbFactory.CreateDbContextAsync(ct);
        auditDb.VocabularyGenerationAudits.Add(failedAudit);
        await auditDb.SaveChangesAsync(ct);
    }
}
