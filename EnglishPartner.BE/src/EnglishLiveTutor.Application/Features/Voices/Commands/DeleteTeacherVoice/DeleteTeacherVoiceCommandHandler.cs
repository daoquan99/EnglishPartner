using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Voices.Repositories;

namespace EnglishLiveTutor.Application.Features.Voices.Commands.DeleteTeacherVoice;

public sealed class DeleteTeacherVoiceCommandHandler : ICommandHandler<DeleteTeacherVoiceCommand, bool>
{
    private readonly ITeacherVoiceRepository _repo;

    public DeleteTeacherVoiceCommandHandler(ITeacherVoiceRepository repo)
    {
        _repo = repo;
    }

    public async Task<bool> HandleAsync(DeleteTeacherVoiceCommand command, CancellationToken ct = default)
    {
        return await _repo.DeleteAsync(command.Id, ct);
    }
}
