import type {
  SubmitGrammarAnswerRequestDto,
  GrammarExercisePromptRequestedItemDto,
  GrammarAttemptKind,
  GrammarExerciseRejectionDto,
  GrammarTheoryDto,
  GrammarTheorySectionDto,
  GrammarTheoryMistakeDto,
} from '../types/grammar.types';

/**
 * Extracts raw error code/detail from ProblemDetails or Error object.
 */
export function getGrammarErrorCode(err: unknown): string {
  if (typeof err === 'string') return err;
  if (!err || typeof err !== 'object') return '';
  const anyErr = err as any;
  return (
    anyErr.response?.data?.detail ||
    anyErr.response?.data?.title ||
    anyErr.detail ||
    anyErr.code ||
    anyErr.message ||
    ''
  );
}

/**
 * Maps Backend ProblemDetails error code or status to localized user-friendly message.
 * Never displays raw error codes to the user.
 */
export function getGrammarErrorMessage(
  err: unknown,
  t: (key: string, options?: any) => string,
  isAdmin: boolean = false
): string {
  const status = (err as any)?.response?.status;
  const code = getGrammarErrorCode(err).trim();
  const lowerCode = code.toLowerCase();

  // 2. Admin moderation & conflict messages (409/400/422/429)
  if (
    lowerCode.includes('cannot approve exercise in status') ||
    lowerCode.includes('cannot reject exercise in status') ||
    lowerCode.includes('must be pending') ||
    code === 'grammar_exercise_not_pending'
  ) {
    return t('admin.grammar.errors.exerciseNotPending');
  }
  if (
    lowerCode.includes('cannot promote exercise') ||
    lowerCode.includes('must be approved') ||
    code === 'grammar_exercise_not_approved'
  ) {
    return t('admin.grammar.errors.exerciseNotApproved');
  }
  if (lowerCode.includes('already exists in step') || lowerCode.includes('an exercise with key')) {
    return t('admin.grammar.errors.duplicateKey');
  }
  if (lowerCode.includes('identical content hash')) {
    return t('admin.grammar.errors.duplicateContentHash');
  }
  if (code === 'grammar_not_found') {
    return isAdmin ? t('admin.grammar.errors.notFound') : t('grammar.errors.generic');
  }
  if (code === 'grammar_forbidden') {
    return t('admin.grammar.errors.forbidden');
  }

  // 3. Learner codes
  if (code === 'grammar_step_locked') {
    return t('grammar.errors.stepLocked');
  }
  if (code === 'grammar_level_test_locked') {
    return t('grammar.errors.levelTestLocked');
  }
  if (code === 'grammar_level_test_not_enough_exercises') {
    return t('grammar.errors.levelTestNotEnoughExercises');
  }
  if (code === 'grammar_step_has_no_core_exercises') {
    return t('grammar.errors.stepHasNoCoreExercises');
  }
  if (code === 'grammar_step_has_no_exercises') {
    return t('grammar.errors.stepHasNoExercises');
  }
  if (code === 'grammar_duplicate_content') {
    return t('grammar.errors.duplicateContent');
  }
  if (code === 'grammar_answer_locked') {
    return t('grammar.errors.answerLocked');
  }
  if (code === 'grammar_conflict') {
    return t('grammar.errors.conflict');
  }
  if (code === 'grammar_pending_limit') {
    return t('grammar.errors.pendingLimit');
  }
  if (code === 'grammar_daily_quota_exceeded') {
    return t('grammar.errors.dailyQuotaExceeded');
  }
  if (code === 'grammar_generation_no_valid_exercises') {
    return t('grammar.errors.generationNoValidExercises');
  }
  if (code === 'grammar_ai_timeout') {
    return t('grammar.errors.aiTimeout');
  }
  if (code.startsWith('grammar_ai_provider_error')) {
    return t('grammar.errors.aiProviderError');
  }

  // Status fallback only after known codes: BE sends 403 grammar_step_locked/grammar_level_test_locked.
  if (status === 403) {
    return t('common.forbidden');
  }
  if (status === 404) {
    return isAdmin ? t('admin.grammar.stepNotFoundTitle') : t('grammar.errors.generic');
  }

  return t('grammar.errors.generic');
}

