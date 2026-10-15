using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Vocabulary.Repositories;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Commands.UpdateVocabulary;

public sealed record UpdateVocabularyCommand(Guid Id, UpsertVocabularyAdminRequest Request) : ICommand<VocabularyFlashcardDto?>;
