using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.AiModels.Commands.CreateAiModelConfig;
using EnglishLiveTutor.Application.Features.AiModels.Commands.DeleteAiModelConfig;
using EnglishLiveTutor.Application.Features.AiModels.Commands.UpdateAiModelConfig;
using EnglishLiveTutor.Application.Features.AiModels.Queries.ListAiModelConfigs;
using EnglishLiveTutor.Application.Features.Conversations.Commands.CreateConversation;
using EnglishLiveTutor.Application.Features.Conversations.Commands.DeleteConversation;
using EnglishLiveTutor.Application.Features.Conversations.Commands.UpdateConversationSettings;
using EnglishLiveTutor.Application.Features.Conversations.Queries.GetConversationHistory;
using EnglishLiveTutor.Application.Features.Conversations.Queries.ListConversations;
using EnglishLiveTutor.Application.Features.Identity.Commands.AdminManageIdentity;
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
using EnglishLiveTutor.Application.Features.Prompts.Commands.DeleteCefrLevelPrompt;
using EnglishLiveTutor.Application.Features.Prompts.Commands.DeleteConversationModePrompt;
using EnglishLiveTutor.Application.Features.Prompts.Commands.DeleteSpeechSpeed;
using EnglishLiveTutor.Application.Features.Prompts.Commands.UpdateSpeechSpeed;
using EnglishLiveTutor.Application.Features.Prompts.Commands.UpsertCefrLevelPrompt;
using EnglishLiveTutor.Application.Features.Prompts.Commands.UpsertConversationModePrompt;
using EnglishLiveTutor.Application.Features.Prompts.Commands.UpsertSpeechSpeed;
using EnglishLiveTutor.Application.Features.Prompts.Queries.ListCefrLevelPrompts;
using EnglishLiveTutor.Application.Features.Prompts.Queries.ListConversationModePrompts;
using EnglishLiveTutor.Application.Features.Prompts.Queries.ListSpeechSpeeds;
using EnglishLiveTutor.Application.Features.Prompts.Queries.PreviewSystemPrompt;
using EnglishLiveTutor.Application.Features.Pronunciation.Commands.AssessPronunciation;
using EnglishLiveTutor.Application.Features.Pronunciation.Commands.DeleteScoringRubric;
using EnglishLiveTutor.Application.Features.Pronunciation.Commands.SynthesizePronunciationAudio;
using EnglishLiveTutor.Application.Features.Pronunciation.Commands.UpdateScoringRubric;
using EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetActiveScoringRubric;
using EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetPronunciationHistory;
using EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetPronunciationModels;
using EnglishLiveTutor.Application.Features.Pronunciation.Queries.ListScoringRubrics;
using EnglishLiveTutor.Application.Features.Topics.Commands.DeleteTopic;
using EnglishLiveTutor.Application.Features.Topics.Commands.UpsertTopic;
using EnglishLiveTutor.Application.Features.Topics.Queries.GetTopicById;
using EnglishLiveTutor.Application.Features.Topics.Queries.ListTopics;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.CreateVocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.DeleteVocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.GenerateVocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.ImportVocabularyJson;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.BuildVocabularyGenerationPrompt;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.UpdateVocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.GetVocabularyFlashcard;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.GetVocabularyGenerationModels;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.ListVocabularyFlashcards;
using EnglishLiveTutor.Application.Features.Voices.Commands.UpsertTeacherVoice;
using EnglishLiveTutor.Application.Features.Voices.Queries.GetTeacherVoiceById;
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

        services.AddTransient<AdminCreateUserCommandHandler>();
        services.AddTransient<ICommandHandler<AdminCreateUserCommand, UserProfileDto>>(sp => sp.GetRequiredService<AdminCreateUserCommandHandler>());

        services.AddTransient<AdminUpdateUserCommandHandler>();
        services.AddTransient<ICommandHandler<AdminUpdateUserCommand, UserProfileDto?>>(sp => sp.GetRequiredService<AdminUpdateUserCommandHandler>());

        services.AddTransient<DeleteUserCommandHandler>();
        services.AddTransient<ICommandHandler<DeleteUserCommand, bool>>(sp => sp.GetRequiredService<DeleteUserCommandHandler>());

        services.AddTransient<CreateRoleCommandHandler>();
        services.AddTransient<ICommandHandler<CreateRoleCommand, RoleDto>>(sp => sp.GetRequiredService<CreateRoleCommandHandler>());

        services.AddTransient<UpdateRolePermissionsCommandHandler>();
        services.AddTransient<ICommandHandler<UpdateRolePermissionsCommand, RoleDto>>(sp => sp.GetRequiredService<UpdateRolePermissionsCommandHandler>());

        services.AddTransient<DeleteRoleCommandHandler>();
        services.AddTransient<ICommandHandler<DeleteRoleCommand, bool>>(sp => sp.GetRequiredService<DeleteRoleCommandHandler>());

        services.AddTransient<CreatePermissionCommandHandler>();
        services.AddTransient<ICommandHandler<CreatePermissionCommand, PermissionDto>>(sp => sp.GetRequiredService<CreatePermissionCommandHandler>());

        services.AddTransient<UpdatePermissionCommandHandler>();
        services.AddTransient<ICommandHandler<UpdatePermissionCommand, PermissionDto?>>(sp => sp.GetRequiredService<UpdatePermissionCommandHandler>());

        services.AddTransient<DeletePermissionCommandHandler>();
        services.AddTransient<ICommandHandler<DeletePermissionCommand, bool>>(sp => sp.GetRequiredService<DeletePermissionCommandHandler>());

        services.AddTransient<GetUserProfileQueryHandler>();
        services.AddTransient<IQueryHandler<GetUserProfileQuery, UserProfileDto?>>(sp => sp.GetRequiredService<GetUserProfileQueryHandler>());

        services.AddTransient<ListUsersQueryHandler>();
        services.AddTransient<IQueryHandler<ListUsersQuery, PagedResultDto<UserProfileDto>>>(sp => sp.GetRequiredService<ListUsersQueryHandler>());

        services.AddTransient<ListRolesQueryHandler>();
        services.AddTransient<IQueryHandler<ListRolesQuery, PagedResultDto<RoleDto>>>(sp => sp.GetRequiredService<ListRolesQueryHandler>());

        services.AddTransient<ListPermissionsQueryHandler>();
        services.AddTransient<IQueryHandler<ListPermissionsQuery, PagedResultDto<PermissionDto>>>(sp => sp.GetRequiredService<ListPermissionsQueryHandler>());

        // Prompts & Session Modes (DB-backed CefrPromptBuilder)
        services.AddTransient<ListCefrLevelPromptsQueryHandler>();
        services.AddTransient<IQueryHandler<ListCefrLevelPromptsQuery, PagedResultDto<CefrLevelPromptEntity>>>(sp => sp.GetRequiredService<ListCefrLevelPromptsQueryHandler>());

        services.AddTransient<ListConversationModePromptsQueryHandler>();
        services.AddTransient<IQueryHandler<ListConversationModePromptsQuery, PagedResultDto<ConversationModePromptEntity>>>(sp => sp.GetRequiredService<ListConversationModePromptsQueryHandler>());

        services.AddTransient<PreviewSystemPromptQueryHandler>();
        services.AddTransient<IQueryHandler<PreviewSystemPromptQuery, string>>(sp => sp.GetRequiredService<PreviewSystemPromptQueryHandler>());

        services.AddTransient<UpsertCefrLevelPromptCommandHandler>();
        services.AddTransient<ICommandHandler<UpsertCefrLevelPromptCommand, CefrLevelPromptEntity>>(sp => sp.GetRequiredService<UpsertCefrLevelPromptCommandHandler>());

        services.AddTransient<UpsertConversationModePromptCommandHandler>();
        services.AddTransient<ICommandHandler<UpsertConversationModePromptCommand, ConversationModePromptEntity>>(sp => sp.GetRequiredService<UpsertConversationModePromptCommandHandler>());

        // Teacher Voices Catalog
        services.AddTransient<ListTeacherVoicesQueryHandler>();
        services.AddTransient<IQueryHandler<ListTeacherVoicesQuery, PagedResultDto<TeacherVoiceDto>>>(sp => sp.GetRequiredService<ListTeacherVoicesQueryHandler>());

        services.AddTransient<GetTeacherVoiceByIdQueryHandler>();
        services.AddTransient<IQueryHandler<GetTeacherVoiceByIdQuery, TeacherVoiceDto?>>(sp => sp.GetRequiredService<GetTeacherVoiceByIdQueryHandler>());

        services.AddTransient<UpsertTeacherVoiceCommandHandler>();
        services.AddTransient<ICommandHandler<UpsertTeacherVoiceCommand, TeacherVoiceDto>>(sp => sp.GetRequiredService<UpsertTeacherVoiceCommandHandler>());

        // Topics Aggregate Catalog
        services.AddTransient<ListTopicsQueryHandler>();
        services.AddTransient<IQueryHandler<ListTopicsQuery, PagedResultDto<TopicDto>>>(sp => sp.GetRequiredService<ListTopicsQueryHandler>());

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
        services.AddTransient<IQueryHandler<ListConversationsQuery, PagedResultDto<object>>>(sp => sp.GetRequiredService<ListConversationsQueryHandler>());

        services.AddTransient<GetConversationHistoryQueryHandler>();
        services.AddTransient<IQueryHandler<GetConversationHistoryQuery, object?>>(sp => sp.GetRequiredService<GetConversationHistoryQueryHandler>());

        // Vocabulary
        services.AddTransient<GenerateVocabularyCommandHandler>();
        services.AddTransient<VocabularyGenerationWriter>();
        services.AddTransient<IQueryHandler<BuildVocabularyGenerationPromptQuery, VocabularyGenerationPromptDto>, BuildVocabularyGenerationPromptQueryHandler>();
        services.AddTransient<ICommandHandler<ImportVocabularyJsonCommand, List<VocabularyFlashcardDto>>, ImportVocabularyJsonCommandHandler>();
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

        // Catalog and admin CQRS handlers.
        services.AddTransient<ListVocabularyFlashcardsQueryHandler>();
        services.AddTransient<IQueryHandler<ListVocabularyFlashcardsQuery, PagedResultDto<VocabularyFlashcardDto>>>(sp => sp.GetRequiredService<ListVocabularyFlashcardsQueryHandler>());

        services.AddTransient<ListScoringRubricsQueryHandler>();
        services.AddTransient<IQueryHandler<ListScoringRubricsQuery, PagedResultDto<ScoringRubricEntity>>>(sp => sp.GetRequiredService<ListScoringRubricsQueryHandler>());

        services.AddTransient<ListAiModelConfigsQueryHandler>();
        services.AddTransient<IQueryHandler<ListAiModelConfigsQuery, PagedResultDto<AiModelConfigEntity>>>(sp => sp.GetRequiredService<ListAiModelConfigsQueryHandler>());

        services.AddTransient<ListSpeechSpeedsQueryHandler>();
        services.AddTransient<IQueryHandler<ListSpeechSpeedsQuery, PagedResultDto<SpeechSpeedOptionDto>>>(sp => sp.GetRequiredService<ListSpeechSpeedsQueryHandler>());

        services.AddTransient<DeleteVocabularyCommandHandler>();
        services.AddTransient<ICommandHandler<DeleteVocabularyCommand, bool>>(sp => sp.GetRequiredService<DeleteVocabularyCommandHandler>());

        services.AddTransient<DeleteScoringRubricCommandHandler>();
        services.AddTransient<ICommandHandler<DeleteScoringRubricCommand, bool>>(sp => sp.GetRequiredService<DeleteScoringRubricCommandHandler>());

        services.AddTransient<DeleteAiModelConfigCommandHandler>();
        services.AddTransient<ICommandHandler<DeleteAiModelConfigCommand, bool>>(sp => sp.GetRequiredService<DeleteAiModelConfigCommandHandler>());

        services.AddTransient<DeleteSpeechSpeedCommandHandler>();
        services.AddTransient<ICommandHandler<DeleteSpeechSpeedCommand, bool>>(sp => sp.GetRequiredService<DeleteSpeechSpeedCommandHandler>());

        services.AddTransient<DeleteCefrLevelPromptCommandHandler>();
        services.AddTransient<ICommandHandler<DeleteCefrLevelPromptCommand, bool>>(sp => sp.GetRequiredService<DeleteCefrLevelPromptCommandHandler>());

        services.AddTransient<DeleteConversationModePromptCommandHandler>();
        services.AddTransient<ICommandHandler<DeleteConversationModePromptCommand, bool>>(sp => sp.GetRequiredService<DeleteConversationModePromptCommandHandler>());

        services.AddTransient<UpsertSpeechSpeedCommandHandler>();
        services.AddTransient<ICommandHandler<UpsertSpeechSpeedCommand, SpeechSpeedOptionDto>>(sp => sp.GetRequiredService<UpsertSpeechSpeedCommandHandler>());

        services.AddTransient<UpdateSpeechSpeedCommandHandler>();
        services.AddTransient<ICommandHandler<UpdateSpeechSpeedCommand, SpeechSpeedOptionDto?>>(sp => sp.GetRequiredService<UpdateSpeechSpeedCommandHandler>());

        services.AddTransient<CreateVocabularyCommandHandler>();
        services.AddTransient<ICommandHandler<CreateVocabularyCommand, VocabularyFlashcardDto>>(sp => sp.GetRequiredService<CreateVocabularyCommandHandler>());

        services.AddTransient<UpdateVocabularyCommandHandler>();
        services.AddTransient<ICommandHandler<UpdateVocabularyCommand, VocabularyFlashcardDto?>>(sp => sp.GetRequiredService<UpdateVocabularyCommandHandler>());

        services.AddTransient<CreateAiModelConfigCommandHandler>();
        services.AddTransient<ICommandHandler<CreateAiModelConfigCommand, AiModelConfigEntity>>(sp => sp.GetRequiredService<CreateAiModelConfigCommandHandler>());

        services.AddTransient<UpdateAiModelConfigCommandHandler>();
        services.AddTransient<ICommandHandler<UpdateAiModelConfigCommand, AiModelConfigEntity?>>(sp => sp.GetRequiredService<UpdateAiModelConfigCommandHandler>());

        services.AddTransient<SynthesizePronunciationAudioCommandHandler>();
        services.AddTransient<ICommandHandler<SynthesizePronunciationAudioCommand, SynthesizePronunciationAudioResponseDto>>(sp => sp.GetRequiredService<SynthesizePronunciationAudioCommandHandler>());

        return services;
    }
}
