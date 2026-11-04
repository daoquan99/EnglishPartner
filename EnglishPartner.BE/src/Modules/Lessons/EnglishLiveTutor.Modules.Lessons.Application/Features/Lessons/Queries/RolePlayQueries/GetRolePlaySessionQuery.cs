using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Lessons.Commands.RolePlayOperations;
using EnglishLiveTutor.Domain.Lessons.Repositories;

namespace EnglishLiveTutor.Application.Features.Lessons.Queries.RolePlayQueries;

public sealed record RolePlaySessionDetailDto(
    Guid SessionId,
    string Status,
    int CurrentTurnIndex,
    string AiRole,
    string Scenario,
    int MaxTurns,
    List<RolePlayTranscriptItemDto> Transcript);

public sealed record GetRolePlaySessionQuery(Guid SessionId) : IQuery<RolePlaySessionDetailDto>;

public sealed class GetRolePlaySessionQueryHandler : IQueryHandler<GetRolePlaySessionQuery, RolePlaySessionDetailDto>
{
    private readonly ILessonRolePlayRepository _repository;
    private readonly ICurrentUser _currentUser;

    public GetRolePlaySessionQueryHandler(ILessonRolePlayRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<RolePlaySessionDetailDto> HandleAsync(GetRolePlaySessionQuery query, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ForbiddenException("Authentication required to view role-play session.");
        }

        var session = await _repository.GetByIdWithTurnsAsync(query.SessionId, ct);
        if (session == null || session.IsDeleted)
        {
            throw new NotFoundException("Role-play session not found.");
        }

        if (!string.Equals(session.UserId, userId, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotFoundException("Role-play session not found.");
        }

        var transcript = session.Turns
            .OrderBy(t => t.SequenceIndex)
            .Select(t => new RolePlayTranscriptItemDto(t.Role, t.Message))
            .ToList();

        return new RolePlaySessionDetailDto(
            SessionId: session.Id,
            Status: session.Status,
            CurrentTurnIndex: session.CurrentTurnIndex,
            AiRole: session.AiRole,
            Scenario: session.Scenario,
            MaxTurns: session.MaxTurns,
            Transcript: transcript);
    }
}
