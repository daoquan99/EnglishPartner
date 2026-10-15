using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.AiCatalog;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetPronunciationModels;

public sealed record GetPronunciationModelsQuery() : IQuery<List<AiModelConfigEntity>>;
