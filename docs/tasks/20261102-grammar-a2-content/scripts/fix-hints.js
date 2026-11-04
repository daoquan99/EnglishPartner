// Round 2 fixes after code review: hints that give away the answer, maxLength, and a few B1 items.
// usage: node fix-hints.js <a2-exercises.json> <b1-exercises.json>
const fs = require('fs');
const [A2F, B1F] = process.argv.slice(2);
const files = { A2: JSON.parse(fs.readFileSync(A2F, 'utf8')), B1: JSON.parse(fs.readFileSync(B1F, 'utf8')) };
const by = {};
for (const p of Object.values(files)) for (const l of Object.values(p)) for (const e of l) by[e.key] = e;
const ex = k => { if (!by[k]) throw new Error('missing ' + k); return by[k]; };
const log = [];

// Replace the hint of a blank, both in blanks[].hint and in the "(…)" shown on the blank's line.
function hint(key, blank, newHint) {
  const e = ex(key);
  const b = e.content.blanks.find(x => x.id === blank);
  if (!b) throw new Error(key + ' ' + blank);
  const old = b.hint;
  const lines = e.content.textWithBlanks.split('\n');
  const i = lines.findIndex(l => l.includes(`[${blank}]`));
  if (i < 0) throw new Error('marker ' + key + ' ' + blank);
  const at = lines[i].lastIndexOf(old);
  if (at >= 0) lines[i] = lines[i].slice(0, at) + newHint + lines[i].slice(at + old.length);
  e.content.textWithBlanks = lines.join('\n');
  b.hint = newHint;
  log.push(`${key} ${blank}: hint ${old} → ${newHint}`);
}
function accept(key, blank, answers) {
  const b = ex(key).grading.blankAnswers[blank];
  for (const a of answers) if (!b.acceptedAnswers.includes(a)) b.acceptedAnswers.push(a);
  log.push(`${key} ${blank}: chấp nhận thêm ${answers.map(a => `"${a}"`).join(', ')}`);
}

// ---- A2: hints that name the very word to choose
hint('A2-07-E06', 'blank-3', '(shall / will)');
hint('A2-07-E07', 'blank-3', '(will / shall)');
hint('A2-11-E06', 'blank-3', '(same / similar)');
hint('A2-11-E06', 'blank-4', '(from / than)');
hint('A2-11-E07', 'blank-3', '(as / than)');
hint('A2-13-E06', 'blank-1', '(must not / don\'t have to)');
hint('A2-13-E06', 'blank-3', '(should / must)');
hint('A2-13-E07', 'blank-1', '(must not / don\'t have to)');
hint('A2-13-E07', 'blank-4', '(should / must)');
hint('A2-14-E06', 'blank-2', '(have / did)');
hint('A2-14-E06', 'blank-4', '(have / has)');
hint('A2-14-E07', 'blank-2', '(never / ever)');
for (const b of ['blank-1', 'blank-2', 'blank-3', 'blank-4']) hint('A2-15-E06', b, '(just / already / yet)');
hint('A2-15-E07', 'blank-1', '(just / already / yet)');
hint('A2-17-E06', 'blank-1', '(too / enough)');
hint('A2-17-E06', 'blank-2', '(too / enough)');
hint('A2-17-E06', 'blank-3', '(too / enough)');
hint('A2-17-E06', 'blank-4', '(much / many)');
hint('A2-17-E07', 'blank-2', '(too / enough)');
hint('A2-17-E07', 'blank-3', '(much / many)');
hint('A2-17-E07', 'blank-4', '(too / enough)');
hint('A2-18-E06', 'blank-1', '(so / because)');
hint('A2-18-E06', 'blank-2', '(so / because)');
hint('A2-18-E07', 'blank-4', '(so / because)');
hint('A2-19-E07', 'blank-2', '(few / little)');
hint('A2-19-E07', 'blank-3', '(few / little)');
hint('A2-19-E07', 'blank-4', '(much / many)');
hint('A2-20-E07', 'blank-4', '(few / little)');

