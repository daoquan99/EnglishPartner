using EnglishPartner.Modules.Curriculum.Application.Topics.Queries.GetTopics;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Topics.Queries.GetTopicById;

public sealed record GetTopicByIdQuery(Guid Id) : IQuery<TopicResponse>;
