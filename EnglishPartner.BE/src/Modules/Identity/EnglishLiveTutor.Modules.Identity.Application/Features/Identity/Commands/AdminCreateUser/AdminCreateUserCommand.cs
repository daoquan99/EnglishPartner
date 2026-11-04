using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.AdminCreateUser;

public sealed record AdminCreateUserCommand(AdminCreateUserRequest Request) : ICommand<UserProfileDto>;
