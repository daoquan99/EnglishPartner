using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Topics.Commands.CreateTopic;

public sealed record CreateTopicCommand(
    string Title,
    string LanguageCode,
    string? Description,
    string? Level,
    string? ImageUrl,
    int SortOrder) : ICommand<Guid>;