/**
 * Dedicated admin error mapper that maps 404 to admin.grammar.errors.notFound.
 */
export function getAdminGrammarErrorMessage(
  err: unknown,
  t: (key: string, options?: any) => string
): string {
  return getGrammarErrorMessage(err, t, true);
}

/**
 * Builds SubmitGrammarAnswerRequestDto matching BE LessonAiExecutionConfigDto.
 * Crucial rule: If apiKey is empty/whitespace, do NOT send aiConfig (even if provider is AGY,
 * because BE only performs AI grading when ApiKey is non-empty).
 */
export function buildGrammarSubmitPayload(
  exerciseId: string,
  answerPayload: Record<string, unknown>,
  aiProvider: string,
  apiKey: string | null | undefined,
  assessmentModelCode?: string | null,
  agyModelCode?: string | null
): SubmitGrammarAnswerRequestDto {
  const cleanKey = apiKey?.trim();
  if (!cleanKey) {
    return {
      exerciseId,
      answerPayload,
    };
  }

  const isAgy = aiProvider === 'agy';
  return {
    exerciseId,
    answerPayload,
    aiConfig: {
      provider: isAgy ? 'agy' : 'gemini',
      assessmentModelCode: isAgy ? agyModelCode || null : assessmentModelCode || null,
      apiKey: cleanKey,
    },
  };
}

/**
 * Validates and parses JSON text field into an object (never array or primitive).
 */
export function parseJsonObjectField(
  jsonStr: string,
  _fieldName?: string,
  isOptional: boolean = false
): { ok: true; value: Record<string, unknown> | null } | { ok: false; errorKey: string } {
  const trimmed = jsonStr.trim();
  if (!trimmed) {
    if (isOptional) {
      return { ok: true, value: null };
    }
    return { ok: false, errorKey: 'grammar.editErrors.required' };
  }

  let parsed: unknown;
  try {
    parsed = JSON.parse(trimmed);
  } catch {
    return { ok: false, errorKey: 'grammar.editErrors.invalidJson' };
  }

  if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) {
    return { ok: false, errorKey: 'grammar.editErrors.mustBeObject' };
  }

  return { ok: true, value: parsed as Record<string, unknown> };
}

/**
 * Evaluates whether Level Test button should be disabled and returns reason key.
 */
export function getLevelTestButtonState(canTakeLevelTest: boolean): {
  disabled: boolean;
  reasonKey?: string;
} {
  if (!canTakeLevelTest) {
    return {
      disabled: true,
      reasonKey: 'grammar.levelTestLockedReason',
    };
  }
  return {
    disabled: false,
  };
}

/**
 * Validates exercise generation requested items count and bounds.
 */
export function validateGenerateItems(
  items: GrammarExercisePromptRequestedItemDto[],
  maxCount: number = 10
): {
  valid: boolean;
  totalCount: number;
  errorKey?: string;
} {
  const totalCount = items.reduce((sum, item) => sum + (item.count || 0), 0);
  if (totalCount <= 0) {
    return { valid: false, totalCount, errorKey: 'grammar.selectAtLeastOneExercise' };
  }
  if (totalCount > maxCount) {
    return {
      valid: false,
      totalCount,
      errorKey: maxCount === 10 ? 'grammar.max10Exercises' : 'admin.grammar.max20Exercises',
    };
  }
  return { valid: true, totalCount };
}

/**
 * Result structure of grouping errors by form field.
 */
export interface GroupedFieldIssues {
  fieldIssues: Record<string, string[]>;
  byField: Record<string, string[]>;
  otherIssues: Array<{ path: string; message: string; messages: string[] }>;
}

/**
 * Pure helper to group backend validation issues by field case-insensitively by prefix.
 * content.* -> content, grading.* -> grading, $.x -> x.
 * Issues that do not match any field are placed in otherIssues as { path, message }.
 */
