import type { CefrLevel, ConversationModeCode, TeacherVoiceName } from '@/shared/api/openapi.types';

export type SessionPhase =
  | 'idle'
  | 'connecting'
  | 'listening'
  | 'user_speaking'
  | 'ai_speaking'
  | 'reconnecting'
  | 'error';

export type HubConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting' | 'error';

export interface ThreeHubStates {
  tutorHub: HubConnectionState;
  transcribeHub: HubConnectionState;
  translateHub: HubConnectionState;
}

export interface BilingualTurnItem {
  turnId: string;
  speaker: 'user' | 'ai';
  englishLine1: string;
  vietnameseLine2: string;
  isFinal: boolean;
  timestamp: number;
}

export interface GrammarErrorDetail {
  wrongSegment: string;
  correction: string;
  errorType: string;
  explanationVi: string;
}

export interface GrammarCorrectionItem {
  id?: string;
  conversationId?: string;
  turnId: string;
  hasErrors: boolean;
  originalSentence: string;
  correctedSentence: string;
  errorCategory: string;
  explanationVi: string;
  cefrTip: string;
  naturalAlternative: string;
  overallTipVi: string;
  errors: GrammarErrorDetail[];
  timestamp: number;
}

export interface ConversationSummaryDto {
  id: string;
  title: string;
  cefrLevel: CefrLevel;
  voiceName: TeacherVoiceName;
  speechSpeed: number;
  conversationMode: ConversationModeCode;
  topic: string;
  customSystemPrompt?: string | null;
  extendedThinking: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  messageCount: number;
  correctionCount: number;
  lastMessagePreview: string;
}

export interface PersistedMessageDto {
  id: string | number;
  turnId: string;
  speaker: 'user' | 'ai';
  transcriptLine1: string;
  translationLine2: string;
  createdAtUtc: string;
}

export interface PersistedCorrectionDto {
  id: string | number;
  conversationId?: string;
  turnId: string;
  originalText?: string;
  correctedText?: string;
  errorCategory?: string;
  explanationVi?: string;
  cefrTip?: string;
  originalSentence?: string;
  correctedSentence?: string;
  naturalAlternative?: string;
  overallTipVi?: string;
  errorsJson?: string;
  createdAtUtc?: string;
  timestamp?: string | number;
}

export interface ConversationDetailDto {
  id: string;
  title: string;
  cefrLevel: CefrLevel;
  voiceName: TeacherVoiceName;
  speechSpeed: number;
  conversationMode: ConversationModeCode;
  topic: string;
  customSystemPrompt?: string | null;
  extendedThinking: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  messages: PersistedMessageDto[];
  corrections: PersistedCorrectionDto[];
}

/**
 * Normalizes both Backend SignalR LanguageCorrectionDto / LanguageCorrectionEntity
 * (originalText, correctedText, errorCategory, explanationVi, cefrTip, timestamp/createdAtUtc)
 * and legacy Frontend GrammarCorrectionItem into a unified GrammarCorrectionItem.
 */
