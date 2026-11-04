// Claude's corrections to the Codex-drafted B1 exercises (self-solved review).
const fs = require('fs');
const F = process.argv[2];
const p = JSON.parse(fs.readFileSync(F, 'utf8'));
const by = {}; for (const l of Object.values(p)) for (const e of l) by[e.key] = e;
const ex = k => { if (!by[k]) throw new Error('missing ' + k); return by[k]; };
const log = [];

// Matching: rewrite pairs so every left item matches exactly one right item.
// pairs = [[left, right], ...]; right items are placed in a fixed shuffled order.
function matching(key, pairs, note) {
  const e = ex(key);
  if (e.type !== 'Matching') throw new Error(key + ' not matching');
  const n = pairs.length;
  const order = [2, 4, 0, 3, 1, 5].filter(i => i < n); // derangement-ish order for right column
  if (order.length !== n) throw new Error('order ' + key);
  e.content.leftItems = pairs.map(([l], i) => ({ id: `l-${i + 1}`, text: l }));
  e.content.rightItems = order.map((src, pos) => ({ id: `r-${pos + 1}`, text: pairs[src][1] }));
  e.grading.correctPairs = pairs.map((_, i) => ({ leftId: `l-${i + 1}`, rightId: `r-${order.indexOf(i) + 1}` }));
  log.push(`${key}: viết lại cặp ghép (${note})`);
}

// FillInTheBlanks: add accepted answers to a blank.
function accept(key, blank, answers, note) {
  const e = ex(key);
  const b = e.grading.blankAnswers[blank];
  if (!b) throw new Error(key + ' ' + blank);
  for (const a of answers) if (!b.acceptedAnswers.includes(a)) b.acceptedAnswers.push(a);
  const max = Math.max(...b.acceptedAnswers.map(a => a.length));
  const def = e.content.blanks.find(x => x.id === blank);
  if (def.maxLength && def.maxLength < max) def.maxLength = max;
  log.push(`${key} ${blank}: chấp nhận thêm ${answers.map(a => `"${a}"`).join(', ')} (${note})`);
}

// ---- Matching cross-match fixes
matching('B1-01-E05', [
  ['How long have you been waiting?', 'Since two o\'clock.'],
  ['Why are your shoes muddy?', 'I have been walking through the garden.'],
  ['How long has Lan known Minh?', 'For about five years.'],
  ['Why does the kitchen smell of soap?', 'I have been cleaning it.'],
  ['Is it still raining?', 'Yes, it has been raining since dawn.'],
], '"What have you been doing all morning?" ghép được với cả hai câu trả lời V-ing');
matching('B1-04-E05', [
  ['What will you be doing at 7 a.m. tomorrow?', 'I will be catching the bus to work.'],
  ['Where will your parents be this time next month?', 'They will be lying on a beach in Nha Trang.'],
  ['Why won\'t Nam answer the phone at eight?', 'He will be attending an online meeting.'],
  ['Why can\'t I call Hoa at noon on Saturday?', 'She will be playing tennis with Mai.'],
  ['Will you be using this computer later?', 'No, you can borrow it.'],
], 'cụm thời gian ghép với bất kỳ hành động nào cũng đúng');
matching('B1-05-E05', [
  ['If you heat butter,', 'it melts.'],
  ['If you don\'t take an umbrella,', 'you will get wet.'],
  ['Unless we leave now,', 'we will be late for the film.'],
  ['If Hoa calls,', 'tell her I am in a meeting.'],
  ['If I finish work early,', 'I will meet you at the café.'],
], '"If you don\'t hurry" ghép được với "we will be late"');
matching('B1-06-E05', [
  ['If Ba had a bike,', 'he wouldn\'t need to take the bus.'],
  ['If Mai knew his number,', 'she would call him.'],
  ['If they practised more,', 'their English would improve.'],
  ['If I were you,', 'I would apologise to Lan.'],
  ['If the weather were warmer,', 'we could eat outside.'],
], '"If I knew her number" và "If I were you" ghép chéo được');
matching('B1-07-E05', [
  ['They grow coffee in Buon Ma Thuot.', 'Coffee is grown in Buon Ma Thuot.'],
  ['The postman delivered the letters yesterday.', 'The letters were delivered yesterday.'],
  ['The city opened this museum in 2012.', 'This museum was opened in 2012.'],
  ['People send millions of emails every day.', 'Millions of emails are sent every day.'],
  ['The flood destroyed the old bridge.', 'The old bridge was destroyed by the flood.'],
], 'chủ ngữ + vị ngữ bị động ghép chéo được; đổi thành cặp chủ động → bị động');
ex('B1-07-E05').grading.explanation = 'Mỗi câu chủ động chuyển sang bị động: tân ngữ thành chủ ngữ, động từ thành be + V3 đúng thì.';
matching('B1-08-E05', [
  ['They will send the tickets by email.', 'The tickets will be sent by email.'],
  ['Someone is cleaning the classroom now.', 'The classroom is being cleaned now.'],
  ['We have already completed the project.', 'The project has already been completed.'],
  ['You must sign the documents by Friday.', 'The documents must be signed by Friday.'],
  ['They are going to demolish the building next month.', 'The building is going to be demolished next month.'],
], 'chủ ngữ + vị ngữ bị động ghép chéo được; đổi thành cặp chủ động → bị động');
ex('B1-08-E05').grading.explanation = 'Bị động giữ thì/modal của câu gốc: will be V3, is being V3, has been V3, must be V3, is going to be V3.';
matching('B1-09-E05', [
  ['The woman who called you', 'is my aunt.'],
  ['The phone that I bought', 'stopped working yesterday.'],
  ['The boy whose bike was stolen', 'is looking for his bike everywhere.'],
  ['The restaurant where we ate', 'has closed.'],
  ['The day when we first met', 'was rainy.'],
], '"The woman who called you" ghép được với "reported it to the police"');

