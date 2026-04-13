using EnglishPartner.Modules.Learning.Domain.StudySessions;
using EnglishPartner.Modules.Learning.Domain.StudySessions.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.StudySessions.Commands.StartSession;

internal sealed class StartStudySessionCommandHandler(
    IStudySessionRepository sessionRepository) : ICommandHandler<StartStudySessionCommand, Guid>
{
    public async Task<Result<Guid>> Handle(StartStudySessionCommand request, CancellationToken cancellationToken)
    {
        // Lazy Closure: Mark any InProgress sessions older than 30 minutes as Abandoned
        var staleThreshold = DateTime.UtcNow.AddMinutes(-30);
        await sessionRepository.AbandonStaleSessionsAsync(request.UserId, staleThreshold, cancellationToken);

        var session = StudySession.Start(request.UserId, request.SessionTitle, request.TotalItems);
        sessionRepository.Add(session);
        return session.Id;
    }
}
