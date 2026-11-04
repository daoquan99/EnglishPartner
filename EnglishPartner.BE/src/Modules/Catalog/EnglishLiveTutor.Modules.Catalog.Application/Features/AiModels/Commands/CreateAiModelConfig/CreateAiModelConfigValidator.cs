using FluentValidation;

namespace EnglishLiveTutor.Application.Features.AiModels.Commands.CreateAiModelConfig;

public sealed class CreateAiModelConfigValidator : AbstractValidator<CreateAiModelConfigCommand>
{
    public CreateAiModelConfigValidator()
    {
        RuleFor(x => x.Request.ModelCode)
            .NotEmpty().WithMessage("Mã Model (ModelCode) không được để trống.");
    }
}
