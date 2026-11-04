# Review bộ bài core A2 (C2)

- Nguồn: AGY soạn `a2-exercises.json` (200 bài, 20 step × 10). Claude tự giải từng bài, sửa, rồi kiểm lại bằng script.
- **Bạn cần làm:** đọc phần "Đã sửa" và lướt danh sách bài. Bài nào cần đổi thì ghi chú theo key (ví dụ `A2-07-E03: …`). Không có ghi chú thì coi như đã duyệt, Claude gộp vào `grammar-a2.json`.

## Kết quả kiểm

| Kiểm tra | Kết quả |
| --- | --- |
| 20 step × 10 bài, key không trùng, phân bố type đúng bảng | Đạt |
| Trùng câu với A1 | 0 |
| Đáp án đúng/sai do Claude tự giải | 33 bài phải sửa (không tính việc xáo option MC), xem bên dưới |
| Vị trí đáp án đúng của MC | Trước khi sửa: 56/60 nằm ở option đầu. Sau khi sửa: rải đều 15/15/15/15 |
| SentenceUnscramble | Đã xáo 2 bài có token xếp sẵn đúng thứ tự đáp án. Đã thêm trật tự hợp lệ cho 4 bài có token trùng chữ hoặc có cách xếp khác |
| Validator và importer thật (`GrammarBundledContentPackTests`) | Chạy sau khi gộp (ghi ở report) |

## Đã sửa

Nhóm lỗi:

1. **Matching ghép chéo vẫn đúng.** Ví dụ ở A2-01-E05, vế "I was" ghép được với nhiều vế sau. Đã viết lại 5 bài thành cặp hỏi–đáp hoặc cặp chỉ hợp nghĩa duy nhất.
2. **Distractor của MC cũng đúng.** Ví dụ "He went to the market. He will be back soon." là câu đúng. Đã thay distractor ở 5 bài.
3. **FillInTheBlanks thiếu mốc thời gian**, nên dùng hiện tại đơn cũng đúng. Đã thêm "yesterday" hoặc "last week" ở 5 câu.
4. **Dùng cấu trúc của step sau.** Đó là `we're staying` (A2-08) trong A2-06, `I have ever seen` và `yet` trước step tương ứng, `since 2020` là cấu trúc B1, và `well` (A2-12) trong A2-11. Đã đổi các câu đó.
5. **Kế hoạch tương lai có hai cách đúng** (`be going to` và hiện tại tiếp diễn). Ở 7 bài, đã chấp nhận cả hai dạng thay vì chấm sai học viên. Không chấp nhận `will` cho các câu về lịch hẹn.

Chi tiết từng thay đổi:

```text
A2-01-E05: viết lại cặp ghép (cặp cũ có thể ghép chéo mà vẫn đúng)
A2-05-E05: viết lại cặp ghép (cặp cũ có thể ghép chéo mà vẫn đúng)
A2-08-E05: viết lại cặp ghép (cặp cũ có thể ghép chéo mà vẫn đúng)
A2-11-E05: viết lại cặp ghép (cặp cũ có thể ghép chéo mà vẫn đúng)
A2-17-E05: viết lại cặp ghép (cặp cũ có thể ghép chéo mà vẫn đúng)
A2-05-E03: option "read / was arriving" → "was reading / arrives"
A2-05-E03: option "was reading / was arriving" → "was read / arrived"
A2-14-E03: option "went" → "have gone"
A2-20-E01: option "went / gone" → "go / been"
A2-20-E02: option "will" → "rains"
A2-20-E03: option "shouldn't / must" → "must / don't have to"
A2-20-E03: option "must / shouldn't" → "must to / don't have"
A2-02-E06: "in front of the school." → "in front of the school five minutes ago."
A2-02-E07: "their hands before lunch." → "their hands before lunch yesterday."
A2-03-E06: "his keys on the table." → "his keys on the table yesterday."
A2-03-E07: "cake for my birthday." → "cake for my birthday last week."
A2-03-E07: "teacher at the bookstore." → "teacher at the bookstore yesterday."
A2-06-E07: "we're staying with friends" → "we are going to stay with friends"
A2-10-E06: "film I have ever seen." → "film of the year."
A2-11-E07: "Lan speaks English as [blank-2] as a native speaker. (well)" → "Lan's English is as [blank-2] as her teacher's. (good)"
A2-14-E07: "They haven't [blank-4] their new teacher yet." → "They have never [blank-4] Lan's parents."
A2-16-E07: "They [blank-2] in this house since 2020. (live)" → "They [blank-2] Hue twice. (visit)"
A2-06-E05: "I've saved some money." → "I have some money now." (hiện tại hoàn thành là step sau)
A2-06-E06 blank-1: chấp nhận thêm am buying, 'm buying
A2-06-E06 blank-2: chấp nhận thêm isn't playing, is not playing
A2-06-E06 blank-3: chấp nhận thêm are moving, 're moving
A2-06-E06 blank-4: chấp nhận thêm is taking, 's taking
A2-06-E07 blank-1: chấp nhận thêm is falling, 's falling
A2-06-E07 blank-2: chấp nhận thêm aren't staying, are not staying
A2-06-E07 blank-4: chấp nhận thêm are joining, 're joining
A2-08-E06 blank-1: chấp nhận thêm am going to have, 'm going to have
A2-08-E06 blank-2: chấp nhận thêm are going to fly, 're going to fly
A2-08-E07 blank-1: chấp nhận thêm is going to play, 's going to play
A2-08-E07 blank-2: chấp nhận thêm is going to get, 's going to get
A2-08-E07 blank-4: chấp nhận thêm aren't going to meet, are not going to meet
A2-20-E06 blank-3: chấp nhận thêm is going to see, 's going to see
A2-07-E06 blank-4: chấp nhận thêm are going to be, 're going to be
A2-07-E07 blank-2: chấp nhận thêm is going to rain
A2-13-E04: context "đúng hay sai ngữ pháp" → "dùng … đúng hay sai" (s-4 sai về nghĩa)
A2-10-E09: thêm trật tự hoán đổi hai token "the"
A2-11-E08: thêm trật tự hoán đổi hai token "as"
A2-11-E09: thêm trật tự hoán đổi hai token "as"
A2-12-E09: chấp nhận thêm "The teacher slowly spoke to the students."
A2-18-E08: xáo token (đang đúng thứ tự đáp án)
A2-18-E09: xáo token (đang đúng thứ tự đáp án)
MC: xáo option 60 bài, vị trí đáp án đúng rải đều
MC pos { '0': 15, '1': 15, '2': 15, '3': 15 }
```

