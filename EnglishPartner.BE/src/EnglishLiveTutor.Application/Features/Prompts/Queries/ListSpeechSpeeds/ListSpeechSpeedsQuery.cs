using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Prompts.Queries.ListSpeechSpeeds;

public sealed record ListSpeechSpeedsQuery(bool? OnlyActive = null, string? Search = null, int? Page = null, int? PageSize = null) : IQuery<PagedResultDto<SpeechSpeedOptionDto>>;
