using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.AiCatalog;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Queries.GetVocabularyGenerationModels;

public sealed record GetVocabularyGenerationModelsQuery() : IQuery<List<AiModelConfigEntity>>;
