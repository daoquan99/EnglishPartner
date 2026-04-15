using EnglishPartner.Modules.AI.Contracts.Services;
using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences;
using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.Repositories;
using EnglishPartner.Modules.Curriculum.Domain.Vocabularies;
using EnglishPartner.Modules.Curriculum.Domain.Vocabularies.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateAndSaveVocabulary;

internal sealed class GenerateAndSaveVocabularyCommandHandler(
    ICurriculumContentGenerationService generationService,
    IVocabularyRepository vocabularyRepository,
    IExampleSentenceRepository exampleSentenceRepository) : ICommandHandler<GenerateAndSaveVocabularyCommand, GenerateAndSaveVocabularyResponse>
{
    public async Task<Result<GenerateAndSaveVocabularyResponse>> Handle(
        GenerateAndSaveVocabularyCommand request, CancellationToken cancellationToken)
    {
        var count = Math.Clamp(request.Count, 1, 20);
        var generated = await generationService.GenerateVocabularyAsync(request.WordHint, count, cancellationToken);

        var items = new List<GeneratedVocabularyItem>();

        foreach (var dto in generated)
        {
            var vocabulary = Vocabulary.Create(
                dto.Word,
                dto.Definition,
                languageCode: "en",
                dto.Pronunciation,
                dto.PhoneticTranscription,
                dto.PartOfSpeech,
                dto.Level ?? request.Level,
                request.TopicId,
                dto.Synonyms,
                isAiGenerated: true);

            vocabularyRepository.Add(vocabulary);

            // Auto-generate example sentences if requested
            var exampleIds = new List<Guid>();
            if (request.AutoGenerateExamples && request.ExampleCount > 0)
            {
                var exampleCount = Math.Clamp(request.ExampleCount, 1, 10);
                var targetLangs = request.ExampleTargetLanguageCodes;
                var sentences = await generationService.GenerateExampleSentencesAsync(
                    dto.Word, exampleCount, request.Level, targetLangs, cancellationToken);

                foreach (var s in sentences)
                {
                    var sentence = ExampleSentence.Create(
                        s.Text,
                        languageCode: "en",
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

        return new GenerateAndSaveVocabularyResponse(items);
    }
}
