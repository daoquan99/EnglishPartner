using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Topics.Queries.ListTopics;
using EnglishLiveTutor.Domain.Topics;
using EnglishLiveTutor.Domain.Topics.Repositories;

namespace EnglishLiveTutor.Application.Features.Topics.Commands.UpsertTopic;

public sealed record UpsertTopicCommand(Guid? Id, UpsertTopicRequest Request) : ICommand<TopicDto>;

public sealed class UpsertTopicCommandHandler : ICommandHandler<UpsertTopicCommand, TopicDto>
{
    private readonly ITopicRepository _topicRepo;

    public UpsertTopicCommandHandler(ITopicRepository topicRepo)
    {
        _topicRepo = topicRepo;
    }

    public async Task<TopicDto> HandleAsync(UpsertTopicCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Code))
            throw new InvalidOperationException("Mã chủ đề (Code) không được để trống.");
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new InvalidOperationException("Tên chủ đề (Name) không được để trống.");

        TopicEntity? existing = null;
        if (command.Id.HasValue && command.Id.Value != Guid.Empty)
        {
            existing = await _topicRepo.GetByIdAsync(command.Id.Value, ct);
        }
        existing ??= await _topicRepo.GetByCodeAsync(req.Code, ct);

        if (existing is not null)
        {
            existing.UpdateDetails(
                req.Name,
                req.DisplayNameVi,
                req.Category,
                req.ModeCode,
                req.SuggestedCefrLevel,
                req.SupportsConversation,
                req.SupportsVocabulary,
                req.Description,
                req.IsFeatured,
                req.IsActive,
                req.DisplayOrder);

            var saved = await _topicRepo.UpsertAsync(existing, ct);
            return ListTopicsQueryHandler.MapToDto(saved);
        }

        var created = new TopicEntity(
            code: req.Code,
            name: req.Name,
            displayNameVi: req.DisplayNameVi,
            category: req.Category,
            modeCode: req.ModeCode,
            suggestedCefrLevel: req.SuggestedCefrLevel,
            supportsConversation: req.SupportsConversation,
            supportsVocabulary: req.SupportsVocabulary,
            description: req.Description,
            isFeatured: req.IsFeatured,
            isActive: req.IsActive,
            displayOrder: req.DisplayOrder,
            id: command.Id.HasValue && command.Id.Value != Guid.Empty ? command.Id.Value : null);

        var result = await _topicRepo.UpsertAsync(created, ct);
        return ListTopicsQueryHandler.MapToDto(result);
    }
}
