using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Voices;
using EnglishLiveTutor.Domain.Voices.Repositories;

namespace EnglishLiveTutor.Application.Features.Voices.Commands.UpsertTeacherVoice;

public sealed record UpsertTeacherVoiceCommand(
    string Id,
    UpsertTeacherVoiceRequest Request
) : ICommand<TeacherVoiceDto>;

public sealed class UpsertTeacherVoiceCommandHandler : ICommandHandler<UpsertTeacherVoiceCommand, TeacherVoiceDto>
{
    private readonly ITeacherVoiceRepository _voiceRepo;

    public UpsertTeacherVoiceCommandHandler(ITeacherVoiceRepository voiceRepo)
    {
        _voiceRepo = voiceRepo;
    }

    public async Task<TeacherVoiceDto> HandleAsync(UpsertTeacherVoiceCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var existing = await _voiceRepo.GetByIdAsync(command.Id, ct);
        if (existing is null)
        {
            existing = new TeacherVoiceEntity(
                id: command.Id,
                name: req.Name,
                displayName: req.DisplayName,
                type: req.Type,
                gender: req.Gender,
                persona: req.Persona,
                description: req.Description,
                label: req.Label,
                style: req.Style,
                isFeatured: req.IsFeatured,
                isEnabled: req.IsEnabled,
                displayOrder: req.DisplayOrder);
            await _voiceRepo.AddAsync(existing, ct);
        }
        else
        {
            existing.UpdateVoice(
                req.DisplayName,
                req.Label,
                req.Gender,
                req.Persona,
                req.Style,
                req.Description,
                req.IsFeatured,
                req.IsEnabled,
                req.DisplayOrder);
        }

        await _voiceRepo.SaveChangesAsync(ct);

        return new TeacherVoiceDto(
            existing.Id,
            existing.Name,
            existing.DisplayName,
            existing.Label,
            existing.Type,
            existing.Gender,
            existing.Persona,
            existing.Style,
            existing.Description,
            existing.IsFeatured,
            existing.IsEnabled,
            existing.DisplayOrder);
    }
}
