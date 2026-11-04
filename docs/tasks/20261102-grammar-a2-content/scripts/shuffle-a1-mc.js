// Spread the correct option position of A1 MultipleChoice exercises (56/60 were first).
// Only reorders content.options arrays; ids, texts and grading stay identical.
const fs = require('fs');
const F = process.argv[2];
const raw = fs.readFileSync(F, 'utf8');
const nl = raw.includes('\r\n') ? '\r\n' : '\n';
const text = raw.replace(/\r\n/g, '\n');
const p = JSON.parse(text);
const trailing = text.endsWith('\n') ? '\n' : '';
if (JSON.stringify(p, null, 2) + trailing !== text) throw new Error('pack is not JSON.stringify(2)-formatted; refusing to rewrite');

let seed = 20261103;
const rnd = () => { seed = (seed * 1103515245 + 12345) % 2147483648; return seed / 2147483648; };
const shuffle = arr => { const a = [...arr]; for (let i = a.length - 1; i > 0; i--) { const j = Math.floor(rnd() * (i + 1)); [a[i], a[j]] = [a[j], a[i]]; } return a; };

const mcs = p.steps.flatMap(s => s.exercises || []).filter(e => e.type === 'MultipleChoice');
let n = 0;
for (const e of mcs) {
  const o = e.content.options;
  const corr = o.filter(x => e.grading.correctOptionIds.includes(x.id));
  if (corr.length !== 1) throw new Error('expected single correct option: ' + e.key);
  const rest = shuffle(o.filter(x => x !== corr[0]));
  rest.splice((n++ * 3 + 1) % o.length, 0, corr[0]);
  if (rest.length !== o.length || new Set(rest.map(x => x.id)).size !== o.length) throw new Error('bad shuffle ' + e.key);
  e.content.options = rest;
}
fs.writeFileSync(F, (JSON.stringify(p, null, 2) + trailing).replace(/\n/g, nl));
const pos = {};
mcs.forEach(e => { const k = e.content.options.findIndex(o => e.grading.correctOptionIds.includes(o.id)); pos[k] = (pos[k] || 0) + 1; });
console.log('MC', mcs.length, 'correct position counts', pos);
