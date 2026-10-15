using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Prompts;
using EnglishLiveTutor.Domain.Prompts.Repositories;

namespace EnglishLiveTutor.Application.Features.Prompts.Commands.UpdateSpeechSpeed;

public sealed class UpdateSpeechSpeedCommandHandler(ISpeechSpeedRepository repository) : ICommandHandler<UpdateSpeechSpeedCommand, SpeechSpeedOptionDto?>
{
    public async Task<SpeechSpeedOptionDto?> HandleAsync(UpdateSpeechSpeedCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var id = command.Id;
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null) return null;

        if (req.IsDefault)
        {
            var others = (await repository.ListDefaultsAsync(ct)).Where(x => x.Id != id);
            foreach (var o in others)
            {
                o.UpdateDetails(o.Code, o.SpeedValue, o.Label, o.DescriptionVi, o.RecommendedCefrRange, false, o.IsActive, o.DisplayOrder);
            }
        }

        entity.UpdateDetails(
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
            entity.Id, entity.Code, entity.SpeedValue, entity.Label,
            entity.DescriptionVi, entity.RecommendedCefrRange, entity.IsDefault,
            entity.IsActive, entity.DisplayOrder);
    }
}
