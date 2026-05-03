using EnglishPartner.Modules.AI.Contracts.Services;
using EnglishPartner.Modules.Curriculum.Domain.Grammars;
using EnglishPartner.Modules.Curriculum.Domain.Grammars.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateAndSaveGrammar;

internal sealed class GenerateAndSaveGrammarCommandHandler(
    ICurriculumContentGenerationService generationService,
    IGrammarRepository grammarRepository) : ICommandHandler<GenerateAndSaveGrammarCommand, GenerateAndSaveGrammarResponse>
{
    public async Task<Result<GenerateAndSaveGrammarResponse>> Handle(
        GenerateAndSaveGrammarCommand request, CancellationToken cancellationToken)
    {
        var count = Math.Clamp(request.Count, 1, 10);
        var generated = await generationService.GenerateGrammarAsync(
            request.TargetLanguage, request.Title, request.Level, request.ExampleCount,
            request.TargetLanguageCodes, count, cancellationToken);

        var items = new List<GeneratedGrammarItem>();

        foreach (var dto in generated)
        {
            var grammar = Grammar.Create(
                dto.Title,
                languageCode: request.TargetLanguage,
                dto.Level ?? request.Level,
                dto.Explanation,
                dto.Formula,
                request.TopicId,
                isAiGenerated: true);

            foreach (var exDto in dto.Examples)
            {
                var example = grammar.AddExample(exDto.Sentence, exDto.Note);
                foreach (var (langCode, translation) in exDto.Translations)
                    example.AddTranslation(langCode, translation);
            }

            grammarRepository.Add(grammar);

            items.Add(new GeneratedGrammarItem(
                grammar.Id,
                grammar.Title,
                grammar.Explanation,
                grammar.Formula,
                grammar.Level,
                grammar.IsAiGenerated,
                dto.Examples
                    .Select(e => new GeneratedGrammarExampleItem(e.Sentence, e.Note, e.Translations))
                    .ToList()));
        }

        return new GenerateAndSaveGrammarResponse(items);
    }
}
