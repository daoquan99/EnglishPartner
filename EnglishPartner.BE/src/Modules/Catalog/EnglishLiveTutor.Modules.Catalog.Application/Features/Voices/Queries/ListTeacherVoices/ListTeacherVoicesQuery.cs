using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Voices.Repositories;

namespace EnglishLiveTutor.Application.Features.Voices.Queries.ListTeacherVoices;

public sealed record ListTeacherVoicesQuery(
    bool OnlyEnabled = true,
    bool OnlyFeatured = false,
    string? Gender = null,
    string? Search = null,
    int? Page = null,
    int? PageSize = null
) : IQuery<PagedResultDto<TeacherVoiceDto>>;
