using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences;
using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.Repositories;

internal sealed class ExampleSentenceRepository(CurriculumDbContext dbContext) : IExampleSentenceRepository
{
    public async Task<ExampleSentence?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.ExampleSentences.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<List<ExampleSentence>> GetByVocabularyIdAsync(Guid vocabularyId, CancellationToken cancellationToken)
        => await dbContext.ExampleSentences.Where(x => x.VocabularyId == vocabularyId).ToListAsync(cancellationToken);

    public async Task<List<ExampleSentence>> GetByGrammarIdAsync(Guid grammarId, CancellationToken cancellationToken)
        => await dbContext.ExampleSentences.Where(x => x.GrammarId == grammarId).ToListAsync(cancellationToken);

    public async Task<List<ExampleSentence>> GetByTopicIdAsync(Guid topicId, CancellationToken cancellationToken)
        => await dbContext.ExampleSentences.Where(x => x.TopicId == topicId).ToListAsync(cancellationToken);

    public void Add(ExampleSentence exampleSentence) => dbContext.ExampleSentences.Add(exampleSentence);
    public void Update(ExampleSentence exampleSentence) => dbContext.ExampleSentences.Update(exampleSentence);
    public void Remove(ExampleSentence exampleSentence) => exampleSentence.SoftDelete();
}
