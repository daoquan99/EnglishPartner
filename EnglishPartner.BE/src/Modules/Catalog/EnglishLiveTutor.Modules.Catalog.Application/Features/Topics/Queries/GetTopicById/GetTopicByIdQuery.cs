using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Topics.Queries.ListTopics;
using EnglishLiveTutor.Domain.Topics.Repositories;

namespace EnglishLiveTutor.Application.Features.Topics.Queries.GetTopicById;

public sealed record GetTopicByIdQuery(Guid Id) : IQuery<TopicDto?>;
