using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Vocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.GenerateVocabulary;
using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Domain.Learning.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Learning.Commands.InitializeDailyVocabularyPlan;

public sealed record InitializeDailyVocabularyPlanCommand(
    string UserId,
    InitializeDailyPlanRequest Request
) : ICommand<DailyVocabularyPlanResponseDto>;