export function groupIssuesByField(
  errors: unknown,
  fields: string[]
): GroupedFieldIssues {
  const fieldIssues: Record<string, string[]> = {};
  for (const f of fields) {
    fieldIssues[f] = [];
  }
  const otherIssues: Array<{ path: string; message: string }> = [];

  const resultHelper = () => ({
    fieldIssues,
    byField: fieldIssues,
    otherIssues: otherIssues.map((o) => ({ path: o.path, message: o.message, messages: [o.message] })),
  });

  if (!errors || typeof errors !== 'object') {
    return resultHelper();
  }

  function assignIssue(rawPath: string, message: string) {
    let cleanPath = rawPath.trim();
    if (cleanPath.startsWith('$.')) {
      cleanPath = cleanPath.slice(2);
    } else if (cleanPath.startsWith('$')) {
      cleanPath = cleanPath.slice(1);
      if (cleanPath.startsWith('.')) cleanPath = cleanPath.slice(1);
    }
    if (cleanPath.startsWith('/')) {
      cleanPath = cleanPath.slice(1);
    }

    const lowerClean = cleanPath.toLowerCase();

    // Find longest matching field case-insensitively
    let matchedField: string | null = null;
    let maxMatchLen = 0;

    for (const f of fields) {
      const lowerF = f.toLowerCase();
      const isExact = lowerClean === lowerF;
      const isPrefixDot = lowerClean.startsWith(lowerF + '.');
      const isPrefixBracket = lowerClean.startsWith(lowerF + '[');
      const isPrefixSlash = lowerClean.startsWith(lowerF + '/');

      if (isExact || isPrefixDot || isPrefixBracket || isPrefixSlash) {
        if (lowerF.length > maxMatchLen) {
          matchedField = f;
          maxMatchLen = lowerF.length;
        }
      }
    }

    if (matchedField) {
      if (!fieldIssues[matchedField]) {
        fieldIssues[matchedField] = [];
      }
      fieldIssues[matchedField].push(message);
    } else {
      otherIssues.push({ path: rawPath, message });
    }
  }

  // Case 1: Array of issues
  if (Array.isArray(errors)) {
    for (const item of errors) {
      if (!item || typeof item !== 'object') continue;
      const rawPath: string = (item.path || item.propertyName || item.field || '$').trim();
      const message: string = item.message || item.errorMessage || '';
      assignIssue(rawPath, message);
    }
    return resultHelper();
  }

  // Case 2: Object dictionary
  const record = errors as Record<string, unknown>;
  for (const [key, val] of Object.entries(record)) {
    const rawPath = key.trim();
    if (Array.isArray(val)) {
      for (const msg of val) {
        if (typeof msg === 'string') {
          assignIssue(rawPath, msg);
        }
      }
    } else if (typeof val === 'string') {
      assignIssue(rawPath, val);
    }
  }

  return resultHelper();
}

/**
 * Extracts validation error dictionary by field/path from 400 ProblemDetails response.
 */
export function extractValidationErrors(err: unknown): Record<string, string[]> {
  if (!err || typeof err !== 'object') return {};
  const anyErr = err as any;
  const errors = anyErr.response?.data?.errors;
  if (errors && typeof errors === 'object' && !Array.isArray(errors)) {
    return errors as Record<string, string[]>;
  }
  return {};
}

/**
 * Creates empty valid structured theory object.
 */
export function createEmptyTheory(): GrammarTheoryDto {
  return {
    objectives: [],
    sections: [],
    commonMistakes: [],
    tips: [],
    summary: [],
  };
}

/**
 * Validates JSON text representing GrammarTheoryDto.
 */
