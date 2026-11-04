import { describe, expect, it } from 'vitest';
import { readTimeout } from '../timeouts';

describe('AI request timeout config', () => {
  const thirtyMinutes = 30 * 60 * 1000;

  it('defaults to 30 minutes when unset or invalid', () => {
    expect(readTimeout(undefined)).toBe(thirtyMinutes);
    expect(readTimeout('')).toBe(thirtyMinutes);
    expect(readTimeout('abc')).toBe(thirtyMinutes);
    expect(readTimeout('0')).toBe(thirtyMinutes);
    expect(readTimeout('-5')).toBe(thirtyMinutes);
  });

  it('accepts a positive override', () => {
    expect(readTimeout('600000')).toBe(600_000);
  });

  it('rejects values above the browser timer limit, which would fire immediately', () => {
    expect(readTimeout('3000000000')).toBe(thirtyMinutes);
  });
});
