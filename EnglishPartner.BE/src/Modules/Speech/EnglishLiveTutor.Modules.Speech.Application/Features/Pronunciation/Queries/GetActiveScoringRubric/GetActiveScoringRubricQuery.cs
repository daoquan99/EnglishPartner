using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Pronunciation;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetActiveScoringRubric;

public sealed record GetActiveScoringRubricQuery() : IQuery<ScoringRubricEntity?>;
