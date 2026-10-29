export { DailyVocabularyPage } from './pages/DailyVocabularyPage';
export { PronunciationHistoryDialog } from './components/PronunciationHistoryDialog';
export { VocabularyFlashcard } from './components/VocabularyFlashcard';
export { PronunciationStudio } from './components/PronunciationStudio';
export { DailyVocabularyToolbar } from './components/DailyVocabularyToolbar';
export { DailyVocabularyAlerts } from './components/DailyVocabularyAlerts';
export { DailyVocabularyEmptyState } from './components/DailyVocabularyEmptyState';
export { AiVocabularyGeneratorDialog } from './components/AiVocabularyGeneratorDialog';
export {
  useDailyVocabularyPlanQuery,
  useSubmitVocabularyReviewMutation,
  useGenerateVocabularyMutation,
  useAssessPronunciationMutation,
  useSynthesizePronunciationMutation,
  useBuildVocabularyGenerationPromptMutation,
  useImportVocabularyJsonMutation,
} from './hooks/useDailyVocabulary';
export { usePronunciationStudio } from './hooks/usePronunciationStudio';
export { vocabularyApi } from './api/vocabularyApi';
export type {
  VocabularyFlashcardDto,
  BuildVocabularyGenerationPromptRequestDto,
  VocabularyGenerationPromptDto,
  ImportVocabularyJsonRequestDto,
} from './types/vocabulary.types';
export { SelectionVocabularyMenu } from './components/SelectionVocabularyMenu';
export { TargetedVocabularyDialog } from './components/TargetedVocabularyDialog';
export { normalizeTargetText, countWords, isSentenceMode } from './lib/targetedVocabulary';
export type {
  GenerateVocabularyRequestDto,
} from './types/vocabulary.types';

