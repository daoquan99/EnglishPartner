using EnglishPartner.Modules.Curriculum.Domain.Exercises;
using FluentValidation;

namespace EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateAndSaveExercise;

public sealed class GenerateAndSaveExerciseCommandValidator : AbstractValidator<GenerateAndSaveExerciseCommand>
{
    /// <summary>AI-supported exercise types (text-only, no media required).</summary>
    private static readonly HashSet<ExerciseType> SupportedTypes =
    [
        ExerciseType.MultipleChoice,
        ExerciseType.FillInTheBlank,
        ExerciseType.TrueFalse,
        ExerciseType.Translation,
        ExerciseType.SentenceCompletion,
        ExerciseType.WordForm,
        ExerciseType.VerbTense,
        ExerciseType.Paraphrasing,
        ExerciseType.ReorderingWords,
        ExerciseType.RewritingSentences,
        ExerciseType.DialogueCompletion,
        ExerciseType.ShortAnswerQuestions,
        ExerciseType.TrueFalseNotGiven,
        ExerciseType.SynonymAntonym,
        ExerciseType.Collocation,
        ExerciseType.OddOneOut,
        ExerciseType.MultipleResponse,
        ExerciseType.SpellingCorrection,
        ExerciseType.SentenceCorrection,
        ExerciseType.WordStress,
        ExerciseType.QuestionFormation,
        ExerciseType.TopicResponse,
        ExerciseType.Categorization,
    ];

    public GenerateAndSaveExerciseCommandValidator()
    {
        RuleFor(x => x.Context)
            .NotEmpty().WithMessage("Context is required.")
            .MaximumLength(1000);

        RuleFor(x => x.Count)
            .InclusiveBetween(1, 20).WithMessage("Count must be between 1 and 20.");

        RuleFor(x => x.XpReward)
            .InclusiveBetween(0, 1000).WithMessage("XpReward must be between 0 and 1000.");

        RuleFor(x => x.Level)
            .MaximumLength(20)
            .Matches("^(A1|A2|B1|B2|C1|C2)$").When(x => x.Level is not null)
            .WithMessage("Level must be one of: A1, A2, B1, B2, C1, C2.");

        RuleFor(x => x.ExerciseType)
            .Must(t => SupportedTypes.Contains(t))
            .WithMessage($"ExerciseType must be one of the AI-supported types: {string.Join(", ", SupportedTypes)}.");
    }
}
