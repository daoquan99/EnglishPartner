using FluentValidation;

namespace EnglishLiveTutor.Application.Features.AiModels.Commands.UpdateAiModelConfig;

public sealed class UpdateAiModelConfigValidator : AbstractValidator<UpdateAiModelConfigCommand>
{
    public UpdateAiModelConfigValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id không được để trống.");
        RuleFor(x => x.Request.ModelCode).NotEmpty().WithMessage("Mã Model (ModelCode) không được để trống.");
    }
}