// ---- FillInTheBlanks
{
  const e = ex('B1-05-E07');
  e.content.textWithBlanks = e.content.textWithBlanks.replace('If she [blank-3] hurry,', 'If she [blank-3],');
  log.push('B1-05-E07 blank-3: bỏ chữ "hurry" thừa sau chỗ trống (đáp án "doesn\'t hurry" tạo thành "doesn\'t hurry hurry")');
}
{
  const e = ex('B1-13-E06');
  e.content.textWithBlanks = e.content.textWithBlanks.replace('4. That [blank-4] be true! (be)', '4. That [blank-4] be true! (can/not)');
  e.content.blanks.find(b => b.id === 'blank-4').hint = '(can/not)';
  log.push('B1-13-E06 blank-4: sửa hint "(be)" thành "(can/not)" (chỗ trống chỉ điền modal)');
}
accept('B1-01-E06', 'blank-1', ['have waited', '\'ve waited'], 'với for, hiện tại hoàn thành đơn cũng đúng');
accept('B1-01-E07', 'blank-1', ['has been teaching', '\'s been teaching'], 'teach với since dùng được cả hai dạng');
accept('B1-01-E07', 'blank-2', ['have stood', '\'ve stood'], 'với for, hiện tại hoàn thành đơn cũng đúng');
accept('B1-06-E06', 'blank-3', ['was'], '"If I was you" chấp nhận trong văn nói');
accept('B1-06-E07', 'blank-3', ['wasn\'t', 'was not'], 'dạng was cũng chấp nhận');
accept('B1-09-E07', 'blank-4', ['that'], '"the day that we first met" đúng');
accept('B1-10-E07', 'blank-3', ['cleaned'], 'không lùi thì vẫn chấp nhận');
accept('B1-11-E07', 'blank-2', ['if'], 'if và whether đều đúng');
accept('B1-11-E07', 'blank-4', ['whether'], 'if và whether đều đúng');
accept('B1-12-E07', 'blank-4', ['won\'t you', 'would you', 'can you', 'could you'], 'câu mệnh lệnh có nhiều đuôi hợp lệ');
accept('B1-13-E07', 'blank-1', ['may', 'could'], 'might/may/could cùng chỉ khả năng');
accept('B1-16-E06', 'blank-4', ['was'], '"If only I was" chấp nhận trong văn nói');
accept('B1-18-E07', 'blank-1', ['if'], 'if và whether đều đúng');

// ---- SentenceUnscramble: extra valid order
{
  const e = ex('B1-04-E08');
  const id = t => e.content.tokens.find(x => x.text === t).id;
  e.grading.validSequences.push(['I', 'will', 'be', 'working', 'at', 'ten', 'in', 'the', 'garden', 'tomorrow.'].map(id));
  log.push('B1-04-E08: chấp nhận thêm "I will be working at ten in the garden tomorrow."');
}

fs.writeFileSync(F, JSON.stringify(p, null, 2) + '\n');
console.log(log.join('\n'));
console.log('total changes', log.length);
