using FluentValidation;

namespace EnglishPartner.Modules.Curriculum.Application.Exercises.Commands.CreateExercise;

public sealed class CreateExerciseCommandValidator : AbstractValidator<CreateExerciseCommand>
{
    public CreateExerciseCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Instruction).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.CorrectAnswer).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.LanguageCode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Level).MaximumLength(20);
        RuleFor(x => x.Explanation).MaximumLength(2000);
        RuleFor(x => x.Content).MaximumLength(5000);
    }
}
