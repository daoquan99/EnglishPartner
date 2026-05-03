using EnglishPartner.Modules.AI.Contracts.Services;
using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences;
using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateAndSaveExampleSentences;

internal sealed class GenerateAndSaveExampleSentencesCommandHandler(
    ICurriculumContentGenerationService generationService,
    IExampleSentenceRepository exampleSentenceRepository) : ICommandHandler<GenerateAndSaveExampleSentencesCommand, GenerateAndSaveExampleSentencesResponse>
{
    public async Task<Result<GenerateAndSaveExampleSentencesResponse>> Handle(
        GenerateAndSaveExampleSentencesCommand request, CancellationToken cancellationToken)
    {
        var count = Math.Clamp(request.Count, 1, 20);

        var generated = await generationService.GenerateExampleSentencesAsync(
            request.TargetLanguage, request.Context, count, request.Level, request.TargetLanguageCodes, cancellationToken);

        var savedItems = new List<GeneratedExampleSentenceItem>();

        foreach (var dto in generated)
        {
            var sentence = ExampleSentence.Create(
                dto.Text,
                languageCode: request.TargetLanguage,
                audioUrl: null,
                dto.Note,
                request.Level,
                request.TopicId,
                request.VocabularyId,
                request.GrammarId,
                isAiGenerated: true);

            foreach (var (langCode, translation) in dto.Translations)
                sentence.AddTranslation(langCode, translation);

            exampleSentenceRepository.Add(sentence);
            savedItems.Add(new GeneratedExampleSentenceItem(
                sentence.Id, sentence.Text, sentence.Note,
                sentence.IsAiGenerated, dto.Translations));
        }

        return new GenerateAndSaveExampleSentencesResponse(savedItems);
    }
}
