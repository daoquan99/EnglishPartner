using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.Repositories;
using EnglishPartner.SharedKernel.Application.Caching;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.ExampleSentences.Commands.UpdateExampleSentence;

internal sealed class UpdateExampleSentenceCommandHandler(
    IExampleSentenceRepository repository,
    ICacheService cache) : ICommandHandler<UpdateExampleSentenceCommand>
{
    public async Task<Result> Handle(UpdateExampleSentenceCommand request, CancellationToken cancellationToken)
    {
        var sentence = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (sentence is null) 
            return Result.Failure(CurriculumErrors.ExampleSentence.NotFound(request.Id));

        sentence.Update(
            request.Text,
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
        else
        {
            sentence.ClearTranslations();
        }

        repository.Update(sentence);

        await cache.RemoveByPrefixAsync(CacheKeys.Curriculum.ExampleSentencesPrefix(sentence.LanguageCode), cancellationToken);

        return Result.Success();
    }
}
