import type {
  GrammarAttemptKind,
  GrammarExerciseSource,
  GrammarExerciseVisibility,
  GrammarModerationStatus,
  GrammarStepProgressStatus,
  GrammarStepPassedBy,
  GrammarRoadmapDto,
  GrammarLevelDto,
  GrammarStepRoadmapItemDto,
  GrammarTheoryFormulaDto,
  GrammarTheoryExampleDto,
  GrammarTheorySectionDto,
  GrammarTheoryMistakeDto,
  GrammarTheoryDto,
  GrammarStepDetailDto,
  GrammarExerciseLearnerDto,
  StartGrammarAttemptRequestDto,
  LessonAiExecutionConfigDto,
  GrammarAiExecutionConfigDto,
  SubmitGrammarAnswerRequestDto,
  GrammarAttemptAnswerDto,
  GrammarAttemptDto,
  GrammarAnswerResultDto,
  GrammarAttemptResultDto,
  GrammarExercisePromptRequestedItemDto,
  GenerateGrammarExercisesRequestDto,
  BuildGrammarExercisePromptRequestDto,
  BuildGrammarExercisePromptResponseDto,
  ImportGrammarExercisesJsonRequestDto,
  GrammarExerciseRejectionDto,
  GrammarMyExerciseDto,
  GenerateGrammarExercisesResponseDto,
  UpdateMyGrammarExerciseRequestDto,
  ChangeMyGrammarExerciseVisibilityRequestDto,
  GrammarStepAdminDto,
  GrammarExerciseAdminDto,
  GrammarStepAdminDetailDto,
  UpdateGrammarStepRequestDto,
  UpdateGrammarStepTheoryRequestDto,
  CreateOfficialGrammarExerciseRequestDto,
  UpdateOfficialGrammarExerciseRequestDto,
  ImportGrammarContentPackRequestDto,
  GrammarImportIssueDto,
  GrammarContentPackImportResultDto,
  GrammarModerationItemDto,
  RejectGrammarExerciseRequestDto,
  PromoteGrammarExerciseRequestDto,
  GenerateOfficialGrammarExercisesRequestDto,
  ImportOfficialGrammarExercisesJsonRequestDto,
  DraftGrammarTheoryWithAiRequestDto,
  ValidationIssueDto,
  DraftGrammarTheoryWithAiResponseDto,
  GenerateOfficialGrammarExercisesResponseDto,
  GrammarExerciseType,
  GrammarGradingMode,
  PagedResultDto,
} from '@/shared/api/openapi.types';

export type {
  GrammarAttemptKind,
  GrammarExerciseSource,
  GrammarExerciseVisibility,
  GrammarModerationStatus,
  GrammarStepProgressStatus,
  GrammarStepPassedBy,
  GrammarRoadmapDto,
  GrammarLevelDto,
  GrammarStepRoadmapItemDto,
  GrammarTheoryFormulaDto,
  GrammarTheoryExampleDto,
  GrammarTheorySectionDto,
  GrammarTheoryMistakeDto,
  GrammarTheoryDto,
  GrammarStepDetailDto,
  GrammarExerciseLearnerDto,
  StartGrammarAttemptRequestDto,
  LessonAiExecutionConfigDto,
  GrammarAiExecutionConfigDto,
  SubmitGrammarAnswerRequestDto,
  GrammarAttemptAnswerDto,
  GrammarAttemptDto,
  GrammarAnswerResultDto,
  GrammarAttemptResultDto,
  GrammarExercisePromptRequestedItemDto,
  GenerateGrammarExercisesRequestDto,
  BuildGrammarExercisePromptRequestDto,
  BuildGrammarExercisePromptResponseDto,
  ImportGrammarExercisesJsonRequestDto,
  GrammarExerciseRejectionDto,
  GrammarMyExerciseDto,
  GenerateGrammarExercisesResponseDto,
  UpdateMyGrammarExerciseRequestDto,
  ChangeMyGrammarExerciseVisibilityRequestDto,
  GrammarStepAdminDto,
  GrammarExerciseAdminDto,
  GrammarStepAdminDetailDto,
  UpdateGrammarStepRequestDto,
  UpdateGrammarStepTheoryRequestDto,
  CreateOfficialGrammarExerciseRequestDto,
  UpdateOfficialGrammarExerciseRequestDto,
  ImportGrammarContentPackRequestDto,
  GrammarImportIssueDto,
  GrammarContentPackImportResultDto,
  GrammarModerationItemDto,
  RejectGrammarExerciseRequestDto,
  PromoteGrammarExerciseRequestDto,
  GenerateOfficialGrammarExercisesRequestDto,
  ImportOfficialGrammarExercisesJsonRequestDto,
  DraftGrammarTheoryWithAiRequestDto,
  ValidationIssueDto,
  DraftGrammarTheoryWithAiResponseDto,
  GenerateOfficialGrammarExercisesResponseDto,
  GrammarExerciseType,
  GrammarGradingMode,
  PagedResultDto,
};

export interface ListGrammarExercisesParams {
  source?: 'core' | 'community' | 'mine' | string;
  page?: number;
  pageSize?: number;
  paged?: boolean;
}

export interface ListMyGrammarExercisesParams {
  stepCode?: string;
  moderation?: GrammarModerationStatus;
  page?: number;
  pageSize?: number;
  paged?: boolean;
}

export interface ListAdminGrammarStepsParams {
  level?: string;
  page?: number;
  pageSize?: number;
  paged?: boolean;
}

export interface ListAdminGrammarModerationParams {
  status?: GrammarModerationStatus;
  level?: string;
  stepCode?: string;
  page?: number;
  pageSize?: number;
  paged?: boolean;
}
