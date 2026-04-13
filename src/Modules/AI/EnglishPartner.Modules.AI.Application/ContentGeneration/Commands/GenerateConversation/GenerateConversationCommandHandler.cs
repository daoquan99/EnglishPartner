using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.AI.Application.ContentGeneration.Commands.GenerateConversation;

internal sealed class GenerateConversationCommandHandler(
    IGeminiService geminiService) : ICommandHandler<GenerateConversationCommand, GenerateConversationResponse>
{
    public async Task<Result<GenerateConversationResponse>> Handle(GenerateConversationCommand request, CancellationToken cancellationToken)
    {
        var result = await geminiService.GenerateConversationAsync(request.TopicName, request.Level, cancellationToken);

        var lines = result.Lines.Select(l => new GeneratedLine(l.Speaker, l.Text, l.Translation)).ToList();

        return new GenerateConversationResponse(result.Title, lines);
    }
}
