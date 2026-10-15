using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Vocabulary;
using EnglishLiveTutor.Domain.Vocabulary.Repositories;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Commands.CreateVocabulary;

public sealed record CreateVocabularyCommand(UpsertVocabularyAdminRequest Request) : ICommand<VocabularyFlashcardDto>;
