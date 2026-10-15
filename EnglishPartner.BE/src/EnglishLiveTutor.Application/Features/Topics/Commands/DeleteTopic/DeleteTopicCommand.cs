using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Topics.Repositories;

namespace EnglishLiveTutor.Application.Features.Topics.Commands.DeleteTopic;

public sealed record DeleteTopicCommand(Guid Id) : ICommand<bool>;
