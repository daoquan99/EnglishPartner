using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Lessons.Enums;

namespace EnglishLiveTutor.Application.Features.Lessons.Queries.GetLessonExerciseTypes;

public sealed record GetLessonExerciseTypesQuery : IQuery<IReadOnlyList<LessonExerciseTypeCatalogDto>>;

public sealed class GetLessonExerciseTypesQueryHandler : IQueryHandler<GetLessonExerciseTypesQuery, IReadOnlyList<LessonExerciseTypeCatalogDto>>
{
    private static readonly IReadOnlyList<LessonExerciseTypeCatalogDto> Catalog = new List<LessonExerciseTypeCatalogDto>
    {
        new(nameof(ExerciseType.Flashcards), (int)ExerciseType.Flashcards, "Learn", "Thẻ từ vựng", "Flashcards", nameof(GradingMode.SelfAssessment), false),
        new(nameof(ExerciseType.SpeakingPractice), (int)ExerciseType.SpeakingPractice, "Skills", "Luyện nói theo chủ đề", "Speaking Practice", nameof(GradingMode.AiRubric), true),
        new(nameof(ExerciseType.MultipleChoice), (int)ExerciseType.MultipleChoice, "Practice", "Trắc nghiệm", "Multiple Choice", nameof(GradingMode.Deterministic), false),
        new(nameof(ExerciseType.FillInTheBlanks), (int)ExerciseType.FillInTheBlanks, "Practice", "Điền vào chỗ trống", "Fill in the Blanks", nameof(GradingMode.Deterministic), false),
        new(nameof(ExerciseType.Matching), (int)ExerciseType.Matching, "Practice", "Nối cặp tương ứng", "Matching Pairs", nameof(GradingMode.Deterministic), false),
        new(nameof(ExerciseType.SpellingPractice), (int)ExerciseType.SpellingPractice, "Practice", "Luyện chính tả", "Spelling Practice", nameof(GradingMode.Deterministic), false),
        new(nameof(ExerciseType.SentenceUnscramble), (int)ExerciseType.SentenceUnscramble, "Practice", "Sắp xếp câu", "Sentence Unscramble", nameof(GradingMode.Deterministic), false),
        new(nameof(ExerciseType.ErrorCorrection), (int)ExerciseType.ErrorCorrection, "Practice", "Tìm & sửa lỗi sai", "Error Correction", nameof(GradingMode.Deterministic), false),
        new(nameof(ExerciseType.Translation), (int)ExerciseType.Translation, "Practice", "Dịch thuật", "Translation", nameof(GradingMode.AiRubric), false),
        new(nameof(ExerciseType.TrueFalse), (int)ExerciseType.TrueFalse, "Practice", "Đúng / Sai", "True or False", nameof(GradingMode.Deterministic), false),
        new(nameof(ExerciseType.ReadingComprehension), (int)ExerciseType.ReadingComprehension, "Skills", "Đọc hiểu văn bản", "Reading Comprehension", nameof(GradingMode.Composite), false),
        new(nameof(ExerciseType.ListeningComprehension), (int)ExerciseType.ListeningComprehension, "Skills", "Nghe hiểu", "Listening Comprehension", nameof(GradingMode.Composite), true),
        new(nameof(ExerciseType.Dictation), (int)ExerciseType.Dictation, "Skills", "Nghe chép chính tả", "Dictation", nameof(GradingMode.Deterministic), true),
        new(nameof(ExerciseType.Shadowing), (int)ExerciseType.Shadowing, "Skills", "Nói nhại theo audio", "Shadowing Practice", nameof(GradingMode.AiRubric), true),
        new(nameof(ExerciseType.ConversationRolePlay), (int)ExerciseType.ConversationRolePlay, "Skills", "Hội thoại tình huống", "Role-Play Conversation", nameof(GradingMode.AiRubric), true),
        new(nameof(ExerciseType.WritingPractice), (int)ExerciseType.WritingPractice, "Skills", "Luyện viết đoạn văn", "Writing Practice", nameof(GradingMode.AiRubric), false)
    }.AsReadOnly();

    public Task<IReadOnlyList<LessonExerciseTypeCatalogDto>> HandleAsync(GetLessonExerciseTypesQuery query, CancellationToken ct = default)
    {
        return Task.FromResult(Catalog);
    }
}
