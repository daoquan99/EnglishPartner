using EnglishPartner.Modules.Curriculum.Domain.Exercises;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateAndSaveExercise;

/// <summary>
/// Admin command: Ask AI to generate exercises.
/// Supported AI ExerciseTypes: MultipleChoice, FillInTheBlank, TrueFalse, Translation,
/// SentenceCompletion, WordForm, VerbTense, Paraphrasing, GapFilling,
/// ReorderingWords, DialogueCompletion, ShortAnswerQuestions.
/// Unsupported (require media): ListeningComprehension, Speaking, Dictation, PictureDescription.
/// </summary>
public sealed record GenerateAndSaveExerciseCommand(
    string Context,
    ExerciseType ExerciseType = ExerciseType.MultipleChoice,
    string? Level = null,
    Guid? TopicId = null,
    Guid? VocabularyId = null,
    Guid? GrammarId = null,
    int XpReward = 10,
    int Count = 1) : ICommand<GenerateAndSaveExerciseResponse>;

public sealed record GenerateAndSaveExerciseResponse(
    List<GeneratedExerciseItem> Items);

public sealed record GeneratedExerciseItem(
    Guid Id,
    string Title,
    string Instruction,
    string? Content,
    string CorrectAnswer,
    string? Explanation,
    ExerciseType Type,
    bool IsAiGenerated,
    List<GeneratedExerciseOptionItem> Options);

public sealed record GeneratedExerciseOptionItem(
    string Text,
    bool IsCorrect);
