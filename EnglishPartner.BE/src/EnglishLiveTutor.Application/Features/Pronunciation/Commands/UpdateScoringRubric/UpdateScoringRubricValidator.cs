using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Commands.UpdateScoringRubric;

public sealed class UpdateScoringRubricValidator : AbstractValidator<UpdateScoringRubricCommand>
{
    public UpdateScoringRubricValidator()
    {
        RuleFor(x => x.CodeOrId).NotEmpty().WithMessage("CodeOrId không được để trống.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
        RuleFor(x => x.Request.Criteria)
            .NotEmpty().WithMessage("Rubric phải có ít nhất 1 tiêu chí chấm điểm.");
    }
}
