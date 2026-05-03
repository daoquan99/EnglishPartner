using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Curriculum.Domain.Exercises.Entities;

/// <summary>
/// An answer option for a multiple-choice or matching exercise.
/// </summary>
public sealed class ExerciseOption : Entity
{
    public Guid ExerciseId { get; private set; }
    public string Text { get; private set; } = null!;
    public bool IsCorrect { get; private set; }
    public int SortOrder { get; private set; }

    private ExerciseOption() { }

    public static ExerciseOption Create(Guid exerciseId, string text, bool isCorrect, int sortOrder)
    {
        return new ExerciseOption
        {
            Id = Guid.NewGuid(),
            ExerciseId = exerciseId,
            Text = text.Trim(),
            IsCorrect = isCorrect,
            SortOrder = sortOrder
        };
    }
}