## Danh sách 200 bài (sau khi sửa)

Ký hiệu:
- `*` đánh dấu đáp án đúng của MC.
- `[T]`/`[F]` là đáp án của TrueFalse.
- `⇒` dẫn tới đáp án hoặc cặp ghép đúng.
- `[a|b]` là các đáp án được chấp nhận cho một chỗ trống.


##### A2-01 — Past of be: was/were | Quá khứ của to be: was/were
E01 MC: They ____ at home yesterday evening. | be / *were / are / was
E02 MC: She ____ at school yesterday because she was ill. | *wasn't / isn't / weren't / not was
E03 MC: ____ you at work two days ago? – No, I ____. | Were / weren't / Was / wasn't / Was / weren't / *Were / wasn't
E04 TF: [T] Nam was tired yesterday.  [F] They was in Hanoi last month.  [T] I was born in 2000.  [T] Where were you last night?  [F] She wasn't at school ago two days.
E05 MA: Were you at home last night? ⇒ Yes, I was. ‖ Was Nam at school yesterday? ⇒ No, he wasn't. ‖ Were your parents in Hue last week? ⇒ Yes, they were. ‖ Was the film good? ⇒ No, it wasn't. It was boring.
E06 FB: 1. Lan [was] at the library yesterday morning. (be)  2. We [were] very happy with the party last night. (be)  3. I [wasn't|was not] at school yesterday because of illness. (not be)  4. [Were|were] you at home last Sunday? (be)
E07 FB: 1. My parents [were] in Hue two days ago. (be)  2. The test [wasn't|was not] difficult, so all students passed. (not be)  3. Where [was] Minh yesterday afternoon? (be)  4. When I [was] ten years old, I was very shy. (be)
E08 SU: {tired / I / was / yesterday.} ⇒ I was tired yesterday.
E09 SU: {were / Where / you / night? / last} ⇒ Where were you last night?
E10 EC: They was at the cinema yesterday evening. ⇒ They were at the cinema yesterday evening.

##### A2-02 — Past simple — regular verbs | Quá khứ đơn — động từ có quy tắc
E01 MC: Yesterday, Nam ____ his room after school. | cleaning / clean / *cleaned / cleans
E02 MC: Choose the correct past form of the verb 'study': | studying / *studied / studies / studyed
E03 MC: Which word has the '-ed' ending pronounced as /ɪd/? | *needed / played / worked / watched
E04 TF: [T] She watched a good film last night.  [F] He stoped his car near the park.  [T] They visited their grandparents last Sunday.  [F] Linh studyed English yesterday.  [T] We lived in Da Nang three years ago.
E05 MA: work ⇒ worked ‖ live ⇒ lived ‖ study ⇒ studied ‖ plan ⇒ planned
E06 FB: 1. I [played] football with my friends yesterday. (play)  2. Mai [studied] for the English test last night. (study)  3. The bus [stopped] in front of the school five minutes ago. (stop)  4. They [lived] in Hanoi five years ago. (live)
E07 FB: 1. He [helped] his mother with the cooking yesterday. (help)  2. We [decided] to visit Hue last summer. (decide)  3. The children [washed] their hands before lunch yesterday. (wash)  4. Lan [visited] her grandparents last weekend. (visit)
E08 SU: {watched / TV / We / night. / last} ⇒ We watched TV last night.
E09 SU: {hard / exam / for / Minh / studied / the / yesterday.} ⇒ Minh studied hard for the exam yesterday.
E10 EC: She watch TV with her family yesterday evening. ⇒ She watched TV with her family yesterday evening.

##### A2-03 — Past simple — irregular verbs | Quá khứ đơn — động từ bất quy tắc
E01 MC: Yesterday, Nam ____ to the supermarket with his sister. | goes / going / goed / *went
E02 MC: She ____ a new dress in the city center last weekend. | buys / buying / *bought / buyed
E03 MC: We ____ dinner at a Vietnamese restaurant and ____ tea. | ate / drinked / *ate / drank / eated / drank / eated / drinked
E04 TF: [T] I saw my old teacher yesterday.  [F] He teached me English two years ago.  [T] They had a great holiday in Da Nang.  [F] She writed a letter to her friend.  [T] We met at the coffee shop last night.
E05 MA: buy ⇒ bought ‖ see ⇒ saw ‖ take ⇒ took ‖ swim ⇒ swam
E06 FB: 1. We [went] to Da Lat on holiday last month. (go)  2. Mai [ate] a bowl of pho this morning. (eat)  3. He [left] his keys on the table yesterday. (leave)  4. I [got] an interesting email from Tom yesterday. (get)
E07 FB: 1. Lan [made] a delicious cake for my birthday last week. (make)  2. They [met] their English teacher at the bookstore yesterday. (meet)  3. The teacher [told] us an interesting story yesterday. (tell)  4. My father [bought] a blue car three years ago. (buy)
E08 SU: {went / to / zoo / the / They / yesterday.} ⇒ They went to the zoo yesterday.
E09 SU: {bought / Linh / a / new / phone / last / week.} ⇒ Linh bought a new phone last week.
E10 EC: He gived his sister a lovely birthday gift yesterday. ⇒ He gave his sister a lovely birthday gift yesterday.

##### A2-04 — Past simple — negatives and questions | Quá khứ đơn — phủ định và câu hỏi
E01 MC: I ____ to school yesterday because it was Sunday. | *didn't go / don't went / not went / didn't went
E02 MC: ____ you finish your English homework last night? | Do / Were / Have / *Did
E03 MC: Where ____ last weekend? – We stayed at home. | were you go / you went / *did you go / did you went
E04 TF: [T] She didn't see him yesterday.  [F] Did you saw the new film?  [T] What did you buy at the market?  [F] He didn't happy last night.  [T] They didn't eat breakfast this morning.
E05 MA: Did you call him? ⇒ Yes, I did, but he was busy. ‖ Where did she go? ⇒ She went to the market. ‖ Why were you late? ⇒ Because I missed the bus. ‖ Did they enjoy the party? ⇒ Yes, they did. They had fun.
E06 FB: 1. I [didn't buy|did not buy] any milk yesterday. (not buy)  2. [Did|did] you watch TV last night? (do)  3. She [didn't come|did not come] to my birthday party on Saturday. (not come)  4. What time [did] they arrive yesterday? (do)
E07 FB: 1. We [didn't visit|did not visit] the museum yesterday because it was closed. (not visit)  2. Why [did] you leave the meeting early yesterday? (do)  3. Nam [didn't find|did not find] his keys, so he could not open the door. (not find)  4. [Did|did] she like the food in Hue? (do)
E08 SU: {see / I / didn't / yesterday. / Nam} ⇒ I didn't see Nam yesterday.
E09 SU: {did / Where / go / you / summer? / last} ⇒ Where did you go last summer?
E10 EC: He didn't went to work yesterday because he was sick. ⇒ He didn't go to work yesterday because he was sick. ‖ He did not go to work yesterday because he was sick.

##### A2-05 — Past continuous with when/while | Quá khứ tiếp diễn và when/while
E01 MC: At 8 p.m. yesterday, I ____ dinner with my family. | had / *was having / were having / am having
E02 MC: They ____ football when it started to rain. | *were playing / are playing / played / was playing
E03 MC: While she ____ a book, her brother ____. | was read / arrived / was reading / arrives / is reading / arrived / *was reading / arrived
E04 TF: [T] I was sleeping when the phone rang.  [F] They was walking in the park at 4 p.m.  [T] While he was cooking, he burnt his finger.  [T] What were you doing at 9 last night?  [F] She was listen to music when I arrived.
E05 MA: What were you doing at 8 p.m.? ⇒ I was watching TV. ‖ What was Lan doing when you called? ⇒ She was cooking dinner. ‖ What were the boys doing in the park? ⇒ They were playing football. ‖ Was it raining when you left? ⇒ Yes, it was raining heavily.
E06 FB: 1. At 10 a.m. yesterday, Lan [was working] in the garden. (work)  2. We [were watching] television when the power went off. (watch)  3. What [were] you doing at this time last night? (be)  4. He [was sleeping] when the alarm clock rang. (sleep)
E07 FB: 1. While I was doing homework, my brother [was playing] guitar. (play)  2. She was crossing the street when she [saw] the accident. (see)  3. They [were having] dinner when their guests arrived. (have)  4. While we [were waiting] for the bus, it began to rain. (wait)
E08 SU: {sleeping / was / He / called. / when / I} ⇒ He was sleeping when I called.
E09 SU: {was / rang. / cooking, / While / she / phone / the} ⇒ While she was cooking, the phone rang.
E10 EC: I was walk home when it started to rain yesterday. ⇒ I was walking home when it started to rain yesterday.

##### A2-06 — Future with be going to | Tương lai với be going to
E01 MC: Look at the dark clouds! It ____ rain. | going to / are going to / *is going to / goes to
E02 MC: Mai and Lan ____ travel to Da Nang next month. | am going to / *are going to / is going to / going to
E03 MC: ____ you ____ buy that new laptop this weekend? | *Are / going to / Is / going to / Do / going to / Are / go to
E04 TF: [T] I'm going to learn Japanese next year.  [F] She is going to visits her grandparents.  [T] Be careful! You are going to fall.  [F] They going to watch a film tonight.  [T] We aren't going to buy that old car.
E05 MA: Look at the dark clouds! ⇒ It's going to rain soon. ‖ I have some money now. ⇒ I'm going to buy a bicycle. ‖ We're very hungry. ⇒ We're going to make dinner. ‖ He has an exam tomorrow. ⇒ He's going to study all night.
E06 FB: 1. I [am going to buy|'m going to buy|am buying|'m buying] a new bicycle next week. (buy)  2. She [isn't going to play|is not going to play|isn't playing|is not playing] tennis this afternoon because she is tired. (not play)  3. They [are going to move|'re going to move|are moving|'re moving] to a new apartment next month. (move)  4. Nam [is going to take|'s going to take|is taking|'s taking] his English exam tomorrow. (take)
E07 FB: 1. Look out! That glass [is going to fall|'s going to fall|is falling|'s falling] off the table. (fall)  2. We [aren't going to stay|are not going to stay|aren't staying|are not staying] at a hotel; we are going to stay with friends. (not stay)  3. What [are] you going to do tonight? (be)  4. Minh and his brother [are going to join|'re going to join|are joining|'re joining] the football club. (join)
E08 SU: {going / He / to / is / tomorrow. / study} ⇒ He is going to study tomorrow.
E09 SU: {going / Are / weekend? / you / this / to / cook} ⇒ Are you going to cook this weekend?
E10 EC: She is going to buys a new laptop tomorrow. ⇒ She is going to buy a new laptop tomorrow.

##### A2-07 — Future with will | Tương lai với will
E01 MC: The phone is ringing. – Don't worry, I ____ answer it. | going to / am / do / *'ll
E02 MC: I promise I ____ tell anyone your secret. | don't / not will / *won't / willn't
E03 MC: I think it ____ sunny tomorrow, so we can go out. | will to be / *will be / wills be / is being
E04 TF: [T] I will call you when I arrive.  [F] She wills help us tomorrow.  [T] I'm sure you will pass the exam.  [F] I will to open the window for you.  [T] Don't worry, we won't be late.
E05 MA: It's cold in here. ⇒ I'll close the door. ‖ I don't have any money. ⇒ I'll lend you some. ‖ I'm so thirsty. ⇒ I'll get you a glass of water. ‖ This bag is very heavy. ⇒ I'll carry it for you.
E06 FB: 1. I'm very tired. I think I [will go|'ll go] to bed now. (go)  2. We promise we [won't forget|will not forget] your birthday. (not forget)  3. [Shall|shall] I carry this suitcase for you? (shall)  4. Don't worry about the test; you [will be|'ll be|are going to be|'re going to be] fine. (be)
E07 FB: 1. A: There is no milk left. B: Oh, I [will buy|'ll buy] some from the shop. (buy)  2. I don't think it [will rain|is going to rain] tomorrow. (rain)  3. [Will|will] you help me with this box, please? (will)  4. Goodbye! I [will see|'ll see] you tomorrow morning. (see)
E08 SU: {call / I / tonight. / you / will} ⇒ I will call you tonight.
E09 SU: {think / win / team / will / the / match. / our / I} ⇒ I think our team will win the match.
E10 EC: Don't worry, I will to help you with your bags. ⇒ Don't worry, I will help you with your bags. ‖ Don't worry, I'll help you with your bags.

##### A2-08 — Present continuous for future arrangements | Hiện tại tiếp diễn cho kế hoạch tương lai
E01 MC: I ____ the doctor tomorrow at 10 a.m. (I booked an appointment). | *am seeing / will to see / see / saw
E02 MC: The train to Hue ____ at 6 a.m. every day. | is leaving / leave / leaving / *leaves
E03 MC: What ____ this Saturday evening? – We are having a party. | will you to do / did you / *are you doing / do you do
E04 TF: [T] We are flying to Bangkok on Friday.  [F] The film is starting at 8 every night.  [T] She is meeting her teacher tomorrow morning.  [T] What time does the lesson start tomorrow?  [F] I am play tennis with Nam this afternoon.
E05 MA: What are you doing tonight? ⇒ I'm meeting my friends at 7. ‖ When does the train leave? ⇒ It leaves at 6 a.m. tomorrow. ‖ Is Mai coming to the party? ⇒ No, she's visiting her grandma. ‖ What time do the shops open tomorrow? ⇒ They open at 8:30 a.m.
E06 FB: 1. I [am having|'m having|am going to have|'m going to have] dinner with Linh tonight at 7. (have)  2. We [are flying|'re flying|are going to fly|'re going to fly] to Da Nang this Friday; we already bought tickets. (fly)  3. The bus [leaves] at 8:00 every morning. (leave)  4. What time [does] the concert begin tonight? (do)
E07 FB: 1. Nam [is playing|'s playing|is going to play|'s going to play] football with his colleagues this Sunday. (play)  2. My sister [is getting|'s getting|is going to get|'s going to get] married next month. (get)  3. Our English class [starts] at 6:30 p.m. on Mondays. (start)  4. They [aren't meeting|are not meeting|aren't going to meet|are not going to meet] anyone this weekend because they want to rest. (not meet)
E08 SU: {meeting / am / I / Lan / tonight. / at / seven} ⇒ I am meeting Lan at seven tonight.
E09 SU: {leaves / at / morning. / six / tomorrow / train / The} ⇒ The train leaves at six tomorrow morning.
E10 EC: We are fly to Da Nang tomorrow morning. ⇒ We are flying to Da Nang tomorrow morning. ‖ We're flying to Da Nang tomorrow morning.

##### A2-09 — Comparative adjectives | So sánh hơn
E01 MC: Hanoi is ____ than Hue. | biger / *bigger / more big / biggest
E02 MC: This new laptop is ____ than my old one. | *more expensive / most expensive / expensiver / as expensive
E03 MC: Today the weather is much ____ than yesterday. | more better / best / gooder / *better
E04 TF: [T] My brother is taller than me.  [F] This phone is more cheaper than that one.  [T] Traveling by plane is faster than by train.  [F] Her English is gooder than mine.  [T] A motorbike is noisier than a bicycle.
E05 MA: cheap ⇒ cheaper than ‖ easy ⇒ easier than ‖ bad ⇒ worse than ‖ modern ⇒ more modern than
E06 FB: 1. A car is [faster] than a bicycle. (fast)  2. My old room was [smaller] than this new room. (small)  3. English is [easier] for me than Russian. (easy)  4. This coffee is [better] than the coffee we had yesterday. (good)
E07 FB: 1. That red dress is [more expensive] than this black one. (expensive)  2. Living in the city is [noisier] than in the countryside. (noisy)  3. His test result was [worse] than mine. (bad)  4. Today is a lot [hotter] than yesterday. (hot)
E08 SU: {taller / than / is / Nam / me.} ⇒ Nam is taller than me.
E09 SU: {than / is / plane / train. / faster / A / a} ⇒ A plane is faster than a train.
E10 EC: This smartphone is more cheaper than that one. ⇒ This smartphone is cheaper than that one.

##### A2-10 — Superlative adjectives | So sánh nhất
E01 MC: Mount Everest is ____ mountain in the world. | the higher / highest / *the highest / most high
E02 MC: Lan is ____ student in our class. | most intelligent / *the most intelligent / more intelligent / the intelligentest
E03 MC: This is ____ restaurant in Hanoi; everyone loves it. | *the best / best / the goodest / the better
E04 TF: [T] He is the tallest boy in the team.  [F] She is tallest girl in my family.  [T] This is the most dangerous road in the area.  [F] That was the worse day of my life.  [T] Russia is the largest country in the world.
E05 MA: good ⇒ the best ‖ happy ⇒ the happiest ‖ expensive ⇒ the most expensive ‖ far ⇒ the farthest
E06 FB: 1. The Nile is [the longest] river in the world. (long)  2. That was [the most boring] film of the year. (boring)  3. Nam is [the best] player in the football club. (good)  4. February is [the shortest] month of the year. (short)
E07 FB: 1. She is [the busiest] person in our office. (busy)  2. Yesterday was [the hottest] day of the week. (hot)  3. This hotel is [the most expensive] one in the city. (expensive)  4. That was [the worst] mistake of my life. (bad)
E08 SU: {tallest / in / is / class. / our / the / He} ⇒ He is the tallest in our class.
E09 SU: {most / the / is / This / city. / in / the / beautiful / park} ⇒ This is the most beautiful park in the city. ‖ This is the most beautiful park in the city.
E10 EC: He is tallest student in our school. ⇒ He is the tallest student in our school.

##### A2-11 — as … as and not as … as | So sánh bằng as … as
E01 MC: My phone is not as ____ as your phone. | more expensive / expensiver / most expensive / *expensive
E02 MC: Lan is as ____ as her sister. | more tall / taller / *tall / tallest
E03 MC: His new bag is the same ____ mine. | like / *as / with / than
E04 TF: [T] She is as tall as her mother.  [F] This exercise isn't as easier as that one.  [T] His car is different from my car.  [T] My room is the same size as your room.  [F] He runs as fast than his brother.
E05 MA: A bicycle isn't as fast ⇒ as a motorbike. ‖ Her score is the same ⇒ as his score. ‖ This old book isn't as interesting ⇒ as the new film. ‖ My opinion is different ⇒ from yours.
E06 FB: 1. Today is not as [hot] as yesterday. (hot)  2. Dogs are as [clever] as cats. (clever)  3. His new house is the [same] as my house. (same)  4. Life in the village is different [from] life in the city. (from)
E07 FB: 1. This coffee isn't as [good] as the one we drank yesterday. (good)  2. Lan's English is as [good] as her teacher's. (good)  3. The second exam was not [as|so] difficult as the first one. (as)  4. Are you as [old] as your brother? (old)
E08 SU: {is / as / Nam / tall / Minh. / as} ⇒ Nam is as tall as Minh. ‖ Nam is as tall as Minh.
E09 SU: {as / car. / isn't / fast / A / bicycle / as / a} ⇒ A bicycle isn't as fast as a car. ‖ A bicycle isn't as fast as a car.
E10 EC: She is not as taller as her sister. ⇒ She is not as tall as her sister. ‖ She isn't as tall as her sister.

##### A2-12 — Adverbs of manner | Trạng từ cách thức
E01 MC: She sings very ____; everyone enjoys her songs. | *beautifully / more beautiful / beautiful / beauty
E02 MC: He plays football very ____. | better / goodly / good / *well
E03 MC: You look ____ today. Did you sleep well? | tire / tiringly / *tired / tiredly
E04 TF: [T] Please speak slowly and clearly.  [F] She speaks English very good.  [T] He drives carefully in the rain.  [F] Nam works very hardly every day.  [T] The children were quiet during the lesson.
E05 MA: He is a fast runner. ⇒ He runs fast. ‖ She is a careful driver. ⇒ She drives carefully. ‖ They are hard workers. ⇒ They work hard. ‖ He is a good singer. ⇒ He sings well.
E06 FB: 1. The baby is sleeping, so please close the door [quietly]. (quiet)  2. She passed the exam [easily] because she studied hard. (easy)  3. He drives very [slowly] when it rains. (slow)  4. My brother plays badminton very [well]. (good)
E07 FB: 1. Nam studied [hard] and got the highest score. (hard)  2. You look [sad]; what happened? (sad)  3. She answered all the teacher's questions [correctly]. (correct)  4. Don't run so [fast]; the floor is wet. (fast)
E08 SU: {drives / carefully. / very / father / My} ⇒ My father drives very carefully.
E09 SU: {spoke / teacher / The / slowly / the / to / students.} ⇒ The teacher spoke slowly to the students. ‖ The teacher slowly spoke to the students.
E10 EC: She speaks English very good with foreign visitors. ⇒ She speaks English very well with foreign visitors.

##### A2-13 — must, have to, should | must, have to, should
E01 MC: You ____ smoke in the hospital. It is strictly forbidden. | must / *mustn't / should / don't have to
E02 MC: Tomorrow is Sunday, so I ____ get up early. | *don't have to / shouldn't to / must not to / mustn't
E03 MC: You look exhausted. You ____ see a doctor. | have / should to / must to / *should
E04 TF: [T] You must wear a helmet when riding a motorbike.  [F] He must to finish his homework now.  [T] She has to wear a uniform at school.  [F] Museum entry is free, so you mustn't pay.  [T] You shouldn't drink too much coffee at night.
E05 MA: You mustn't park here. ⇒ It's not allowed; look at the sign. ‖ You don't have to wash the dishes. ⇒ I already washed them. ‖ You should rest. ⇒ You have a bad headache. ‖ Yesterday I had to ⇒ work late because of the meeting.
E06 FB: 1. Students [mustn't|must not] use mobile phones during the exam. (must not)  2. It's a holiday today, so we [don't have to|do not have to] go to work. (not have to)  3. You [should] eat more vegetables; they are good for you. (should)  4. Nam [has to] get up at 6 a.m. every weekday. (have to)
E07 FB: 1. You [mustn't|must not] touch that hot pan; you will burn your hand. (must not)  2. We [had to] wait for the bus for 40 minutes yesterday. (have to - past)  3. [Does|does] she have to wear glasses for reading? (do)  4. Children [should] go to bed early on school nights. (should)
E08 SU: {wear / must / You / helmet. / a} ⇒ You must wear a helmet.
E09 SU: {have / early / up / don't / get / to / We / tomorrow.} ⇒ We don't have to get up early tomorrow.
E10 EC: The museum is free today, so you mustn't buy a ticket. ⇒ The museum is free today, so you don't have to buy a ticket. ‖ The museum is free today, so you do not have to buy a ticket.

##### A2-14 — Present perfect for experience | Hiện tại hoàn thành: kinh nghiệm (ever/never)
E01 MC: ____ you ever been to Singapore? | Do / Did / *Have / Were
E02 MC: Lan has ____ eaten sushi in her life. | already / *never / yet / ever
E03 MC: Where is Nam? – He ____ to the market. He will be back soon. | *has gone / has been / is gone / have gone
E04 TF: [F] I have ever visited Japan.  [T] She has never seen snow.  [F] Have you ever ate durian?  [T] They have been to Da Nang twice.  [F] He has went to the supermarket.
E05 MA: Have you ever ⇒ flown in a helicopter? ‖ I've never ⇒ tried Japanese food before. ‖ She has been ⇒ to London and came back yesterday. ‖ He has gone ⇒ to Hanoi and is still there.
E06 FB: 1. I have [been] to Phu Quoc island three times. (be)  2. [Have|have] you ever seen a tiger in real life? (have)  3. Minh has never [ridden] an elephant. (ride)  4. We [have|'ve] never met his parents. (have)
E07 FB: 1. Lan isn't at home; she has [gone] to the supermarket. (go)  2. She has [never] written a poem before. (never)  3. [Has|has] your brother ever worked in a restaurant? (have)  4. They have never [met] Lan's parents. (meet)
E08 SU: {ever / Have / been / you / Japan? / to} ⇒ Have you ever been to Japan?
E09 SU: {eaten / has / sushi / never / Mai / before.} ⇒ Mai has never eaten sushi before.
E10 EC: Have you ever went to Da Lat on vacation? ⇒ Have you ever been to Da Lat on vacation? ‖ Have you ever gone to Da Lat on vacation?

##### A2-15 — Present perfect with just, already, yet | Hiện tại hoàn thành: just, already, yet
E01 MC: I have ____ finished my lunch, so I'm not hungry now. | never / yet / ever / *just
E02 MC: Have you done your homework ____? – No, not ____. | already / already / yet / already / *yet / yet / just / yet
E03 MC: Don't wash the floor. Nam has ____ cleaned it. | yet / *already / ever / still
E04 TF: [T] I have just received your email.  [F] She hasn't finished her report already.  [T] They have already left for the airport.  [T] Have you called the doctor yet?  [F] He has finished just his dinner.
E05 MA: I've just made some tea. ⇒ Would you like a cup? ‖ The train has already left. ⇒ We missed it by five minutes. ‖ I haven't eaten yet. ⇒ So I'm going to order food now. ‖ Has he arrived yet? ⇒ No, he is still on the bus.
E06 FB: 1. I have [just] heard the good news; congratulations! (just)  2. She has [already] packed her bags for tomorrow's trip. (already)  3. We haven't bought tickets for the concert [yet]. (yet)  4. Have you seen the new teacher [yet]? (yet)
E07 FB: 1. The postman has [just] delivered a letter for you. (just)  2. Mai [has] already cleaned the kitchen. (have)  3. The film hasn't [started] yet; take your seat. (start)  4. I can't find my keys; I think I have [lost] them. (lose)
E08 SU: {just / arrived / have / home. / They} ⇒ They have just arrived home.
E09 SU: {hasn't / work / finished / his / Nam / yet.} ⇒ Nam hasn't finished his work yet.
E10 EC: She has already not done her homework. ⇒ She hasn't done her homework yet. ‖ She has not done her homework yet.

##### A2-16 — Present perfect vs past simple | Hiện tại hoàn thành và quá khứ đơn
E01 MC: I ____ to Da Nang three years ago. | *went / have been / go / have gone
E02 MC: ____ you ever visited Ha Long Bay? | Do / Were / Did / *Have
E03 MC: When ____ your new car? – I bought it last month. | were you buy / have you bought / *did you buy / did you bought
E04 TF: [T] I saw him yesterday afternoon.  [F] I have seen him yesterday afternoon.  [T] When did you arrive in Hanoi?  [T] Have you ever eaten banh mi?  [F] She has lived in Hue in 2015.
E05 MA: Have you seen Nam? ⇒ Yes, I've just talked to him. ‖ When did you see him? ⇒ I saw him ten minutes ago. ‖ I've been to London. ⇒ Really? Did you like it? ‖ When were you there? ⇒ I went there in 2019.
E06 FB: 1. I [went] to Nha Trang last summer. (go)  2. We have already [seen] that new movie. (see)  3. Lan [lost] her keys two days ago. (lose)  4. He has [drunk] three cups of tea this morning. (drink)
E07 FB: 1. When [did] you buy that smartphone? (do)  2. They [have visited|'ve visited] Hue twice. (visit)  3. My father [arrived] home ten minutes ago. (arrive)  4. I haven't [eaten] dinner yet tonight. (eat)
E08 SU: {saw / yesterday. / I / him} ⇒ I saw him yesterday.
E09 SU: {did / you / When / arrive / airport? / the / at} ⇒ When did you arrive at the airport?
E10 EC: I have bought this bicycle two days ago. ⇒ I bought this bicycle two days ago.

##### A2-17 — too and enough | too và enough
E01 MC: This tea is ____ hot to drink right now. | many / *too / very much / enough
E02 MC: He is eighteen, so he is old ____ to drive a car. | *enough / too / more / as
E03 MC: We can't buy that house because we don't have ____. | too money / too many money / money enough / *enough money
E04 TF: [T] The box is too heavy to carry.  [F] She is enough tall to play basketball.  [T] There are too many cars on the street.  [F] We don't have time enough to finish.  [T] This room is too small for five people.
E05 MA: I can't drink this tea. ⇒ It's too hot. ‖ He can't lift the table. ⇒ He isn't strong enough. ‖ Some guests have to stand at the party. ⇒ There aren't enough chairs. ‖ We can't get on the bus. ⇒ There are too many people on it.
E06 FB: 1. The water is [too] cold to swim in today. (too)  2. She didn't run fast [enough] to catch the bus. (enough)  3. Do you have [enough] money to buy lunch? (enough)  4. There is too [much] sugar in this tea. (much)
E07 FB: 1. The coat is too small for him to [wear]. (wear)  2. They aren't old [enough] to watch this scary film. (enough)  3. There were too [many] students in the small classroom. (many)  4. We have [enough] food for the party; don't worry. (enough)
E08 SU: {hot / is / coffee / This / too / drink. / to} ⇒ This coffee is too hot to drink.
E09 SU: {isn't / He / drive. / enough / to / old} ⇒ He isn't old enough to drive.
E10 EC: She is enough old to drive a car. ⇒ She is old enough to drive a car.

##### A2-18 — Conjunctions and time clauses | Liên từ và mệnh đề thời gian
E01 MC: I went to bed early ____ I was very tired. | but / or / *because / so
E02 MC: I'll call you when I ____ at the hotel. | will arrive / *arrive / arrived / am arriving
E03 MC: Please wait here until the teacher ____ back. | *comes / will come / came / is coming
E04 TF: [T] I'll help you after I finish my work.  [F] Because he was sick, so he stayed at home.  [T] Wash your hands before you eat.  [F] She will call us when she will arrive.  [T] The weather was bad, so we cancelled the picnic.
E05 MA: I like tea, ⇒ but my sister prefers coffee. ‖ He was hungry, ⇒ so he bought a sandwich. ‖ We stayed home ⇒ because it rained all day. ‖ You can have tea ⇒ or coffee, whichever you prefer.
E06 FB: 1. I was tired, [so] I went to sleep early. (so)  2. We couldn't go out [because] it was raining heavily. (because)  3. I'll send you a message when I [get] home. (get)  4. Always turn off the lights before you [leave] the room. (leave)
E07 FB: 1. Please wait in the room until the doctor [calls] you. (call)  2. She will be very happy when she [sees] this gift. (see)  3. We had breakfast before we [went] to work yesterday. (go)  4. He worked hard [because] he wanted to buy a house. (because)
E08 SU: {went / tired, / home. / was / Nam / so / he} ⇒ Nam was tired, so he went home.
E09 SU: {when / He / he / you / will / arrives. / call} ⇒ He will call you when he arrives.
E10 EC: Because I was sick, so I stayed at home yesterday. ⇒ Because I was sick, I stayed at home yesterday. ‖ I was sick, so I stayed at home yesterday.

##### A2-19 — Verb patterns and a few/a little | Động từ + to V / V-ing và lượng từ a few/a little
E01 MC: She decided ____ a new laptop for her studies. | buying / buy / bought / *to buy
E02 MC: Nam really enjoys ____ to music in the evening. | listened / listen / *listening / to listen
E03 MC: Could you put a ____ sugar in my coffee, please? | many / *little / lot / few
E04 TF: [T] I want to learn English well.  [F] She finished to do her homework at 9 p.m.  [T] He is interested in playing chess.  [T] I only have a few friends in this city.  [F] There is a few milk in the fridge.
E05 MA: She decided ⇒ to study abroad next year. ‖ They finished ⇒ cleaning the classroom. ‖ I have a few ⇒ books on English grammar. ‖ Can I have a little ⇒ milk in my tea, please?
E06 FB: 1. I want [to travel] to Hanoi this weekend. (travel)  2. Lan enjoys [reading] books before going to bed. (read)  3. Would you mind [opening] the window? (open)  4. He hopes [to pass] his English exam. (pass)
E07 FB: 1. She is very good at [baking] delicious cakes. (bake)  2. We need a [few] minutes to prepare for the test. (few)  3. Add a [little] salt to the soup, please. (little)  4. I don't have [much] time today because of meetings. (much)
E08 SU: {to / learn / wants / She / English.} ⇒ She wants to learn English.
E09 SU: {cooking / enjoys / dinner / My / father / at / weekends.} ⇒ My father enjoys cooking dinner at weekends.
E10 EC: She enjoys to cook traditional Vietnamese food. ⇒ She enjoys cooking traditional Vietnamese food.

##### A2-20 — A2 review and level test | Ôn tập A2 và kiểm tra level
E01 MC: I ____ to Da Lat in 2021, but I have never ____ to Sapa. | *went / been / go / been / have gone / been / have been / went
E02 MC: Look at those dark clouds! It ____ rain. | shall / rains / is / *is going to
E03 MC: You ____ enter this room; it is private. But you ____ pay to wait outside. | don't have to / mustn't / must to / don't have / *mustn't / don't have to / must / don't have to
E04 TF: [T] She was cooking when the phone rang.  [F] This hotel is more cheaper than the first one.  [T] Have you seen the new teacher yet?  [F] Because it was raining, so we stayed home.  [T] He is old enough to drive a car.
E05 MA: While I was studying, ⇒ my brother was playing games. ‖ This phone is not as ⇒ expensive as that one. ‖ I'll call you as soon as ⇒ I arrive at the airport. ‖ She wants to learn ⇒ how to play the guitar.
E06 FB: 1. While they were walking in the park, it [started] to rain. (start)  2. We have already [booked] our flight tickets to Da Nang. (book)  3. Lan [is seeing|'s seeing|is going to see|'s going to see] her doctor tomorrow morning at 9. (see)  4. He [lost] his keys yesterday afternoon. (lose)
E07 FB: 1. Mount Everest is [the highest] mountain in the world. (high)  2. The soup is too salty for us to [eat]. (eat)  3. We will start the meeting when Mr. Nam [arrives]. (arrive)  4. Could you please give me a [little] sugar? (little)
E08 SU: {walking / was / home / started. / when / rain / the / She} ⇒ She was walking home when the rain started.
E09 SU: {will / call / you / when / arrives. / he / Nam} ⇒ Nam will call you when he arrives.
E10 EC: I will call you as soon as I will arrive in Hanoi. ⇒ I will call you as soon as I arrive in Hanoi. ‖ I'll call you as soon as I arrive in Hanoi.

## Phát hiện về A1 (đang chạy trên production, ngoài phạm vi task này)

- 56/60 bài MC của A1 đặt đáp án đúng ở option đầu. BE và FE đều không xáo option, nên học viên chỉ cần chọn option đầu là đúng gần hết.
- A1-16-E08 và A1-16-E09 có hai token `the` nhưng chỉ khai báo một trật tự. Học viên đặt hai chữ `the` đổi chỗ nhau sẽ bị chấm sai.
- Đề xuất: làm task riêng để xáo option khi trả bài cho learner (sửa trong sanitizer, áp dụng cho cả Lesson) hoặc xáo ngay trong dữ liệu A1. Phải kiểm cách importer xử lý bài đã có khi `ContentHash` thay đổi.

## Vòng 2 (sau code review, 2026-11-02)

Các thay đổi:

- Sửa 28 hint làm lộ đúng từ cần chọn (just/already/yet, too/enough, so/because, few/little, must not, should…). Hint mới ghi các lựa chọn để học viên chọn.
- Tăng `maxLength` ở 5 chỗ trống quá ngắn so với đáp án.

Giữ nguyên các hint là tính từ hoặc động từ gốc, ví dụ "as (hot) as" hay "(hard)", vì bài kiểm việc giữ nguyên dạng từ chứ không đổi sang so sánh hơn hoặc trạng từ.

```text
A2-07-E06 blank-3: hint (shall) → (shall / will)
A2-07-E07 blank-3: hint (will) → (will / shall)
A2-11-E06 blank-3: hint (same) → (same / similar)
A2-11-E06 blank-4: hint (from) → (from / than)
A2-11-E07 blank-3: hint (as) → (as / than)
A2-13-E06 blank-1: hint (must not) → (must not / don't have to)
A2-13-E06 blank-3: hint (should) → (should / must)
A2-13-E07 blank-1: hint (must not) → (must not / don't have to)
A2-13-E07 blank-4: hint (should) → (should / must)
A2-14-E06 blank-2: hint (have) → (have / did)
A2-14-E06 blank-4: hint (have) → (have / has)
A2-14-E07 blank-2: hint (never) → (never / ever)
A2-15-E06 blank-1: hint (just) → (just / already / yet)
A2-15-E06 blank-2: hint (already) → (just / already / yet)
A2-15-E06 blank-3: hint (yet) → (just / already / yet)
A2-15-E06 blank-4: hint (yet) → (just / already / yet)
A2-15-E07 blank-1: hint (just) → (just / already / yet)
A2-17-E06 blank-1: hint (too) → (too / enough)
A2-17-E06 blank-2: hint (enough) → (too / enough)
A2-17-E06 blank-3: hint (enough) → (too / enough)
A2-17-E06 blank-4: hint (much) → (much / many)
A2-17-E07 blank-2: hint (enough) → (too / enough)
A2-17-E07 blank-3: hint (many) → (much / many)
A2-17-E07 blank-4: hint (enough) → (too / enough)
A2-18-E06 blank-1: hint (so) → (so / because)
A2-18-E06 blank-2: hint (because) → (so / because)
A2-18-E07 blank-4: hint (because) → (so / because)
A2-19-E07 blank-2: hint (few) → (few / little)
A2-19-E07 blank-3: hint (little) → (few / little)
A2-19-E07 blank-4: hint (much) → (much / many)
A2-20-E07 blank-4: hint (little) → (few / little)
A2-07-E07 blank-2: maxLength 15 → 16
A2-08-E06 blank-1: maxLength 15 → 16
A2-08-E06 blank-2: maxLength 15 → 16
A2-08-E07 blank-1: maxLength 15 → 16
A2-08-E07 blank-4: maxLength 20 → 21
```
