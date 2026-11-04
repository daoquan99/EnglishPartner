using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.BuildVocabularyGenerationPrompt;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Commands.GenerateVocabulary;

public sealed record GenerateVocabularyCommand(string UserId, GenerateVocabularyRequest Request)
    : ICommand<List<VocabularyFlashcardDto>>;
