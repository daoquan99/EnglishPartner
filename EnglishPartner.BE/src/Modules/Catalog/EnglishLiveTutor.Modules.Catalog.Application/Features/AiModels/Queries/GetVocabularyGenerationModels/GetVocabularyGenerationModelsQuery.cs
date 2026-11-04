using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.AiCatalog;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.AiModels.Queries.GetVocabularyGenerationModels;

public sealed record GetVocabularyGenerationModelsQuery() : IQuery<List<AiModelConfigEntity>>;
