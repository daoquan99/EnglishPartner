using EnglishPartner.Modules.AI.Contracts.Services;
using EnglishPartner.Modules.Curriculum.Domain.Conversations;
using EnglishPartner.Modules.Curriculum.Domain.Conversations.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateAndSaveConversation;

internal sealed class GenerateAndSaveConversationCommandHandler(
    ICurriculumContentGenerationService generationService,
    IConversationRepository conversationRepository) : ICommandHandler<GenerateAndSaveConversationCommand, GenerateAndSaveConversationResponse>
{
    public async Task<Result<GenerateAndSaveConversationResponse>> Handle(
        GenerateAndSaveConversationCommand request, CancellationToken cancellationToken)
    {
        var count = Math.Clamp(request.Count, 1, 10);
        var generated = await generationService.GenerateConversationAsync(
            request.TargetLanguage, request.TopicName, request.Level, request.Scenario, request.LineCount,
            request.TargetLanguageCodes, count, cancellationToken);

        var items = new List<GeneratedConversationItem>();

        foreach (var dto in generated)
        {
            var conversation = Conversation.Create(
                dto.Title,
                languageCode: request.TargetLanguage,
                request.Scenario,
                request.Level,
                imageUrl: null,
                request.TopicId,
                request.GrammarId,
                isAiGenerated: true);

            foreach (var lineDto in dto.Lines)
            {
                var line = conversation.AddLine(lineDto.Speaker, lineDto.Text, audioUrl: null);
                foreach (var (langCode, translation) in lineDto.Translations)
                    line.AddTranslation(langCode, translation);
            }

            conversationRepository.Add(conversation);

            items.Add(new GeneratedConversationItem(
                conversation.Id,
                conversation.Title,
                conversation.IsAiGenerated,
                dto.Lines
                    .Select(l => new GeneratedConversationLineItem(l.Speaker, l.Text, l.Translations))
                    .ToList()));
        }

        return new GenerateAndSaveConversationResponse(items);
    }
}
