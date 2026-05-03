using EnglishPartner.Modules.Learning.Domain.StudySessions;
using EnglishPartner.Modules.Learning.Domain.StudySessions.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.StudySessions.Commands.StartSession;

internal sealed class StartStudySessionCommandHandler(
    IStudySessionRepository sessionRepository,
    TimeProvider timeProvider,
    ICurrentUserService currentUser) : ICommandHandler<StartStudySessionCommand, Guid>
{
    public async Task<Result<Guid>> Handle(StartStudySessionCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        // Lazy Closure: Mark any InProgress sessions older than 30 minutes as Abandoned
        var staleThreshold = utcNow.AddMinutes(-30);
        await sessionRepository.AbandonStaleSessionsAsync(userId, staleThreshold, cancellationToken);

        var session = StudySession.Start(userId, request.SessionTitle, request.TotalItems, utcNow);
        sessionRepository.Add(session);
        return session.Id;
    }
}
