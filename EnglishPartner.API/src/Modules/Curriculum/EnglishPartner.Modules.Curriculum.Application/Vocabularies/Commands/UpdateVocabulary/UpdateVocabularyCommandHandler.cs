using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.Vocabularies.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;
using EnglishPartner.SharedKernel.Application.Caching;

namespace EnglishPartner.Modules.Curriculum.Application.Vocabularies.Commands.UpdateVocabulary;

internal sealed class UpdateVocabularyCommandHandler(
    IVocabularyRepository vocabularyRepository,
    ICacheService cache) : ICommandHandler<UpdateVocabularyCommand>
{
    public async Task<Result> Handle(UpdateVocabularyCommand request, CancellationToken cancellationToken)
    {
        var vocabulary = await vocabularyRepository.GetByIdAsync(request.Id, cancellationToken);
        if (vocabulary is null)
            return Result.Failure(CurriculumErrors.Vocabulary.NotFound(request.Id));

        vocabulary.Update(
            request.Word,
            request.Definition,
            request.Pronunciation,
            request.PhoneticTranscription,
            request.PartOfSpeech,
            request.ImageUrl,
            request.AudioUrl,
            request.Level,
            request.TopicId,
            request.Synonyms);

        vocabularyRepository.Update(vocabulary);
        
        await cache.RemoveByPrefixAsync(CacheKeys.Curriculum.VocabulariesPrefix(vocabulary.LanguageCode), cancellationToken);
        
        return Result.Success();
    }
}
