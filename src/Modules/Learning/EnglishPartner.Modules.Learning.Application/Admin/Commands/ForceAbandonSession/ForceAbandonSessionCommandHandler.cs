using EnglishPartner.Modules.Learning.Domain;
using EnglishPartner.Modules.Learning.Domain.StudySessions.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.Admin.Commands.ForceAbandonSession;

internal sealed class ForceAbandonSessionCommandHandler(
    IStudySessionRepository sessionRepository) : ICommandHandler<ForceAbandonSessionCommand>
{
    public async Task<Result> Handle(ForceAbandonSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session is null)
            return Result.Failure(LearningErrors.Session.NotFound(request.SessionId));

        session.Abandon();
        sessionRepository.Update(session);
        return Result.Success();
    }
}
