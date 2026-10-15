using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Prompts.Commands.UpsertCefrLevelPrompt;

public sealed class UpsertCefrLevelPromptValidator : AbstractValidator<UpsertCefrLevelPromptCommand>
{
    public UpsertCefrLevelPromptValidator()
    {
        RuleFor(x => x.CefrLevel).NotEmpty().WithMessage("CefrLevel không được để trống.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
        RuleFor(x => x.Request.DisplayName).NotEmpty().WithMessage("DisplayName không được để trống.");
    }
}