export function normalizeGrammarCorrection(raw: unknown): GrammarCorrectionItem | null {
  if (!raw || typeof raw !== 'object') return null;
  const r = raw as Record<string, any>;

  const turnId = String(r.turnId ?? r.TurnId ?? '').trim();
  const originalSentence = String(
    r.originalText ?? r.OriginalText ?? r.originalSentence ?? r.OriginalSentence ?? ''
  ).trim();
  const correctedSentence = String(
    r.correctedText ?? r.CorrectedText ?? r.correctedSentence ?? r.CorrectedSentence ?? ''
  ).trim();

  if (!turnId || (!originalSentence && !correctedSentence)) {
    return null;
  }

  const errorCategory = String(
    r.errorCategory ?? r.ErrorCategory ?? 'Grammar & Vocabulary'
  ).trim();
  const explanationVi = String(
    r.explanationVi ?? r.ExplanationVi ?? r.overallTipVi ?? r.OverallTipVi ?? ''
  ).trim();
  const cefrTip = String(r.cefrTip ?? r.CefrTip ?? '').trim();
  const naturalAlternative = String(
    r.naturalAlternative ?? r.NaturalAlternative ?? ''
  ).trim();

  let parsedErrors: GrammarErrorDetail[] = [];
  if (Array.isArray(r.errors)) {
    parsedErrors = r.errors;
  } else if (typeof r.errorsJson === 'string' && r.errorsJson.trim()) {
    try {
      const parsed = JSON.parse(r.errorsJson);
      if (Array.isArray(parsed)) parsedErrors = parsed;
    } catch {
      parsedErrors = [];
    }
  }

  let ts = Date.now();
  const rawTime = r.timestamp ?? r.Timestamp ?? r.createdAtUtc ?? r.CreatedAtUtc;
  if (typeof rawTime === 'number' && Number.isFinite(rawTime)) {
    ts = rawTime;
  } else if (typeof rawTime === 'string' && rawTime.trim()) {
    const parsedTime = new Date(rawTime).getTime();
    if (!Number.isNaN(parsedTime)) ts = parsedTime;
  }

  const hasErrors =
    typeof r.hasErrors === 'boolean'
      ? r.hasErrors
      : Boolean(originalSentence || correctedSentence);

  return {
    id: r.id ? String(r.id) : r.Id ? String(r.Id) : `corr_${turnId}`,
    conversationId: r.conversationId ? String(r.conversationId) : r.ConversationId ? String(r.ConversationId) : undefined,
    turnId,
    hasErrors,
    originalSentence: sanitizeLiveTranscriptText(originalSentence || correctedSentence),
    correctedSentence: sanitizeLiveTranscriptText(correctedSentence || originalSentence),
    errorCategory,
    explanationVi,
    cefrTip,
    naturalAlternative,
    overallTipVi: explanationVi,
    errors: parsedErrors,
    timestamp: ts,
  };
}

/**
 * Deterministic client-side fallback analyzer so the learner always gets immediate
 * grammar, collocation, and CEFR speaking upgrade feedback even if the network/API is delayed.
 */
