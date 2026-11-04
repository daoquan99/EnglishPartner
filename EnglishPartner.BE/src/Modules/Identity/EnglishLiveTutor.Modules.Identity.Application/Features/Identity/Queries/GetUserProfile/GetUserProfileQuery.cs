using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Queries.GetUserProfile;

public sealed record GetUserProfileQuery(Guid UserId) : IQuery<UserProfileDto?>;
