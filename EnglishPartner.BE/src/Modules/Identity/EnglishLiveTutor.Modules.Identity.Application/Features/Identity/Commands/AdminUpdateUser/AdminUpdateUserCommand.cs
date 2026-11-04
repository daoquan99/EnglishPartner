using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.AdminUpdateUser;

public sealed record AdminUpdateUserCommand(Guid UserId, AdminUpdateUserRequest Request) : ICommand<UserProfileDto?>;