// ---- B1
hint('B1-03-E06', 'blank-4', '(would / used to)');
hint('B1-03-E07', 'blank-4', '(would / used to)');
hint('B1-11-E07', 'blank-2', '(if / whether)');
hint('B1-11-E07', 'blank-4', '(if / whether)');
hint('B1-18-E07', 'blank-1', '(if / whether)');
hint('B1-13-E07', 'blank-1', '(might / must / can\'t)');
hint('B1-13-E07', 'blank-3', '(must / can\'t)');
hint('B1-15-E06', 'blank-1', '(so / such)');
hint('B1-15-E06', 'blank-2', '(so / such)');
hint('B1-15-E07', 'blank-1', '(so / such)');
hint('B1-15-E07', 'blank-2', '(that / than)');
hint('B1-16-E06', 'blank-3', '(will / would)');
hint('B1-16-E07', 'blank-2', '(can)');
hint('B1-17-E06', 'blank-1', '(bỏ cuộc)');
hint('B1-17-E06', 'blank-3', '(dùng hết)');
hint('B1-17-E06', 'blank-4', '(chăm sóc)');
hint('B1-17-E07', 'blank-2', '(dùng hết)');
hint('B1-17-E07', 'blank-3', '(hòa hợp với)');
// remaining B1-17 blanks also named the phrasal verb; give the meaning instead
for (const [k, b, h] of [['B1-17-E06', 'blank-2', '(tìm kiếm)'], ['B1-17-E07', 'blank-1', '(tình cờ thấy)'], ['B1-17-E07', 'blank-4', '(xuống xe)']]) {
  const cur = ex(k).content.blanks.find(x => x.id === b).hint;
  if (/look for|come across|get off/.test(cur)) hint(k, b, h);
}

// B1-14-E07 blank-4: "We stopped ___ photos" also fits "taking"; add an object so only stop + to V works
{
  const e = ex('B1-14-E07');
  const before = e.content.textWithBlanks;
  e.content.textWithBlanks = before.replace('4. We stopped [blank-4] photos of the beautiful lake.', '4. We stopped the car [blank-4] photos of the beautiful lake.');
  if (before === e.content.textWithBlanks) throw new Error('B1-14-E07 text');
  log.push('B1-14-E07 blank-4: "We stopped [ ] photos" → "We stopped the car [ ] photos" (trước đó "taking" cũng đúng)');
}
accept('B1-03-E07', 'blank-2', ['got used to', 'have got used to', '\'ve got used to']);
accept('B1-03-E07', 'blank-3', ['had been living', '\'d been living']);
{
  const e = ex('B1-11-E08');
  const t = e.content.tokens.find(x => x.text === 'last');
  if (!t) throw new Error('B1-11-E08 last');
  t.text = 'the previous';
  log.push('B1-11-E08: "last year" → "the previous year" (đổi trạng từ thời gian trong câu gián tiếp)');
}
{
  const e = ex('B1-18-E02');
  e.content.question = 'Your passport ____ at the airport tomorrow, so keep it ready.';
  const map = { 'will be announced': 'will be checked', 'will announce': 'will check', 'are announcing': 'are checking', 'will been announced': 'will been checked' };
  for (const o of e.content.options) { if (!map[o.text]) throw new Error('B1-18-E02 option ' + o.text); o.text = map[o.text]; }
  e.grading.explanation = 'Hộ chiếu được kiểm tra (bị động tương lai): will be + V3. "will check" sai vì hộ chiếu không tự kiểm tra.';
  log.push('B1-18-E02: đổi câu để không gần trùng B1-08-E02 (passport will be checked)');
}

// ---- maxLength must fit every accepted answer (A2 and B1)
for (const e of Object.values(by)) {
  if (e.type !== 'FillInTheBlanks') continue;
  for (const b of e.content.blanks) {
    const max = Math.max(...e.grading.blankAnswers[b.id].acceptedAnswers.map(a => a.length));
    if (b.maxLength && b.maxLength < max) { log.push(`${e.key} ${b.id}: maxLength ${b.maxLength} → ${max}`); b.maxLength = max; }
  }
}

fs.writeFileSync(A2F, JSON.stringify(files.A2, null, 2) + '\n');
fs.writeFileSync(B1F, JSON.stringify(files.B1, null, 2) + '\n');
console.log(log.join('\n'));
console.log('total', log.length);
