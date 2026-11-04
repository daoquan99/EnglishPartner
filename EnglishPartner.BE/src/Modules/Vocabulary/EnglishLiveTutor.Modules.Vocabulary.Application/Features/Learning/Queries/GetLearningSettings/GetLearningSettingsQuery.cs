using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;

namespace EnglishLiveTutor.Application.Features.Learning.Queries.GetLearningSettings;

public sealed record GetLearningSettingsQuery(string UserId) : IQuery<UserLearningSettingsDto>;
