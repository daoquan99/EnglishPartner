using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Domain.Prompts;
using EnglishLiveTutor.Domain.Topics;
using EnglishLiveTutor.Domain.Voices;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Modules.Catalog;

public interface ICatalogDbContext : IModuleDbContext
{
    DbSet<CefrLevelPromptEntity> CefrLevelPrompts { get; }
    DbSet<ConversationModePromptEntity> ConversationModePrompts { get; }
    DbSet<SpeechSpeedEntity> SpeechSpeeds { get; }
    DbSet<TeacherVoiceEntity> TeacherVoices { get; }
    DbSet<TopicEntity> Topics { get; }
    DbSet<AiModelConfigEntity> AiModelCatalog { get; }
}
