using System.Reflection;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.AiModels.Contracts;
using EnglishLiveTutor.Application.Features.Identity.Contracts;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;
using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Vocabulary;
using EnglishLiveTutor.Domain.Lessons;
using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Modules.Catalog;
using EnglishLiveTutor.Modules.Grammar;
using EnglishLiveTutor.Modules.Grammar.Contracts;
using EnglishLiveTutor.Modules.Identity;
using EnglishLiveTutor.Modules.Lessons;
using EnglishLiveTutor.Modules.LiveTutor;
using EnglishLiveTutor.Modules.LiveTutor.Contracts;
using EnglishLiveTutor.Modules.Progress;
using EnglishLiveTutor.Modules.Progress.Contracts;
using EnglishLiveTutor.Modules.Speech;
using EnglishLiveTutor.Modules.Vocabulary;

namespace EnglishLiveTutor.Application;

public static class ApplicationAssemblies
{
    public static IReadOnlyList<Assembly> All { get; } =
    [
        typeof(ApplicationAssemblies).Assembly,
        typeof(ICommand<>).Assembly,
        typeof(CatalogModuleServiceCollectionExtensions).Assembly,
        typeof(AiModelConfigEntity).Assembly,
        typeof(IAiModelCatalogReader).Assembly,
        typeof(GrammarModuleServiceCollectionExtensions).Assembly,
        typeof(GrammarStepEntity).Assembly,
        typeof(GrammarContractsAssembly).Assembly,
        typeof(IdentityModuleServiceCollectionExtensions).Assembly,
        typeof(UserEntity).Assembly,
        typeof(IUserDirectory).Assembly,
        typeof(LessonsModuleServiceCollectionExtensions).Assembly,
        typeof(LessonEntity).Assembly,
        typeof(ILessonAiGrader).Assembly,
        typeof(LiveTutorModuleServiceCollectionExtensions).Assembly,
        typeof(ConversationEntity).Assembly,
        typeof(LiveTutorContractsAssembly).Assembly,
        typeof(SpeechModuleServiceCollectionExtensions).Assembly,
        typeof(AudioRecordingEntity).Assembly,
        typeof(IPronunciationScoreReader).Assembly,
        typeof(VocabularyModuleServiceCollectionExtensions).Assembly,
        typeof(VocabularyEntity).Assembly,
        typeof(FallingWordSessionDto).Assembly,
        typeof(ProgressContractsAssembly).Assembly,
        typeof(ProgressModuleServiceCollectionExtensions).Assembly
    ];
}
