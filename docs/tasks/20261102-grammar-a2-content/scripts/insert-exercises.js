// Insert "exercises" arrays into a grammar content pack by text, leaving all other text untouched.
// usage: node insert-exercises.js <pack.json> <exercises.json>
const fs = require('fs');
const [packF, exF] = process.argv.slice(2);
const text = fs.readFileSync(packF, 'utf8');
const nl = text.includes('\r\n') ? '\r\n' : '\n';
const ex = JSON.parse(fs.readFileSync(exF, 'utf8'));
const pack = JSON.parse(text);
if (pack.steps.some(s => s.exercises)) throw new Error('pack already has exercises');

// find the end (closing brace index) of each step object by scanning with string awareness
const steps = []; // {code, closeIdx}
let depth = 0, inStr = false, esc = false, stepsArrDepth = -1, stepStart = -1;
const stepsKey = text.indexOf('"steps"');
const arrOpen = text.indexOf('[', stepsKey);
for (let i = arrOpen; i < text.length; i++) {
  const c = text[i];
  if (inStr) { if (esc) esc = false; else if (c === '\\') esc = true; else if (c === '"') inStr = false; continue; }
  if (c === '"') { inStr = true; continue; }
  if (c === '[' || c === '{') {
    depth++;
    if (c === '{' && depth === 2) stepStart = i;
  } else if (c === ']' || c === '}') {
    if (c === '}' && depth === 2) steps.push({ start: stepStart, close: i });
    depth--;
    if (depth === 0) break;
  }
}
if (steps.length !== pack.steps.length) throw new Error(`found ${steps.length} steps, expected ${pack.steps.length}`);

let out = text;
for (let k = steps.length - 1; k >= 0; k--) {
  const code = pack.steps[k].code;
  const list = ex[code];
  if (!list) throw new Error('missing exercises for ' + code);
  const segment = text.slice(steps[k].start, steps[k].close);
  if (!segment.includes(`"code": "${code}"`)) throw new Error('step order mismatch at ' + code);
  // indentation of step properties = indentation of the closing brace + 2
  const lineStart = text.lastIndexOf('\n', steps[k].close) + 1;
  const braceIndent = text.slice(lineStart, steps[k].close);
  const propIndent = braceIndent + '  ';
  const body = JSON.stringify(list, null, 2).split('\n').map((l, i) => (i === 0 ? l : propIndent + l)).join(nl);
  // insert after the last non-whitespace char before the closing brace
  let j = steps[k].close - 1;
  while (/\s/.test(out[j])) j--;
  out = out.slice(0, j + 1) + ',' + nl + propIndent + '"exercises": ' + body + out.slice(j + 1);
}
const check = JSON.parse(out);
const total = check.steps.reduce((a, s) => a + s.exercises.length, 0);
// everything except exercises must be identical
for (const s of check.steps) delete s.exercises;
if (JSON.stringify(check) !== JSON.stringify(pack)) throw new Error('non-exercise content changed');
fs.writeFileSync(packF, out);
console.log('inserted', total, 'exercises into', steps.length, 'steps');
