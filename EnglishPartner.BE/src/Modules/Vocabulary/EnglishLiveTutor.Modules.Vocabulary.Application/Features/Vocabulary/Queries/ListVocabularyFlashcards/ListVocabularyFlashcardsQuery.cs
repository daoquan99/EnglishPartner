using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Vocabulary;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Queries.ListVocabularyFlashcards;

public sealed record ListVocabularyFlashcardsQuery(string? Search = null, string? CefrLevel = null, string? TopicCategory = null, int? Page = null, int? PageSize = null) : IQuery<PagedResultDto<VocabularyFlashcardDto>>;
