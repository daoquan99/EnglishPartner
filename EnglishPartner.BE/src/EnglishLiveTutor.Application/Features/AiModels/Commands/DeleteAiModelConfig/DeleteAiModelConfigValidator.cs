using FluentValidation;

namespace EnglishLiveTutor.Application.Features.AiModels.Commands.DeleteAiModelConfig;

public sealed class DeleteAiModelConfigValidator : AbstractValidator<DeleteAiModelConfigCommand>
{
    public DeleteAiModelConfigValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id không được để trống.");
    }
}
