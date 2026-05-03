using EnglishPartner.Modules.Curriculum.Domain.Exercises;
using EnglishPartner.Modules.Curriculum.Domain.Exercises.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.Repositories;

internal sealed class ExerciseRepository(CurriculumDbContext dbContext) : IExerciseRepository
{
    public async Task<Exercise?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.Exercises.Include(e => e.Options).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<List<Exercise>> GetByTopicIdAsync(Guid topicId, CancellationToken cancellationToken)
        => await dbContext.Exercises.AsNoTracking().Where(x => x.TopicId == topicId).Include(e => e.Options).ToListAsync(cancellationToken);

    public async Task<List<Exercise>> GetByVocabularyIdAsync(Guid vocabularyId, CancellationToken cancellationToken)
        => await dbContext.Exercises.AsNoTracking().Where(x => x.VocabularyId == vocabularyId).ToListAsync(cancellationToken);

    public async Task<List<Exercise>> GetByGrammarIdAsync(Guid grammarId, CancellationToken cancellationToken)
        => await dbContext.Exercises.AsNoTracking().Where(x => x.GrammarId == grammarId).ToListAsync(cancellationToken);

    public async Task<(List<Exercise> Items, int TotalCount)> GetPagedAsync(
        string languageCode, int page, int pageSize, Guid? topicId, Guid? vocabularyId, Guid? grammarId, string? level, ExerciseType? exerciseType, CancellationToken cancellationToken)
    {
        var query = dbContext.Exercises.AsNoTracking().Where(x => x.LanguageCode == languageCode);

        if (topicId.HasValue)
            query = query.Where(x => x.TopicId == topicId.Value);
        
        if (vocabularyId.HasValue)
            query = query.Where(x => x.VocabularyId == vocabularyId.Value);

        if (grammarId.HasValue)
            query = query.Where(x => x.GrammarId == grammarId.Value);

        if (!string.IsNullOrWhiteSpace(level))
            query = query.Where(x => x.Level == level);

        if (exerciseType.HasValue)
            query = query.Where(x => x.Type == exerciseType.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query.Include(e => e.Options)
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<List<Exercise>> GetWithoutOptionsAsync(CancellationToken cancellationToken)
        => await dbContext.Exercises
            .Include(e => e.Options)
            .Where(e => !e.Options.Any())
            .ToListAsync(cancellationToken);

    public void Add(Exercise exercise) => dbContext.Exercises.Add(exercise);
    public void Update(Exercise exercise) => dbContext.Exercises.Update(exercise);
    public void Remove(Exercise exercise, Guid? deletedBy = null, DateTime? deletedAt = null)
        => exercise.SoftDelete(deletedBy, deletedAt);
}

