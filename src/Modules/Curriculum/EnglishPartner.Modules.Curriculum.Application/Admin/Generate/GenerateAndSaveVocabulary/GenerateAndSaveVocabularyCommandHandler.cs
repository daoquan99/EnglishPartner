using EnglishPartner.Modules.AI.Contracts.Services;
using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences;
using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.Repositories;
using EnglishPartner.Modules.Curriculum.Domain.Vocabularies;
using EnglishPartner.Modules.Curriculum.Domain.Vocabularies.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;
using EnglishPartner.SharedKernel.Application.Caching;

namespace EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateAndSaveVocabulary;

internal sealed class GenerateAndSaveVocabularyCommandHandler(
    ICurriculumContentGenerationService generationService,
    IVocabularyRepository vocabularyRepository,
    IExampleSentenceRepository exampleSentenceRepository,
    ICacheService cache) : ICommandHandler<GenerateAndSaveVocabularyCommand, GenerateAndSaveVocabularyResponse>
{
    public async Task<Result<GenerateAndSaveVocabularyResponse>> Handle(
        GenerateAndSaveVocabularyCommand request, CancellationToken cancellationToken)
    {
        var count = Math.Clamp(request.Count, 1, 20);
        var generated = await generationService.GenerateVocabularyAsync(request.TargetLanguage, request.WordHint, count, cancellationToken);

        var items = new List<GeneratedVocabularyItem>();
        var hasChanges = false;

        foreach (var dto in generated)
        {
            var vocabulary = Vocabulary.Create(
                dto.Word,
                dto.Definition,
                languageCode: request.TargetLanguage,
                dto.Pronunciation,
                dto.PhoneticTranscription,
                dto.PartOfSpeech,
                dto.Level ?? request.Level,
                request.TopicId,
                dto.Synonyms,
                isAiGenerated: true);

            vocabularyRepository.Add(vocabulary);
            hasChanges = true;

            // Auto-generate example sentences if requested
            var exampleIds = new List<Guid>();
            if (request.AutoGenerateExamples && request.ExampleCount > 0)
            {
                var exampleCount = Math.Clamp(request.ExampleCount, 1, 10);
                var targetLangs = request.ExampleTargetLanguageCodes;
                var sentences = await generationService.GenerateExampleSentencesAsync(
                    request.TargetLanguage, dto.Word, exampleCount, request.Level, targetLangs, cancellationToken);

                foreach (var s in sentences)
                {
                    var sentence = ExampleSentence.Create(
                        s.Text,
                        languageCode: request.TargetLanguage,
                        audioUrl: null,
                        s.Note,
                        request.Level,
                        request.TopicId,
                        vocabularyId: vocabulary.Id,
                        grammarId: null,
                        isAiGenerated: true);

                    foreach (var (langCode, translation) in s.Translations)
                        sentence.AddTranslation(langCode, translation);

                    exampleSentenceRepository.Add(sentence);
                    exampleIds.Add(sentence.Id);
                }
            }

            items.Add(new GeneratedVocabularyItem(
                vocabulary.Id,
                vocabulary.Word,
                vocabulary.Definition,
                vocabulary.Pronunciation,
                vocabulary.PhoneticTranscription,
                vocabulary.PartOfSpeech,
                vocabulary.Level,
                vocabulary.Synonyms,
                vocabulary.IsAiGenerated,
                exampleIds));
        }

        if (hasChanges)
        {
            await cache.RemoveByPrefixAsync(CacheKeys.Curriculum.VocabulariesPrefix(request.TargetLanguage), cancellationToken);
        }

        return new GenerateAndSaveVocabularyResponse(items);
    }
}
