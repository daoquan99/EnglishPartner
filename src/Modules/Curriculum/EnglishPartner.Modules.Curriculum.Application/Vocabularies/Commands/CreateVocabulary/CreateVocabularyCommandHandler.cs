using EnglishPartner.Modules.Curriculum.Domain.Vocabularies;
using EnglishPartner.Modules.Curriculum.Domain.Vocabularies.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Vocabularies.Commands.CreateVocabulary;

internal sealed class CreateVocabularyCommandHandler(
    IVocabularyRepository vocabularyRepository) : ICommandHandler<CreateVocabularyCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateVocabularyCommand request, CancellationToken cancellationToken)
    {
        var vocabulary = Vocabulary.Create(
            request.Word,
            request.Definition,
            request.LanguageCode,
            request.Pronunciation,
            request.PhoneticTranscription,
            request.PartOfSpeech,
            request.Level,
            request.TopicId,
            request.Synonyms);

        vocabularyRepository.Add(vocabulary);
        return vocabulary.Id;
    }
}