export function validateTheoryJson(jsonStr: string):
  | { ok: true; value: GrammarTheoryDto }
  | { ok: false; errorKey: string; errorDetail?: string } {
  const trimmed = jsonStr.trim();
  if (!trimmed) {
    return { ok: false, errorKey: 'admin.grammar.theoryErrors.empty' };
  }
  let parsed: unknown;
  try {
    parsed = JSON.parse(trimmed);
  } catch (ex: any) {
    return {
      ok: false,
      errorKey: 'admin.grammar.theoryErrors.invalidJson',
      errorDetail: ex?.message,
    };
  }

  if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) {
    return { ok: false, errorKey: 'admin.grammar.theoryErrors.mustBeObject' };
  }

  const theory = parsed as Record<string, any>;

  // Validate objectives elements are strings
  if (theory.objectives !== undefined && theory.objectives !== null) {
    if (!Array.isArray(theory.objectives)) {
      return { ok: false, errorKey: 'admin.grammar.theoryErrors.invalidObjectives' };
    }
    for (const obj of theory.objectives) {
      if (typeof obj !== 'string') {
        return { ok: false, errorKey: 'admin.grammar.theoryErrors.invalidObjectives' };
      }
    }
  }

  if (!Array.isArray(theory.sections)) {
    return { ok: false, errorKey: 'admin.grammar.theoryErrors.missingSections' };
  }

  // Validate sections elements are non-null objects
  const isOptionalArray = (v: unknown) => v === undefined || v === null || Array.isArray(v);
  const isObject = (v: unknown) => typeof v === 'object' && v !== null && !Array.isArray(v);
  for (const s of theory.sections) {
    if (!isObject(s)) {
      return { ok: false, errorKey: 'admin.grammar.theoryErrors.invalidSections' };
    }
    // Preview/editor map over these, so a non-array value would crash rendering.
    if (!isOptionalArray(s.formulas) || !isOptionalArray(s.examples) || !isOptionalArray(s.notes)) {
      return { ok: false, errorKey: 'admin.grammar.theoryErrors.invalidSections' };
    }
    if ((s.formulas ?? []).some((x: unknown) => typeof x !== 'string' && !isObject(x)) || (s.examples ?? []).some((x: unknown) => !isObject(x))) {
      return { ok: false, errorKey: 'admin.grammar.theoryErrors.invalidSections' };
    }
  }

  // Validate tips elements are strings
  if (theory.tips !== undefined && theory.tips !== null) {
    if (!Array.isArray(theory.tips)) {
      return { ok: false, errorKey: 'admin.grammar.theoryErrors.invalidTips' };
    }
    for (const tip of theory.tips) {
      if (typeof tip !== 'string') {
        return { ok: false, errorKey: 'admin.grammar.theoryErrors.invalidTips' };
      }
    }
  }

  // Validate summary elements are strings
  if (theory.summary !== undefined && theory.summary !== null) {
    if (!Array.isArray(theory.summary)) {
      return { ok: false, errorKey: 'admin.grammar.theoryErrors.invalidSummary' };
    }
    for (const sum of theory.summary) {
      if (typeof sum !== 'string') {
        return { ok: false, errorKey: 'admin.grammar.theoryErrors.invalidSummary' };
      }
    }
  }

  // Validate commonMistakes elements are non-null objects
  if (theory.commonMistakes !== undefined && theory.commonMistakes !== null) {
    if (!Array.isArray(theory.commonMistakes)) {
      return { ok: false, errorKey: 'admin.grammar.theoryErrors.invalidMistakes' };
    }
    for (const m of theory.commonMistakes) {
      if (typeof m !== 'object' || m === null || Array.isArray(m)) {
        return { ok: false, errorKey: 'admin.grammar.theoryErrors.invalidMistakes' };
      }
    }
  }

  return { ok: true, value: parsed as GrammarTheoryDto };
}

/**
 * Adds an objective to theory.
 */
export function addObjectiveToTheory(theory: GrammarTheoryDto, text: string): GrammarTheoryDto {
  const current = theory.objectives ? [...theory.objectives] : [];
  return {
    ...theory,
    objectives: [...current, text],
  };
}

/**
 * Removes an objective from theory by index.
 */
export function removeObjectiveFromTheory(theory: GrammarTheoryDto, index: number): GrammarTheoryDto {
  const current = theory.objectives ? [...theory.objectives] : [];
  current.splice(index, 1);
  return {
    ...theory,
    objectives: current,
  };
}

/**
 * Reorders objectives in theory.
 */
