using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Prompts;
using EnglishLiveTutor.Domain.Prompts.Repositories;

namespace EnglishLiveTutor.Application.Features.Prompts.Commands.UpsertSpeechSpeed;

public sealed record UpsertSpeechSpeedCommand(UpsertSpeechSpeedRequest Request) : ICommand<SpeechSpeedOptionDto>;

public sealed class UpsertSpeechSpeedCommandHandler(ISpeechSpeedRepository repository) : ICommandHandler<UpsertSpeechSpeedCommand, SpeechSpeedOptionDto>
{
    public async Task<SpeechSpeedOptionDto> HandleAsync(UpsertSpeechSpeedCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        if (req.IsDefault)
        {
            var currentDefaults = await repository.ListDefaultsAsync(ct);
            foreach (var d in currentDefaults)
            {
                d.UpdateDetails(d.Code, d.SpeedValue, d.Label, d.DescriptionVi, d.RecommendedCefrRange, false, d.IsActive, d.DisplayOrder);
            }
        }

        var existing = await repository.GetByCodeIncludingDeletedAsync(req.Code, ct);

        if (existing is not null)
        {
            existing.Restore();
            existing.UpdateDetails(
                req.Code,
                req.Value,
                req.Label,
                req.DescriptionVi,
                req.RecommendedCefrRange,
                req.IsDefault,
                req.IsActive,
                req.DisplayOrder);
            await repository.SaveChangesAsync(ct);
            return new SpeechSpeedOptionDto(
                existing.Id, existing.Code, existing.SpeedValue, existing.Label,
                existing.DescriptionVi, existing.RecommendedCefrRange, existing.IsDefault,
                existing.IsActive, existing.DisplayOrder);
        }

        var entity = new SpeechSpeedEntity(
            req.Code,
            req.Value,
            req.Label,
            req.DescriptionVi,
            req.RecommendedCefrRange,
            req.IsDefault,
            req.IsActive,
            req.DisplayOrder);

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);

        return new SpeechSpeedOptionDto(
            entity.Id, entity.Code, entity.SpeedValue, entity.Label,
            entity.DescriptionVi, entity.RecommendedCefrRange, entity.IsDefault,
            entity.IsActive, entity.DisplayOrder);
    }
}
