using FluentValidation;

namespace EnglishPartner.Modules.Curriculum.Application.Exercises.Commands.UpdateExercise;

internal sealed class UpdateExerciseCommandValidator : AbstractValidator<UpdateExerciseCommand>
{
    public UpdateExerciseCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(300)
            .WithMessage("Title is required and must be ≤ 300 characters.");

        RuleFor(x => x.Instruction)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("Instruction is required and must be ≤ 500 characters.");

        RuleFor(x => x.CorrectAnswer)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("Correct answer is required.");

        RuleFor(x => x.XpReward)
            .InclusiveBetween(0, 1000)
            .WithMessage("XP reward must be between 0 and 1000.");

        RuleFor(x => x.Level)
            .MaximumLength(10)
            .When(x => x.Level is not null);
    }
}
