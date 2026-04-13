using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.ExampleSentences.Queries.GetExampleSentences;

internal sealed class GetExampleSentencesQueryHandler(
    IExampleSentenceRepository repository) : IQueryHandler<GetExampleSentencesQuery, List<ExampleSentenceResponse>>
{
    public async Task<Result<List<ExampleSentenceResponse>>> Handle(GetExampleSentencesQuery request, CancellationToken cancellationToken)
    {
        var sentences = new List<EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.ExampleSentence>();

        // Extremely simplified routing, in a real app would have more robust query building
        if (request.VocabularyId.HasValue)
            sentences = await repository.GetByVocabularyIdAsync(request.VocabularyId.Value, cancellationToken);
        else if (request.GrammarId.HasValue)
            sentences = await repository.GetByGrammarIdAsync(request.GrammarId.Value, cancellationToken);
        else if (request.TopicId.HasValue)
            sentences = await repository.GetByTopicIdAsync(request.TopicId.Value, cancellationToken);

        return sentences.Select(s => new ExampleSentenceResponse(
            s.Id, s.Text, s.Translation, s.AudioUrl, s.Note, s.Level)).ToList();
    }
}
