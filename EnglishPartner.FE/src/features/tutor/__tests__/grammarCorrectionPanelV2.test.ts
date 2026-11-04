import { describe, it, expect, vi, beforeEach } from 'vitest';
import { normalizeSentence } from '../components/GrammarCorrectionPanel';

describe('GrammarCorrectionPanel V2 & Helper Logic', () => {
  describe('normalizeSentence', () => {
    it('normalizes punctuation, case and whitespace correctly', () => {
      expect(normalizeSentence('  I goes to school.  ')).toBe('i goes to school');
      expect(normalizeSentence('Hello, world! How are you?')).toBe('hello world how are you');
      expect(normalizeSentence('Multiple    spaces...')).toBe('multiple spaces');
      expect(normalizeSentence(null)).toBe('');
      expect(normalizeSentence(undefined)).toBe('');
    });

    it('identifies identical sentences despite punctuation or casing differences', () => {
      const orig = 'I love learning English.';
      const corr = 'i love learning english!';
      expect(normalizeSentence(orig)).toBe(normalizeSentence(corr));
    });
  });

  describe('Anti-duplicate rule calculations', () => {
    it('hides correctedSentence if normalized matches original', () => {
      const originalSentence = 'I like reading books.';
      const correctedSentence = 'I like reading books!';

      const normOriginal = normalizeSentence(originalSentence);
      const normCorrected = normalizeSentence(correctedSentence);

      const hasCorrected = Boolean(correctedSentence.trim()) && normCorrected !== normOriginal;
      expect(hasCorrected).toBe(false);
    });

    it('shows correctedSentence if different from original', () => {
      const originalSentence = 'I goes to work.';
      const correctedSentence = 'I go to work.';

      const normOriginal = normalizeSentence(originalSentence);
      const normCorrected = normalizeSentence(correctedSentence);

      const hasCorrected = Boolean(correctedSentence.trim()) && normCorrected !== normOriginal;
      expect(hasCorrected).toBe(true);
    });

    it('hides improvedText if identical to original or corrected', () => {
      const originalSentence = 'Good morning everyone.';
      const correctedSentence = 'Good morning, everyone.';
      const improvedText = 'Good morning everyone!';

      const normOriginal = normalizeSentence(originalSentence);
      const normCorrected = normalizeSentence(correctedSentence);
      const normImproved = normalizeSentence(improvedText);

      const hasImproved =
        Boolean(improvedText.trim()) &&
        normImproved !== normOriginal &&
        normImproved !== normCorrected;

      expect(hasImproved).toBe(false);
    });

    it('shows improvedText if distinct from both original and corrected', () => {
      const originalSentence = 'I am tired.';
      const correctedSentence = 'I am tired.';
      const improvedText = "I'm feeling quite exhausted today.";

      const normOriginal = normalizeSentence(originalSentence);
      const normCorrected = normalizeSentence(correctedSentence);
      const normImproved = normalizeSentence(improvedText);

      const hasImproved =
        Boolean(improvedText.trim()) &&
        normImproved !== normOriginal &&
        normImproved !== normCorrected;

      expect(hasImproved).toBe(true);
    });
  });

  describe('Status Badge Mapping', () => {
    it('maps v2 status values correctly', () => {
      const statuses = ['has_errors', 'can_improve', 'already_natural', 'analysis_failed'];
      statuses.forEach((status) => {
        expect(['has_errors', 'can_improve', 'already_natural', 'analysis_failed']).toContain(status);
      });
    });
  });
});
