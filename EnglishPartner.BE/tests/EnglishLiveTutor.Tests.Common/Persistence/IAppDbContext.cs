using EnglishLiveTutor.Application.Modules.Catalog;
using EnglishLiveTutor.Application.Modules.Grammar;
using EnglishLiveTutor.Application.Modules.Identity;
using EnglishLiveTutor.Application.Modules.Lessons;
using EnglishLiveTutor.Application.Modules.LiveTutor;
using EnglishLiveTutor.Application.Modules.Speech;
using EnglishLiveTutor.Application.Modules.Vocabulary;

namespace EnglishLiveTutor.Application.Common.Interfaces;

public interface IAppDbContext :
    IIdentityDbContext,
    ICatalogDbContext,
    ISpeechDbContext,
    ILessonsDbContext,
    IGrammarDbContext,
    IVocabularyDbContext,
    ILiveTutorDbContext
{
}

public interface IAppDbContextFactory
{
    Task<IAppDbContext> CreateDbContextAsync(CancellationToken ct = default);
}