export function buildClientGrammarCorrectionFallback(
  turnId: string,
  rawTranscript: string,
  cefrLevel: CefrLevel = 'B1'
): GrammarCorrectionItem | null {
  const cleaned = sanitizeLiveTranscriptText(rawTranscript);
  const words = cleaned.split(' ').filter(Boolean);
  if (words.length < 2) return null;

  const rules: Array<{
    pattern: RegExp;
    replacement: string;
    category: string;
    explanationVi: string;
    tip: string;
  }> = [
    {
      pattern: /\bone\s+(?:of\s+)?my\s+favou?rite\s+([a-z]+\s+)?(movie|book|song|place|food|sport|hobby|scene|actor|game)\b(?!s)/i,
      replacement: 'one of my favorite $1$2s',
      category: 'Grammar (One of + Plural Noun)',
      explanationVi:
        'Sau cấu trúc "one of my favorite...", danh từ đếm được bắt buộc phải ở dạng số nhiều (ví dụ: "one of my favorite comedy movies").',
      tip: 'Hãy phát âm rõ âm cuối /-s/ hoặc /-z/ của danh từ số nhiều.',
    },
    {
      pattern: /\bwatched\s+it\s+in\s+several\s+times?\b/i,
      replacement: 'watched it several times',
      category: 'Grammar (Redundant Preposition)',
      explanationVi:
        'Cụm chỉ tần suất "several times" (vài lần) đi trực tiếp sau tân ngữ, không dùng giới từ "in" phía trước.',
      tip: 'Nói gọn và tự nhiên: "I have watched it several times".',
    },
    {
      pattern: /\bto\s+relax\s+and\s+forgot\b/i,
      replacement: 'to relax and forget',
      category: 'Grammar (Parallel Verb Forms)',
      explanationVi:
        'Trong cấu trúc song song "to relax and...", động từ sau "and" cần giữ nguyên mẫu "forget" tương ứng với "relax", không chia quá khứ "forgot".',
      tip: 'Cấu trúc chuẩn: "a great way to relax and forget about work after a busy day".',
    },
    {
      pattern: /\bto\s+new\s+place\b/i,
      replacement: 'to new places',
      category: 'Grammar (Plural Noun)',
      explanationVi:
        'Khi nói chung về việc đi đến "những địa điểm mới", danh từ đếm được "place" cần để ở dạng số nhiều "new places" (hoặc "a new place").',
      tip: `Chú ý phát âm rõ âm đuôi /ɪz/ trong 'places' (/ˈpleɪ.sɪz/).`,
    },
    {
      pattern: /\ba\s+([aeiou]\w+)\b/i,
      replacement: 'an $1',
      category: 'Grammar (Article a/an)',
      explanationVi:
        'Trước danh từ hoặc tính từ bắt đầu bằng nguyên âm (u, e, o, a, i), cần dùng mạo từ "an" thay vì "a".',
      tip: 'Hãy nối âm mượt giữa "an" và nguyên âm phía sau (ví dụ: an_idea, an_engineer).',
    },
    {
      pattern: /\bdiscussed\s+about\b/i,
      replacement: 'discussed',
      category: 'Grammar & Collocation',
      explanationVi:
        'Động từ "discuss" là ngoại động từ đi trực tiếp với tân ngữ, không đi kèm giới từ "about".',
      tip: 'Ghi nhớ: discuss something = talk about something.',
    },
    {
      pattern: /\btoo\s+many\s+informations\b/i,
      replacement: 'too much information',
      category: 'Uncountable Noun',
      explanationVi:
        '"Information" là danh từ không đếm được nên không thêm "-s" và dùng với "too much".',
      tip: 'Các danh từ không đếm được thông dụng: information, advice, equipment, feedback.',
    },
    {
      pattern: /\bfor\s+(buy|reduce|improve|make|get|learn|practice|fix|solve|manage|receive|remember)\b/i,
      replacement: 'to $1',
      category: "Grammar (Purpose 'to + V')",
      explanationVi:
        'Để diễn đạt mục đích "để làm gì", tiếng Anh dùng cấu trúc "to + V nguyên mẫu" thay vì "for + V".',
      tip: 'Ví dụ: I practice every day to improve my speaking skills.',
    },
    {
      pattern: /\b(two|three|four|five|many|several|different)\s+(time|night|day|year|month|week|problem|bug|query|place|photo|trip|project|service|user)\b(?!s)/i,
      replacement: '$1 $2s',
      category: 'Grammar (Plural Noun)',
      explanationVi:
        'Sau số đếm từ 2 trở lên hoặc các lượng từ "many / several / different", danh từ đếm được phải ở dạng số nhiều (thêm -s/-es).',
      tip: 'Đừng quên bật nhẹ âm cuối /-s/, /-z/ hoặc /-ɪz/ của danh từ số nhiều.',
    },
    {
      pattern: /\bi\s+am\s+agree\b/i,
      replacement: 'I agree',
      category: 'Grammar (Action Verb)',
      explanationVi:
        '"Agree" là động từ thường, không dùng kèm to-be "am/is/are". Hãy nói "I agree" hoặc "I completely agree".',
      tip: 'Nâng cấp tự nhiên: "I couldn’t agree more" hoặc "I completely agree with your point".',
    },
  ];

  for (const rule of rules) {
    if (rule.pattern.test(cleaned)) {
      const fixed = cleaned.replace(rule.pattern, rule.replacement);
      return {
        id: `local_${turnId}`,
        turnId,
        hasErrors: true,
        originalSentence: cleaned,
        correctedSentence: fixed.charAt(0).toUpperCase() + fixed.slice(1),
        errorCategory: rule.category,
        explanationVi: rule.explanationVi,
        cefrTip: `[${cefrLevel}] ${rule.tip}`,
        naturalAlternative: '',
        overallTipVi: rule.explanationVi,
        errors: [],
        timestamp: Date.now(),
      };
    }
  }

  // Polished sentence upgrade for any utterance with 3+ words
  const polished = cleaned
    .replace(/\b(\w+)(\s+\1\b)+/gi, '$1')
    .replace(/\.\s+([a-z])/g, ' $1')
    .trim();
  const formattedPolished =
    (polished.charAt(0).toUpperCase() + polished.slice(1)).replace(/([^.?!])$/, '$1.');

  return {
    id: `upgrade_${turnId}`,
    turnId,
    hasErrors: true,
    originalSentence: cleaned,
    correctedSentence: formattedPolished,
    errorCategory: 'Speaking & Vocabulary Upgrade',
    explanationVi:
      'Câu nói của bạn có ý nghĩa rõ ràng! Hãy giữ nhịp nói liền mạch, nhấn trọng âm vào các từ khóa chính và bật rõ âm cuối.',
    cefrTip: `[${cefrLevel}] Bạn có thể mở rộng câu trả lời bằng liên từ "because...", "specifically...", hoặc "as a result..." để tăng điểm Fluency & Coherence.`,
    naturalAlternative: `In my experience, ${formattedPolished.charAt(0).toLowerCase() + formattedPolished.slice(1)}`,
    overallTipVi:
      'Câu nói của bạn có ý nghĩa rõ ràng! Hãy giữ nhịp nói liền mạch và mở rộng thêm ví dụ thực tế.',
    errors: [],
    timestamp: Date.now(),
  };
}

