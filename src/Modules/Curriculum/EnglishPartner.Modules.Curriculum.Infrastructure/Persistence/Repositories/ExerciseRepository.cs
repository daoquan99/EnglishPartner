using EnglishPartner.Modules.Curriculum.Domain.Exercises;
using EnglishPartner.Modules.Curriculum.Domain.Exercises.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.Repositories;

internal sealed class ExerciseRepository(CurriculumDbContext dbContext) : IExerciseRepository
{
    public async Task<Exercise?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.Exercises.Include(e => e.Options).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<List<Exercise>> GetByTopicIdAsync(Guid topicId, CancellationToken cancellationToken)
        => await dbContext.Exercises.Where(x => x.TopicId == topicId).Include(e => e.Options).ToListAsync(cancellationToken);

    public async Task<List<Exercise>> GetByVocabularyIdAsync(Guid vocabularyId, CancellationToken cancellationToken)
        => await dbContext.Exercises.Where(x => x.VocabularyId == vocabularyId).ToListAsync(cancellationToken);

    public async Task<List<Exercise>> GetByGrammarIdAsync(Guid grammarId, CancellationToken cancellationToken)
        => await dbContext.Exercises.Where(x => x.GrammarId == grammarId).ToListAsync(cancellationToken);

    public async Task<List<Exercise>> GetByLanguageAndLevelAsync(string languageCode, string? level, int count, CancellationToken cancellationToken)
    {
        var query = dbContext.Exercises.Where(x => x.LanguageCode == languageCode);
        if (level is not null)
            query = query.Where(x => x.Level == level);
        return await query.Include(e => e.Options).OrderBy(_ => EF.Functions.Random()).Take(count).ToListAsync(cancellationToken);
    }

    public void Add(Exercise exercise) => dbContext.Exercises.Add(exercise);
    public void Update(Exercise exercise) => dbContext.Exercises.Update(exercise);
    public void Remove(Exercise exercise) => exercise.SoftDelete();
}
