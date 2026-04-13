using EnglishPartner.Modules.Curriculum.Domain.Conversations.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Queries.GetConversationById;

internal sealed class GetConversationByIdQueryHandler(
    IConversationRepository conversationRepository) : IQueryHandler<GetConversationByIdQuery, ConversationDetailResponse>
{
    public async Task<Result<ConversationDetailResponse>> Handle(GetConversationByIdQuery request, CancellationToken cancellationToken)
    {
        var conversation = await conversationRepository.GetByIdAsync(request.Id, cancellationToken);
        if (conversation is null) return Result.Failure<ConversationDetailResponse>(new Error("Conversation.NotFound", "Conversation not found."));

        return new ConversationDetailResponse(
            conversation.Id,
            conversation.Title,
            conversation.LanguageCode,
            conversation.Scenario,
            conversation.Level,
            conversation.ImageUrl,
            conversation.TopicId,
            conversation.GrammarId,
            conversation.Lines.Select(l => new ConversationLineDto(
                l.Id, l.Speaker, l.Text, l.Translation, l.AudioUrl, l.SortOrder)).ToList());
    }
}
