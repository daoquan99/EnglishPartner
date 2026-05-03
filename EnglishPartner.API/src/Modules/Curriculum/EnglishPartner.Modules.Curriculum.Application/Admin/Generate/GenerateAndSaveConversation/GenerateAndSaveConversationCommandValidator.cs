using FluentValidation;

namespace EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateAndSaveConversation;

public sealed class GenerateAndSaveConversationCommandValidator : AbstractValidator<GenerateAndSaveConversationCommand>
{
    public GenerateAndSaveConversationCommandValidator()
    {
        RuleFor(x => x.TopicName)
            .NotEmpty().WithMessage("TopicName is required.")
            .MaximumLength(300);

        RuleFor(x => x.Count)
            .InclusiveBetween(1, 10).WithMessage("Count must be between 1 and 10.");

        RuleFor(x => x.LineCount)
            .InclusiveBetween(2, 30).WithMessage("LineCount must be between 2 and 30.");

        RuleFor(x => x.Level)
            .MaximumLength(20)
            .Matches("^(A1|A2|B1|B2|C1|C2)$").When(x => x.Level is not null)
            .WithMessage("Level must be one of: A1, A2, B1, B2, C1, C2.");

        RuleFor(x => x.Scenario).MaximumLength(1000);

        RuleForEach(x => x.TargetLanguageCodes)
            .NotEmpty().MaximumLength(10)
            .When(x => x.TargetLanguageCodes is { Count: > 0 });
    }
}
