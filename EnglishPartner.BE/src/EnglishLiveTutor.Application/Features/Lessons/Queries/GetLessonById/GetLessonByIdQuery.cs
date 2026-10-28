using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Lessons.Services;

namespace EnglishLiveTutor.Application.Features.Lessons.Queries.GetLessonById;

public sealed record GetLessonByIdQuery(Guid LessonId, bool IsLearner = false) : IQuery<object?>;

public sealed class GetLessonByIdQueryHandler : IQueryHandler<GetLessonByIdQuery, object?>
{
    private readonly ILessonDetailBuilder _lessonDetailBuilder;

    public GetLessonByIdQueryHandler(ILessonDetailBuilder lessonDetailBuilder)
    {
        _lessonDetailBuilder = lessonDetailBuilder;
    }

    public async Task<object?> HandleAsync(GetLessonByIdQuery query, CancellationToken ct = default)
    {
        return await _lessonDetailBuilder.BuildAsync(query.LessonId, query.IsLearner, ct);
    }
}