export function reorderObjectivesInTheory(
  theory: GrammarTheoryDto,
  fromIdx: number,
  toIdx: number
): GrammarTheoryDto {
  const list = theory.objectives ? [...theory.objectives] : [];
  if (fromIdx < 0 || fromIdx >= list.length || toIdx < 0 || toIdx >= list.length) {
    return theory;
  }
  const [removed] = list.splice(fromIdx, 1);
  list.splice(toIdx, 0, removed);
  return {
    ...theory,
    objectives: list,
  };
}

/**
 * Adds a new section to theory.
 */
export function addSectionToTheory(
  theory: GrammarTheoryDto,
  section?: Partial<GrammarTheorySectionDto>
): GrammarTheoryDto {
  const newSection: GrammarTheorySectionDto = {
    heading: section?.heading || '',
    explanationVi: section?.explanationVi || '',
    formulas: section?.formulas || [],
    examples: section?.examples || [],
    notes: section?.notes || [],
  };
  return {
    ...theory,
    sections: [...(theory.sections || []), newSection],
  };
}

/**
 * Removes a section from theory by index.
 */
export function removeSectionFromTheory(theory: GrammarTheoryDto, index: number): GrammarTheoryDto {
  const current = [...(theory.sections || [])];
  current.splice(index, 1);
  return {
    ...theory,
    sections: current,
  };
}

/**
 * Reorders sections in theory.
 */
export function reorderSectionsInTheory(
  theory: GrammarTheoryDto,
  fromIdx: number,
  toIdx: number
): GrammarTheoryDto {
  const list = [...(theory.sections || [])];
  if (fromIdx < 0 || fromIdx >= list.length || toIdx < 0 || toIdx >= list.length) {
    return theory;
  }
  const [removed] = list.splice(fromIdx, 1);
  list.splice(toIdx, 0, removed);
  return {
    ...theory,
    sections: list,
  };
}

/**
 * Updates a specific section in theory.
 */
export function updateSectionInTheory(
  theory: GrammarTheoryDto,
  index: number,
  patch: Partial<GrammarTheorySectionDto>
): GrammarTheoryDto {
  const sections = [...(theory.sections || [])];
  if (index >= 0 && index < sections.length) {
    sections[index] = { ...sections[index], ...patch };
  }
  return {
    ...theory,
    sections,
  };
}

/**
 * Adds a mistake to theory.
 */
export function addMistakeToTheory(
  theory: GrammarTheoryDto,
  mistake?: Partial<GrammarTheoryMistakeDto>
): GrammarTheoryDto {
  const newMistake: GrammarTheoryMistakeDto = {
    wrong: mistake?.wrong || '',
    right: mistake?.right || '',
    explanationVi: mistake?.explanationVi || '',
  };
  return {
    ...theory,
    commonMistakes: [...(theory.commonMistakes || []), newMistake],
  };
}

/**
 * Removes a mistake from theory by index.
 */
export function removeMistakeFromTheory(theory: GrammarTheoryDto, index: number): GrammarTheoryDto {
  const current = [...(theory.commonMistakes || [])];
  current.splice(index, 1);
  return {
    ...theory,
    commonMistakes: current,
  };
}

/**
 * Updates a mistake in theory.
 */
export function updateMistakeInTheory(
  theory: GrammarTheoryDto,
  index: number,
  patch: Partial<GrammarTheoryMistakeDto>
): GrammarTheoryDto {
  const mistakes = [...(theory.commonMistakes || [])];
  if (index >= 0 && index < mistakes.length) {
    mistakes[index] = { ...mistakes[index], ...patch };
  }
  return {
    ...theory,
    commonMistakes: mistakes,
  };
}

/**
 * Adds a tip to theory.
 */
export function addTipToTheory(theory: GrammarTheoryDto, tip: string): GrammarTheoryDto {
  const current = theory.tips ? [...theory.tips] : [];
  return {
    ...theory,
    tips: [...current, tip],
  };
}

/**
 * Removes a tip from theory by index.
 */
