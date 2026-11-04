using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.UpdateMyGrammarExercise;

public sealed class UpdateMyGrammarExerciseValidator : AbstractValidator<UpdateMyGrammarExerciseCommand>
{
    public UpdateMyGrammarExerciseValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Exercise Id is required.");

        RuleFor(x => x.Request.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Request.Instructions)
            .MaximumLength(1000).WithMessage("Instructions must not exceed 1000 characters.");

        RuleFor(x => x.Request.Difficulty)
            .InclusiveBetween(1, 3).WithMessage("Difficulty must be between 1 and 3.");

        RuleFor(x => x.Request.Content)
            .Must(c => c.ValueKind == System.Text.Json.JsonValueKind.Object)
            .WithMessage("Content must be a JSON object.");

        RuleFor(x => x.Request.Focus)
            .MaximumLength(200).WithMessage("Focus must not exceed 200 characters.");
    }
}
