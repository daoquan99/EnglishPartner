const PUNCTUATION_AND_QUOTES_REGEX = /^[\p{P}`~^]+|[\p{P}`~^]+$/gu;
const WHITESPACE_REGEX = /\s+/g;

/**
 * Normalizes raw text from user selection:
 * 1. Trim leading and trailing whitespace
 * 2. Strip leading and trailing punctuation & quotation marks using Unicode \p{P}
 *    matching .NET char.IsPunctuation (including …, —, –, curly quotes, brackets, etc.)
 * 3. Collapse multiple whitespace characters into a single space
 */
export function normalizeTargetText(raw: string | null | undefined): string {
  if (!raw) return '';
  let text = raw.trim();
  if (!text) return '';

  // Repeatedly strip leading/trailing punctuation and quotes
  let prev = '';
  while (text && text !== prev) {
    prev = text;
    text = text.replace(PUNCTUATION_AND_QUOTES_REGEX, '').trim();
  }

  return text.replace(WHITESPACE_REGEX, ' ');
}

/**
 * Counts the number of words in a normalized string (splitting by space after normalization).
 */
export function countWords(normalizedText: string): number {
  if (!normalizedText || !normalizedText.trim()) return 0;
  return normalizedText.trim().split(' ').filter(Boolean).length;
}

/**
 * Checks whether the text should be treated as a sentence (> 6 words) or a single term/phrase (<= 6 words).
 */
export function isSentenceMode(normalizedText: string): boolean {
  return countWords(normalizedText) > 6;
}
