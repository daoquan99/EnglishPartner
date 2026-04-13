using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Domain;

/// <summary>
/// All domain errors for the Curriculum module.
/// </summary>
public static class CurriculumErrors
{
    public static class Topic
    {
        public static Error NotFound(Guid id) => new("Topic.NotFound", $"Topic with ID '{id}' was not found.");
    }

    public static class Vocabulary
    {
        public static Error NotFound(Guid id) => new("Vocabulary.NotFound", $"Vocabulary with ID '{id}' was not found.");
        public static Error WordAlreadyExists(string word) => new("Vocabulary.WordAlreadyExists", $"Vocabulary '{word}' already exists.");
    }

    public static class Grammar
    {
        public static Error NotFound(Guid id) => new("Grammar.NotFound", $"Grammar with ID '{id}' was not found.");
    }

    public static class Exercise
    {
        public static Error NotFound(Guid id) => new("Exercise.NotFound", $"Exercise with ID '{id}' was not found.");
    }

    public static class Language
    {
        public static Error NotFound(string code) => new("Language.NotFound", $"Language with code '{code}' was not found.");
        public static Error AlreadyExists(string code) => new("Language.AlreadyExists", $"Language '{code}' already exists.");
    }
}
