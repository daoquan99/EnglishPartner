import { describe, it, expect } from 'vitest';
import { vi as viLocale } from '@/shared/i18n/locales/vi';
import { en as enLocale } from '@/shared/i18n/locales/en';

declare const require: any;
declare const __dirname: string;

const fs = require('node:fs');
const path = require('node:path');

function getFiles(dir: string): string[] {
  let results: string[] = [];
  const list = fs.readdirSync(dir);
  for (const file of list) {
    const full = path.join(dir, file);
    const stat = fs.statSync(full);
    if (stat && stat.isDirectory()) {
      if (file !== '__tests__') {
        results = results.concat(getFiles(full));
      }
    } else if (file.endsWith('.ts') || file.endsWith('.tsx')) {
      results.push(full);
    }
  }
  return results;
}

function getNestedValue(obj: any, keyPath: string): any {
  const parts = keyPath.split('.');
  let current = obj;
  for (const part of parts) {
    if (current === undefined || current === null) return undefined;
    current = current[part];
  }
  return current;
}

describe('Progress i18n literal keys verification', () => {
  it('all literal keys used in t(...) exist in both vi and en locales', () => {
    const progressDir = path.resolve(__dirname, '..');
    const progressFiles = getFiles(progressDir);
    const extraFiles = [
      path.resolve(__dirname, '../../../app/layouts/StudentLayout.tsx'),
    ];

    const allFiles = [...progressFiles, ...extraFiles];
    const tCallRegex = /(?:^|[^a-zA-Z0-9_])t\(\s*['"]([a-zA-Z0-9_.]+)['"]/g;
    const extractedKeys = new Set<string>();

    for (const filePath of allFiles) {
      const content = fs.readFileSync(filePath, 'utf8');
      let match: RegExpExecArray | null;
      while ((match = tCallRegex.exec(content)) !== null) {
        // Only inspect progress.* and nav.progress* keys (other keys are covered by other tests)
        if (match[1].startsWith('progress.') || match[1].startsWith('nav.progress')) {
          extractedKeys.add(match[1]);
        }
      }
    }

    const missingInVi: string[] = [];
    const missingInEn: string[] = [];

    for (const key of Array.from(extractedKeys).sort()) {
      if (getNestedValue(viLocale, key) === undefined) {
        missingInVi.push(key);
      }
      if (getNestedValue(enLocale, key) === undefined) {
        missingInEn.push(key);
      }
    }

    expect(missingInVi, `Keys missing in vi locale: ${missingInVi.join(', ')}`).toEqual([]);
    expect(missingInEn, `Keys missing in en locale: ${missingInEn.join(', ')}`).toEqual([]);
  });
});
