using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Conversations.Commands.CreateConversation;
using EnglishLiveTutor.Application.Features.Conversations.Commands.DeleteConversation;
using EnglishLiveTutor.Application.Features.Conversations.Commands.UpdateConversationSettings;
using EnglishLiveTutor.Application.Features.Conversations.Queries.GetConversationHistory;
using EnglishLiveTutor.Application.Features.Conversations.Queries.ListConversations;
using EnglishLiveTutor.Application.Features.Identity.Commands.AssignUserRoles;
using EnglishLiveTutor.Application.Features.Identity.Commands.CreatePermission;
using EnglishLiveTutor.Application.Features.Identity.Commands.CreateRole;
using EnglishLiveTutor.Application.Features.Identity.Commands.Login;
using EnglishLiveTutor.Application.Features.Identity.Commands.RefreshToken;
using EnglishLiveTutor.Application.Features.Identity.Commands.RegisterUser;
using EnglishLiveTutor.Application.Features.Identity.Commands.RevokeRefreshToken;
using EnglishLiveTutor.Application.Features.Identity.Commands.UpdateRolePermissions;
using EnglishLiveTutor.Application.Features.Identity.Queries.GetUserProfile;
using EnglishLiveTutor.Application.Features.Identity.Queries.ListPermissions;
using EnglishLiveTutor.Application.Features.Identity.Queries.ListRoles;
using EnglishLiveTutor.Application.Features.Identity.Queries.ListUsers;
using EnglishLiveTutor.Application.Features.Learning.Commands.InitializeDailyVocabularyPlan;
using EnglishLiveTutor.Application.Features.Learning.Commands.SubmitVocabularyReview;
using EnglishLiveTutor.Application.Features.Learning.Queries.GetDailyVocabularyPlan;
using EnglishLiveTutor.Application.Features.Learning.Queries.GetLearningHistory;
using EnglishLiveTutor.Application.Features.Learning.Queries.GetLearningProgress;
using EnglishLiveTutor.Application.Features.Prompts.Commands.UpsertCefrLevelPrompt;
using EnglishLiveTutor.Application.Features.Prompts.Commands.UpsertConversationModePrompt;
using EnglishLiveTutor.Application.Features.Prompts.Queries.ListCefrLevelPrompts;
using EnglishLiveTutor.Application.Features.Prompts.Queries.ListConversationModePrompts;
using EnglishLiveTutor.Application.Features.Prompts.Queries.PreviewSystemPrompt;
using EnglishLiveTutor.Application.Features.Pronunciation.Commands.AssessPronunciation;
using EnglishLiveTutor.Application.Features.Pronunciation.Commands.UpdateScoringRubric;
using EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetActiveScoringRubric;
using EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetPronunciationHistory;
using EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetPronunciationModels;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.GenerateVocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.GetVocabularyFlashcard;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.GetVocabularyGenerationModels;
using EnglishLiveTutor.Application.Features.Voices.Commands.UpsertTeacherVoice;
using EnglishLiveTutor.Application.Features.Voices.Queries.GetTeacherVoiceById;
using EnglishLiveTutor.Application.Features.Topics.Commands.DeleteTopic;
using EnglishLiveTutor.Application.Features.Topics.Commands.UpsertTopic;
using EnglishLiveTutor.Application.Features.Topics.Queries.GetTopicById;
using EnglishLiveTutor.Application.Features.Topics.Queries.ListTopics;
using EnglishLiveTutor.Application.Features.Voices.Queries.ListTeacherVoices;
using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Domain.Prompts;
using EnglishLiveTutor.Domain.Pronunciation;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishLiveTutor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Identity & Auth
        services.AddTransient<RegisterUserCommandHandler>();
        services.AddTransient<ICommandHandler<RegisterUserCommand, AuthResponseDto>>(sp => sp.GetRequiredService<RegisterUserCommandHandler>());

        services.AddTransient<LoginCommandHandler>();
        services.AddTransient<ICommandHandler<LoginCommand, AuthResponseDto>>(sp => sp.GetRequiredService<LoginCommandHandler>());

        services.AddTransient<RefreshTokenCommandHandler>();
        services.AddTransient<ICommandHandler<RefreshTokenCommand, AuthResponseDto>>(sp => sp.GetRequiredService<RefreshTokenCommandHandler>());

        services.AddTransient<RevokeRefreshTokenCommandHandler>();
        services.AddTransient<ICommandHandler<RevokeRefreshTokenCommand, bool>>(sp => sp.GetRequiredService<RevokeRefreshTokenCommandHandler>());

        services.AddTransient<AssignUserRolesCommandHandler>();
        services.AddTransient<ICommandHandler<AssignUserRolesCommand, UserProfileDto>>(sp => sp.GetRequiredService<AssignUserRolesCommandHandler>());

        services.AddTransient<CreateRoleCommandHandler>();
        services.AddTransient<ICommandHandler<CreateRoleCommand, RoleDto>>(sp => sp.GetRequiredService<CreateRoleCommandHandler>());

        services.AddTransient<UpdateRolePermissionsCommandHandler>();
        services.AddTransient<ICommandHandler<UpdateRolePermissionsCommand, RoleDto>>(sp => sp.GetRequiredService<UpdateRolePermissionsCommandHandler>());

        services.AddTransient<CreatePermissionCommandHandler>();
        services.AddTransient<ICommandHandler<CreatePermissionCommand, PermissionDto>>(sp => sp.GetRequiredService<CreatePermissionCommandHandler>());

        services.AddTransient<GetUserProfileQueryHandler>();
        services.AddTransient<IQueryHandler<GetUserProfileQuery, UserProfileDto?>>(sp => sp.GetRequiredService<GetUserProfileQueryHandler>());

        services.AddTransient<ListUsersQueryHandler>();
        services.AddTransient<IQueryHandler<ListUsersQuery, List<UserProfileDto>>>(sp => sp.GetRequiredService<ListUsersQueryHandler>());

        services.AddTransient<ListRolesQueryHandler>();
        services.AddTransient<IQueryHandler<ListRolesQuery, List<RoleDto>>>(sp => sp.GetRequiredService<ListRolesQueryHandler>());

        services.AddTransient<ListPermissionsQueryHandler>();
        services.AddTransient<IQueryHandler<ListPermissionsQuery, List<PermissionDto>>>(sp => sp.GetRequiredService<ListPermissionsQueryHandler>());

        // Prompts & Session Modes (DB-backed CefrPromptBuilder)
        services.AddTransient<ListCefrLevelPromptsQueryHandler>();
        services.AddTransient<IQueryHandler<ListCefrLevelPromptsQuery, List<CefrLevelPromptEntity>>>(sp => sp.GetRequiredService<ListCefrLevelPromptsQueryHandler>());

        services.AddTransient<ListConversationModePromptsQueryHandler>();
        services.AddTransient<IQueryHandler<ListConversationModePromptsQuery, List<ConversationModePromptEntity>>>(sp => sp.GetRequiredService<ListConversationModePromptsQueryHandler>());

        services.AddTransient<PreviewSystemPromptQueryHandler>();
        services.AddTransient<IQueryHandler<PreviewSystemPromptQuery, string>>(sp => sp.GetRequiredService<PreviewSystemPromptQueryHandler>());

        services.AddTransient<UpsertCefrLevelPromptCommandHandler>();
        services.AddTransient<ICommandHandler<UpsertCefrLevelPromptCommand, CefrLevelPromptEntity>>(sp => sp.GetRequiredService<UpsertCefrLevelPromptCommandHandler>());

        services.AddTransient<UpsertConversationModePromptCommandHandler>();
        services.AddTransient<ICommandHandler<UpsertConversationModePromptCommand, ConversationModePromptEntity>>(sp => sp.GetRequiredService<UpsertConversationModePromptCommandHandler>());

        // Teacher Voices Catalog
        services.AddTransient<ListTeacherVoicesQueryHandler>();
        services.AddTransient<IQueryHandler<ListTeacherVoicesQuery, List<TeacherVoiceDto>>>(sp => sp.GetRequiredService<ListTeacherVoicesQueryHandler>());

        services.AddTransient<GetTeacherVoiceByIdQueryHandler>();
        services.AddTransient<IQueryHandler<GetTeacherVoiceByIdQuery, TeacherVoiceDto?>>(sp => sp.GetRequiredService<GetTeacherVoiceByIdQueryHandler>());

        services.AddTransient<UpsertTeacherVoiceCommandHandler>();
        services.AddTransient<ICommandHandler<UpsertTeacherVoiceCommand, TeacherVoiceDto>>(sp => sp.GetRequiredService<UpsertTeacherVoiceCommandHandler>());

        // Topics Aggregate Catalog
        services.AddTransient<ListTopicsQueryHandler>();
        services.AddTransient<IQueryHandler<ListTopicsQuery, List<TopicDto>>>(sp => sp.GetRequiredService<ListTopicsQueryHandler>());

        services.AddTransient<GetTopicByIdQueryHandler>();
        services.AddTransient<IQueryHandler<GetTopicByIdQuery, TopicDto?>>(sp => sp.GetRequiredService<GetTopicByIdQueryHandler>());

        services.AddTransient<UpsertTopicCommandHandler>();
        services.AddTransient<ICommandHandler<UpsertTopicCommand, TopicDto>>(sp => sp.GetRequiredService<UpsertTopicCommandHandler>());

        services.AddTransient<DeleteTopicCommandHandler>();
        services.AddTransient<ICommandHandler<DeleteTopicCommand, bool>>(sp => sp.GetRequiredService<DeleteTopicCommandHandler>());

        // Conversations
        services.AddTransient<CreateConversationCommandHandler>();
        services.AddTransient<ICommandHandler<CreateConversationCommand, ConversationEntity>>(sp => sp.GetRequiredService<CreateConversationCommandHandler>());

        services.AddTransient<UpdateConversationSettingsCommandHandler>();
        services.AddTransient<ICommandHandler<UpdateConversationSettingsCommand, ConversationEntity>>(sp => sp.GetRequiredService<UpdateConversationSettingsCommandHandler>());

        services.AddTransient<DeleteConversationCommandHandler>();
        services.AddTransient<ICommandHandler<DeleteConversationCommand, bool>>(sp => sp.GetRequiredService<DeleteConversationCommandHandler>());

        services.AddTransient<ListConversationsQueryHandler>();
        services.AddTransient<IQueryHandler<ListConversationsQuery, List<object>>>(sp => sp.GetRequiredService<ListConversationsQueryHandler>());

        services.AddTransient<GetConversationHistoryQueryHandler>();
        services.AddTransient<IQueryHandler<GetConversationHistoryQuery, object?>>(sp => sp.GetRequiredService<GetConversationHistoryQueryHandler>());

        // Vocabulary
        services.AddTransient<GenerateVocabularyCommandHandler>();
        services.AddTransient<ICommandHandler<GenerateVocabularyCommand, List<VocabularyFlashcardDto>>>(sp => sp.GetRequiredService<GenerateVocabularyCommandHandler>());

        services.AddTransient<GetVocabularyFlashcardQueryHandler>();
        services.AddTransient<IQueryHandler<GetVocabularyFlashcardQuery, VocabularyFlashcardDto?>>(sp => sp.GetRequiredService<GetVocabularyFlashcardQueryHandler>());

        services.AddTransient<GetVocabularyGenerationModelsQueryHandler>();
        services.AddTransient<IQueryHandler<GetVocabularyGenerationModelsQuery, List<AiModelConfigEntity>>>(sp => sp.GetRequiredService<GetVocabularyGenerationModelsQueryHandler>());

        // Learning
        services.AddTransient<InitializeDailyVocabularyPlanCommandHandler>();
        services.AddTransient<ICommandHandler<InitializeDailyVocabularyPlanCommand, DailyVocabularyPlanResponseDto>>(sp => sp.GetRequiredService<InitializeDailyVocabularyPlanCommandHandler>());

        services.AddTransient<SubmitVocabularyReviewCommandHandler>();
        services.AddTransient<ICommandHandler<SubmitVocabularyReviewCommand, VocabularyFlashcardDto>>(sp => sp.GetRequiredService<SubmitVocabularyReviewCommandHandler>());

        services.AddTransient<GetDailyVocabularyPlanQueryHandler>();
        services.AddTransient<IQueryHandler<GetDailyVocabularyPlanQuery, DailyVocabularyPlanResponseDto>>(sp => sp.GetRequiredService<GetDailyVocabularyPlanQueryHandler>());

        services.AddTransient<GetLearningProgressQueryHandler>();
        services.AddTransient<IQueryHandler<GetLearningProgressQuery, object>>(sp => sp.GetRequiredService<GetLearningProgressQueryHandler>());

        services.AddTransient<GetLearningHistoryQueryHandler>();
        services.AddTransient<IQueryHandler<GetLearningHistoryQuery, object>>(sp => sp.GetRequiredService<GetLearningHistoryQueryHandler>());

        // Pronunciation
        services.AddTransient<AssessPronunciationCommandHandler>();
        services.AddTransient<ICommandHandler<AssessPronunciationCommand, PronunciationAssessmentResultDto>>(sp => sp.GetRequiredService<AssessPronunciationCommandHandler>());

        services.AddTransient<UpdateScoringRubricCommandHandler>();
        services.AddTransient<ICommandHandler<UpdateScoringRubricCommand, ScoringRubricEntity>>(sp => sp.GetRequiredService<UpdateScoringRubricCommandHandler>());

        services.AddTransient<GetPronunciationModelsQueryHandler>();
        services.AddTransient<IQueryHandler<GetPronunciationModelsQuery, List<AiModelConfigEntity>>>(sp => sp.GetRequiredService<GetPronunciationModelsQueryHandler>());

        services.AddTransient<GetActiveScoringRubricQueryHandler>();
        services.AddTransient<IQueryHandler<GetActiveScoringRubricQuery, ScoringRubricEntity?>>(sp => sp.GetRequiredService<GetActiveScoringRubricQueryHandler>());

        services.AddTransient<GetPronunciationHistoryQueryHandler>();
        services.AddTransient<IQueryHandler<GetPronunciationHistoryQuery, List<PronunciationAssessmentResultDto>>>(sp => sp.GetRequiredService<GetPronunciationHistoryQueryHandler>());

        return services;
    }
}
