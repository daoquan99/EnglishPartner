using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Voices.Repositories;

namespace EnglishLiveTutor.Application.Features.Voices.Queries.GetTeacherVoiceById;

public sealed record GetTeacherVoiceByIdQuery(string Id) : IQuery<TeacherVoiceDto?>;