export function removeTipFromTheory(theory: GrammarTheoryDto, index: number): GrammarTheoryDto {
  const current = theory.tips ? [...theory.tips] : [];
  current.splice(index, 1);
  return {
    ...theory,
    tips: current,
  };
}

/**
 * Safely converts between structured mode and raw JSON mode without losing data.
 */
export function convertTheoryBetweenModes(
  currentTheory: GrammarTheoryDto,
  targetMode: 'structured' | 'raw',
  currentRaw: string
): { theory: GrammarTheoryDto; raw: string; errorKey?: string } {
  if (targetMode === 'raw') {
    // Structured -> Raw: serialize current theory
    return {
      theory: currentTheory,
      raw: JSON.stringify(currentTheory, null, 2),
    };
  }

  // Raw -> Structured: parse currentRaw
  const validation = validateTheoryJson(currentRaw);
  if (!validation.ok) {
    return {
      theory: currentTheory,
      raw: currentRaw,
      errorKey: validation.errorKey,
    };
  }

  return {
    theory: validation.value,
    raw: currentRaw,
  };
}


/**
 * Determines whether submission of an exercise requires explicit user confirmation.
 * - StepCore and LevelTest attempts require confirmation for one-time submit (unless locked)
 * - If already answered and failed grading, allows resubmission with confirmation
 * - Practice attempts never require confirmation (free practice)
 */
export function shouldConfirmGrammarSubmit(
  kind: GrammarAttemptKind,
  answerStatus?: string | null
): boolean {
  if (kind === 'Practice') {
    return false;
  }
  if (!answerStatus || answerStatus === 'FailedGrading') {
    return true;
  }
  return false;
}

/**
 * Determines whether an exercise can be redone.
 * - Practice mode allows redoing any exercise
 * - StepCore and LevelTest only allow redoing if previous submission was FailedGrading
 */
export function canRedoGrammarExercise(
  kind: GrammarAttemptKind,
  answerStatus?: string | null
): boolean {
  if (kind === 'Practice') {
    return true;
  }
  return answerStatus === 'FailedGrading';
}

/**
 * Determines whether the exercise answer is locked from further submission.
 */
export function isGrammarAnswerLocked(
  kind: GrammarAttemptKind,
  answerStatus?: string | null
): boolean {
  if (kind === 'Practice') {
    return false;
  }
  return Boolean(answerStatus && answerStatus !== 'FailedGrading');
}

/**
 * Extracts rejected exercise list from 422 ProblemDetails response.
 */
export function extractGrammarRejectionsFromError(
  err: unknown
): GrammarExerciseRejectionDto[] {
  if (!err || typeof err !== 'object') return [];
  const anyErr = err as any;
  const list =
    anyErr.response?.data?.rejected ||
    anyErr.response?.data?.extensions?.rejected ||
    anyErr.response?.data?.errors?.rejected ||
    anyErr.rejected;
  return Array.isArray(list) ? list : [];
}

/**
 * Pure helper to move an item within an array.
 * Safe against out of bounds indices.
 */
export function moveItem<T>(list: T[], fromIndex: number, toIndex: number): T[] {
  if (
    fromIndex < 0 ||
    fromIndex >= list.length ||
    toIndex < 0 ||
    toIndex >= list.length ||
    fromIndex === toIndex
  ) {
    return list;
  }
  const copy = [...list];
  const [removed] = copy.splice(fromIndex, 1);
  copy.splice(toIndex, 0, removed);
  return copy;
}

/**
 * Determines whether a theory object has any non-empty content.
 */
export function hasAnyTheoryContent(theory?: GrammarTheoryDto | null): boolean {
  if (!theory) return false;
  if (theory.objectives && theory.objectives.some((o) => Boolean(o && o.trim()))) return true;
  if (theory.sections && theory.sections.length > 0) {
    const hasSectionContent = theory.sections.some(
      (s) =>
        Boolean(s.heading?.trim()) ||
        Boolean(s.explanationVi?.trim()) ||
        Boolean(s.formulas?.length) ||
        Boolean(s.examples?.length) ||
        Boolean(s.notes?.length)
    );
    if (hasSectionContent) return true;
  }
  if (
    theory.commonMistakes &&
    theory.commonMistakes.some(
      (m) => Boolean(m.wrong?.trim()) || Boolean(m.right?.trim()) || Boolean(m.explanationVi?.trim())
    )
  ) {
    return true;
  }
  if (theory.tips && theory.tips.some((t) => Boolean(t && t.trim()))) return true;
  if (theory.summary && theory.summary.some((s) => Boolean(s && s.trim()))) return true;
  return false;
}

