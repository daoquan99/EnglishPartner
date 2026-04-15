using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.Vocabularies.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Vocabularies.Queries.GetVocabularyById;

internal sealed class GetVocabularyByIdQueryHandler(
    IVocabularyRepository vocabularyRepository) : IQueryHandler<GetVocabularyByIdQuery, VocabularyDetailResponse>
{
    public async Task<Result<VocabularyDetailResponse>> Handle(GetVocabularyByIdQuery request, CancellationToken cancellationToken)
    {
        var vocab = await vocabularyRepository.GetByIdAsync(request.Id, cancellationToken);
        if (vocab is null)
            return Result.Failure<VocabularyDetailResponse>(CurriculumErrors.Vocabulary.NotFound(request.Id));

        return new VocabularyDetailResponse(
            vocab.Id,
            vocab.Word,
            vocab.Definition,
            vocab.LanguageCode,
            vocab.Pronunciation,
            vocab.PhoneticTranscription,
            vocab.PartOfSpeech,
            vocab.ImageUrl,
            vocab.AudioUrl,
            vocab.Level,
            vocab.TopicId,
            vocab.Synonyms); // now List<string> directly
    }
}
