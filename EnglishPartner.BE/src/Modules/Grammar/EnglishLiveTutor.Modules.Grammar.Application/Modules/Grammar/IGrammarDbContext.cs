using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Grammar.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Modules.Grammar;

public interface IGrammarDbContext : IModuleDbContext
{
    DbSet<GrammarStepEntity> GrammarSteps { get; }
    DbSet<GrammarExerciseEntity> GrammarExercises { get; }
    DbSet<UserGrammarStepProgressEntity> UserGrammarStepProgress { get; }
    DbSet<GrammarAttemptEntity> GrammarAttempts { get; }
    DbSet<GrammarAttemptAnswerEntity> GrammarAttemptAnswers { get; }
}
