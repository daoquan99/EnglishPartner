using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Vocabulary;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Queries.BuildVocabularyGenerationPrompt;

public sealed record BuildVocabularyGenerationPromptQuery(BuildVocabularyGenerationPromptRequest Request)
    : IQuery<VocabularyGenerationPromptDto>;
