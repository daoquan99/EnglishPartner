using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences;
using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.Repositories;
using EnglishPartner.SharedKernel.Application.Caching;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.ExampleSentences.Commands.CreateExampleSentence;

internal sealed class CreateExampleSentenceCommandHandler(
    IExampleSentenceRepository repository,
    ICacheService cache) : ICommandHandler<CreateExampleSentenceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateExampleSentenceCommand request, CancellationToken cancellationToken)
    {
        var sentence = ExampleSentence.Create(
            request.Text,
            request.LanguageCode,
            request.AudioUrl,
            request.Note,
            request.Level,
            request.TopicId,
            request.VocabularyId,
            request.GrammarId);

        if (request.Translations is { Count: > 0 })
        {
            foreach (var (langCode, text) in request.Translations)
                sentence.AddTranslation(langCode, text);
        }

        repository.Add(sentence);
        
        await cache.RemoveByPrefixAsync(CacheKeys.Curriculum.ExampleSentencesPrefix(request.LanguageCode), cancellationToken);
        
        return sentence.Id;
    }
}