/**
 * Validates reject reason (must be non-empty, between 1 and 500 characters).
 */
export function validateRejectReason(reason: string): { valid: boolean; ok: boolean; errorKey?: string } {
  const trimmed = (reason || '').trim();
  if (!trimmed) {
    return { valid: false, ok: false, errorKey: 'admin.grammar.rejectReasonRequired' };
  }
  if (trimmed.length > 500) {
    return { valid: false, ok: false, errorKey: 'admin.grammar.rejectReasonMaxLength' };
  }
  return { valid: true, ok: true };
}

/**
 * Returns query keys to invalidate for each admin action.
 * approve/reject/promote -> moderation + steps; promote -> adds step.
 */
export function getInvalidationKeysFor(
  action: 'approve' | 'reject' | 'promote' | 'updateExercise' | 'updateOfficialExercise' | 'createOfficialExercise' | 'deleteOfficialExercise' | 'generateOfficialExercises' | 'importOfficialExercises',
  stepCode?: string
): (string | readonly string[])[][] {
  const moderationKey = ['admin', 'grammar', 'moderation'];
  const stepsKey = ['admin', 'grammar', 'steps'];

  switch (action) {
    case 'approve':
    case 'reject':
      return [moderationKey, stepsKey];
    case 'promote':
      return stepCode
        ? [moderationKey, stepsKey, ['admin', 'grammar', 'step', stepCode]]
        : [moderationKey, stepsKey];
    case 'updateExercise':
    case 'updateOfficialExercise':
    case 'createOfficialExercise':
    case 'deleteOfficialExercise':
    case 'generateOfficialExercises':
    case 'importOfficialExercises':
      return stepCode ? [['admin', 'grammar', 'step', stepCode], stepsKey] : [stepsKey];
    default:
      return [];
  }
}

export interface ModerationActionButtonsState {
  canApprove: boolean;
  canReject: boolean;
  canPromote: boolean;
  isReadOnly: boolean;
}

/**
 * Pure helper to determine allowed moderation actions based on item moderation status and user permissions.
 * Pending + moderate -> approve/reject; Approved + manage -> promote; insufficient permissions -> none.
 */
export function getModerationActionButtons(
  status: string,
  permissions: { canModerate: boolean; canManage: boolean }
): ModerationActionButtonsState {
  const normStatus = (status || '').toLowerCase();
  const isPending = normStatus === 'pending';
  const isApproved = normStatus === 'approved';
  const isRejected = normStatus === 'rejected';

  const canApprove = isPending && Boolean(permissions.canModerate);
  const canReject = isPending && Boolean(permissions.canModerate);
  const canPromote = isApproved && Boolean(permissions.canManage);
  const isReadOnly = isRejected || (!canApprove && !canReject && !canPromote);

  return {
    canApprove,
    canReject,
    canPromote,
    isReadOnly,
  };
}

export type AnswerIndicatorTone = 'unanswered' | 'correct' | 'partial' | 'wrong' | 'ungraded' | 'failed';

/**
 * Colour category for a question indicator in the attempt header.
 * Only graded answers (`Completed`) are judged by score; other statuses mean "submitted, no score".
 */
export function getAnswerIndicatorTone(
  answer: { status: string; score100?: number | null } | null | undefined
): AnswerIndicatorTone {
  if (!answer) return 'unanswered';
  if (answer.status === 'FailedGrading') return 'failed';
  if (answer.status !== 'Completed' || answer.score100 == null) return 'ungraded';
  if (answer.score100 >= 100) return 'correct';
  if (answer.score100 <= 0) return 'wrong';
  return 'partial';
}
