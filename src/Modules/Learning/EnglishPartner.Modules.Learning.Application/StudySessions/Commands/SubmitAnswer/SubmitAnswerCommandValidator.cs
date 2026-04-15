using FluentValidation;

namespace EnglishPartner.Modules.Learning.Application.StudySessions.Commands.SubmitAnswer;

internal sealed class SubmitAnswerCommandValidator : AbstractValidator<SubmitAnswerCommand>
{
    public SubmitAnswerCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.UserAnswer).NotNull().MaximumLength(2000);
        RuleFor(x => x.TimeSpentSeconds).GreaterThanOrEqualTo(0);
        // QualityScore removed: now computed server-side by AI evaluation
    }
}
