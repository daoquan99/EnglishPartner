using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Domain;

/// <summary>
/// Domain errors for the Curriculum module.
/// Generic entity-not-found cases delegate to <see cref="CommonErrors.NotFound"/> for consistent message format.
/// Module-specific business errors (duplicates, conflicts) are defined here explicitly.
/// </summary>
public static class CurriculumErrors
{
    public static class Topic
    {
        public static Error NotFound(Guid id) => CommonErrors.NotFound("Topic", id);
    }

    public static class Vocabulary
    {
        public static Error NotFound(Guid id) => CommonErrors.NotFound("Vocabulary", id);
        public static Error WordAlreadyExists(string word) => new("Vocabulary.WordAlreadyExists", $"Vocabulary '{word}' already exists.");
    }

    public static class Grammar
    {
        public static Error NotFound(Guid id) => CommonErrors.NotFound("Grammar", id);
    }

    public static class Conversation
    {
        public static Error NotFound(Guid id) => CommonErrors.NotFound("Conversation", id);
    }

    public static class ExampleSentence
    {
        public static Error NotFound(Guid id) => CommonErrors.NotFound("ExampleSentence", id);
    }

    public static class Exercise
    {
        public static Error NotFound(Guid id) => CommonErrors.NotFound("Exercise", id);
        public static Error UnsupportedAiType(string typeName) => new(
            "Exercise.UnsupportedAiType",
            $"ExerciseType '{typeName}' requires media and cannot be AI-generated. " +
            "Supported AI types: MultipleChoice, FillInTheBlank, TrueFalse, Translation, " +
            "SentenceCompletion, WordForm, VerbTense, Paraphrasing, GapFilling, " +
            "ReorderingWords, DialogueCompletion, ShortAnswerQuestions, TrueFalseNotGiven, RewritingSentences.");
    }

    public static class Language
    {
        public static Error NotFound(string code) => CommonErrors.NotFound("Language", code);
        public static Error AlreadyExists(string code) => new("Language.AlreadyExists", $"Language '{code}' already exists.");
    }
}
