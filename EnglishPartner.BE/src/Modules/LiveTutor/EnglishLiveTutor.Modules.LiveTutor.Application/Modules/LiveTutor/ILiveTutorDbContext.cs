using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Domain.Conversations.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Modules.LiveTutor;

public interface ILiveTutorDbContext : IModuleDbContext
{
    DbSet<ConversationEntity> Conversations { get; }
    DbSet<ChatMessageEntity> Messages { get; }
    DbSet<LanguageCorrectionEntity> Corrections { get; }
}
