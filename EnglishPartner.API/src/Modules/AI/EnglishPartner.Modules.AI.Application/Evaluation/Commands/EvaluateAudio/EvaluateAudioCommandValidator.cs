using EnglishPartner.Modules.Curriculum.Contracts.Services;
using FluentValidation;

namespace EnglishPartner.Modules.AI.Application.Evaluation.Commands.EvaluateAudio;

/// <summary>
/// Note: AudioStream, FileName, ContentType are validated at the endpoint level (IFormFile checks).
/// This validator covers the logical context fields.
/// </summary>
internal sealed class EvaluateAudioCommandValidator : AbstractValidator<EvaluateAudioCommand>
{
    public EvaluateAudioCommandValidator()
    {
        // Must provide either (ItemId + ItemType) or ItemContext
        RuleFor(x => x)
            .Must(x => (x.ItemId.HasValue && x.ItemType.HasValue) || !string.IsNullOrWhiteSpace(x.ItemContext))
            .WithName("Context")
            .WithMessage("Provide either (ItemId + ItemType) for server-side context, or ItemContext string.");

        // If ItemId is provided, ItemType is required
        When(x => x.ItemId.HasValue, () =>
        {
            RuleFor(x => x.ItemType)
                .NotNull().WithMessage("ItemType is required when ItemId is provided.");
        });

        RuleFor(x => x.ItemContext)
            .MaximumLength(2000)
            .When(x => x.ItemContext is not null);

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => ct.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
            .WithMessage("ContentType must be an audio MIME type (e.g. audio/webm, audio/mp4).");
    }
}
