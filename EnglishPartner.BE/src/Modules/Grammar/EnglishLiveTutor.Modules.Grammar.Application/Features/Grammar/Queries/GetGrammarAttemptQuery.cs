using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.Common;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Services;
using EnglishLiveTutor.Application.Modules.Grammar;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Queries;

public sealed record GetGrammarAttemptQuery(Guid Id) : IQuery<GrammarAttemptDto>;

public sealed class GetGrammarAttemptQueryHandler : IQueryHandler<GetGrammarAttemptQuery, GrammarAttemptDto>
{
    private readonly IGrammarDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IGrammarLearnerContentSanitizer _sanitizer;

    public GetGrammarAttemptQueryHandler(
        IGrammarDbContext db,
        ICurrentUser currentUser,
        IGrammarLearnerContentSanitizer sanitizer)
    {
        _db = db;
        _currentUser = currentUser;
        _sanitizer = sanitizer;
    }

    public async Task<GrammarAttemptDto> HandleAsync(GetGrammarAttemptQuery query, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Authentication required to view grammar attempt.");
        }

        var attempt = await _db.GrammarAttempts
            .AsNoTracking()
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == query.Id, ct);

        if (attempt == null || !string.Equals(attempt.UserId, userId, StringComparison.Ordinal))
        {
            throw new NotFoundException($"Grammar attempt with ID '{query.Id}' was not found.");
        }

        return await GrammarAttemptDtoMapper.MapToDtoAsync(_db, _sanitizer, attempt, userId, ct);
    }
}
