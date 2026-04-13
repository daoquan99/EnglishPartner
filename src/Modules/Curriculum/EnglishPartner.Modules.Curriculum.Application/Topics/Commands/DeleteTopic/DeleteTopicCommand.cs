using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Topics.Commands.DeleteTopic;

public sealed record DeleteTopicCommand(Guid Id) : ICommand;