/**
 * Cleans progressive ASR repetition echoes from a transcript string
 * (e.g. "Yes, one mile Yes, one my favorites Yes, one my favourite comedy movie is Home Alone")
 * so the chat bubble and grammar analyzer only see the clean final utterance.
 */
export function sanitizeLiveTranscriptText(raw: string): string {
  if (!raw || !raw.trim()) return '';
  let text = raw.replace(/\s+/g, ' ').trim();
  if (text.length < 12) return text;

  const stripPunct = (w: string) => w.toLowerCase().replace(/[^a-z0-9']/g, '');
  const wordsRoughlyMatch = (a: string, b: string) => {
    if (!a || !b) return false;
    if (a === b) return true;
    // Handle favourite vs favorite
    if (a.replace('our', 'or') === b.replace('our', 'or')) return true;
    return false;
  };

  // 1. Collapse progressive restarts where a 2-word anchor repeats and the following slice overlaps
  const tokens = text.split(' ');
  if (tokens.length >= 6) {
    const cleaned: string[] = [];
    const cleanedIndices: number[] = [];
    let i = 0;
    while (i < tokens.length) {
      cleaned.push(tokens[i]);
      cleanedIndices.push(i);

      if (cleaned.length >= 2) {
        const w1 = stripPunct(cleaned[cleaned.length - 2]);
        const w2 = stripPunct(cleaned[cleaned.length - 1]);
        const startIdx = cleanedIndices[cleanedIndices.length - 2];

        if (w1 && w2) {
          let restartIdx = -1;
          const limit = Math.min(tokens.length - 1, i + 40);
          for (let j = i + 1; j < limit; j++) {
            if (
              !wordsRoughlyMatch(w1, stripPunct(tokens[j])) ||
              !wordsRoughlyMatch(w2, stripPunct(tokens[j + 1]))
            ) {
              continue;
            }

            const spanLen = j - startIdx;
            const draftWords = tokens
              .slice(startIdx, j)
              .map(stripPunct)
              .filter(Boolean);
            const nextSliceSet = new Set(
              tokens
                .slice(j, Math.min(tokens.length, j + spanLen + 8))
                .map((x) => stripPunct(x).replace('our', 'or'))
                .filter(Boolean)
            );
            const matchCount = draftWords.filter((dw) =>
              nextSliceSet.has(dw.replace('our', 'or'))
            ).length;
            const overlapRatio =
              draftWords.length > 0 ? matchCount / draftWords.length : 0;

            if (overlapRatio >= 0.55) {
              restartIdx = j;
            }
          }
          if (restartIdx > i) {
            cleaned.pop();
            cleaned.pop();
            cleanedIndices.pop();
            cleanedIndices.pop();
            i = restartIdx;
            continue;
          }
        }
      }
      i++;
    }
    text = cleaned.join(' ');
  }

  // 2. Collapse consecutive duplicate words ("watched watched" -> "watched", "I I" -> "I")
  text = text.replace(/\b(\w+)(\s+\1\b)+/gi, '$1');

  // 3. Deduplicate repeated sentences that share >= 65% words
  const sentences = text.split(/(?<=[.!?])\s+/).filter(Boolean);
  if (sentences.length >= 2) {
    const deduped: string[] = [sentences[0]];
    for (let sIdx = 1; sIdx < sentences.length; sIdx++) {
      const prev = deduped[deduped.length - 1];
      const curr = sentences[sIdx];
      const prevWords = prev.toLowerCase().match(/[a-z']+/g) || [];
      const currWords = curr.toLowerCase().match(/[a-z']+/g) || [];
      if (prevWords.length >= 3 && currWords.length >= 3) {
        const setPrev = new Set(prevWords.map((w) => w.replace('our', 'or')));
        const overlap = currWords.filter((w) => setPrev.has(w.replace('our', 'or'))).length;
        const ratio = overlap / Math.min(prevWords.length, currWords.length);
        if (ratio >= 0.65) {
          if (curr.length >= prev.length) {
            deduped[deduped.length - 1] = curr;
          }
          continue;
        }
      }
      deduped.push(curr);
    }
    text = deduped.join(' ');
  }

  return text.trim();
}
