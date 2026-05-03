using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Topics.Commands.UpdateTopic;

public sealed record UpdateTopicCommand(
    Guid Id,
    string Title,
    string? Description,
    string? Level,
    string? ImageUrl,
    int SortOrder) : ICommand;
