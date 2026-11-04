# Review bộ bài core B1 (đợt 2)

- **Nguồn:** Codex soạn `b1-exercises.json` gồm 180 bài (18 step × 10). AGY không chạy được vì hết credit, nên đổi sang Codex.
- **Cách review:** Claude tự giải từng bài, sửa các bài sai, rồi kiểm lại bằng script và importer thật.
- **Duyệt:** người dùng ủy quyền cho Claude tự duyệt (2026-11-02).

## Kết quả kiểm

| Kiểm tra | Kết quả |
| --- | --- |
| 18 step × 10 bài; key không trùng; phân bố type đúng bảng | Đạt |
| Vị trí đáp án đúng trong 54 câu MC | 14/14/13/13, đã rải đều ngay từ bản Codex soạn |
| Câu MC có hai đáp án đúng | 0 |
| SentenceUnscramble | Không bài nào có token đã nằm sẵn đúng thứ tự đáp án. Bài có token trùng chữ đã liệt kê đủ các hoán vị. Token khác nhau chỉ ở chữ hoa (The/the) được giữ là hai token riêng. |
| Importer thật (`GrammarBundledContentPackTests`) | 580 bài A1+A2+B1, không có lỗi. Import lần 2 không tạo thêm bài. |
| Số bài phải sửa | 23 lần sửa trên 22 bài, xem bên dưới |

## Đã sửa

Các nhóm lỗi:

1. **Matching ghép chéo vẫn ra câu đúng** (7 bài).
   - B1-07 và B1-08 (bị động): chủ ngữ nào cũng ghép được với vị ngữ nào. Đã đổi thành cặp câu chủ động → câu bị động, mỗi cặp chỉ khớp một cách duy nhất.
   - Các bài còn lại: đã viết lại cặp hỏi–đáp, hoặc dùng đại từ và nghĩa để mỗi mục chỉ khớp với một mục.
2. **FillInTheBlanks thiếu biến thể đúng** (12 chỗ trống).
   - Thiếu `have waited` cho câu có `for`, `has been teaching` cho câu có `since`.
   - Thiếu `was` cho `If I were`, thiếu `that` cho `the day when`.
   - Thiếu cặp `if`/`whether`, thiếu `may`/`could` khi đáp án là `might`.
   - Thiếu các dạng đuôi khác của câu mệnh lệnh.
3. **Lỗi cơ học** (2 bài): chữ "hurry" bị lặp sau chỗ trống ở B1-05-E07; hint sai ở B1-13-E06.
4. **SentenceUnscramble:** thêm một trật tự hợp lệ cho B1-04-E08.

Chi tiết:

```text
B1-01-E05: viết lại cặp ghép ("What have you been doing all morning?" ghép được với cả hai câu trả lời V-ing)
B1-04-E05: viết lại cặp ghép (cụm thời gian ghép với bất kỳ hành động nào cũng đúng)
B1-05-E05: viết lại cặp ghép ("If you don't hurry" ghép được với "we will be late")
B1-06-E05: viết lại cặp ghép ("If I knew her number" và "If I were you" ghép chéo được)
B1-07-E05: viết lại cặp ghép (chủ ngữ + vị ngữ bị động ghép chéo được; đổi thành cặp chủ động → bị động)
B1-08-E05: viết lại cặp ghép (chủ ngữ + vị ngữ bị động ghép chéo được; đổi thành cặp chủ động → bị động)
B1-09-E05: viết lại cặp ghép ("The woman who called you" ghép được với "reported it to the police")
B1-05-E07 blank-3: bỏ chữ "hurry" thừa sau chỗ trống (đáp án "doesn't hurry" tạo thành "doesn't hurry hurry")
B1-13-E06 blank-4: sửa hint "(be)" thành "(can/not)" (chỗ trống chỉ điền modal)
B1-01-E06 blank-1: chấp nhận thêm "have waited", "'ve waited" (với for, hiện tại hoàn thành đơn cũng đúng)
B1-01-E07 blank-1: chấp nhận thêm "has been teaching", "'s been teaching" (teach với since dùng được cả hai dạng)
B1-01-E07 blank-2: chấp nhận thêm "have stood", "'ve stood" (với for, hiện tại hoàn thành đơn cũng đúng)
B1-06-E06 blank-3: chấp nhận thêm "was" ("If I was you" chấp nhận trong văn nói)
B1-06-E07 blank-3: chấp nhận thêm "wasn't", "was not" (dạng was cũng chấp nhận)
B1-09-E07 blank-4: chấp nhận thêm "that" ("the day that we first met" đúng)
B1-10-E07 blank-3: chấp nhận thêm "cleaned" (không lùi thì vẫn chấp nhận)
B1-11-E07 blank-2: chấp nhận thêm "if" (if và whether đều đúng)
B1-11-E07 blank-4: chấp nhận thêm "whether" (if và whether đều đúng)
B1-12-E07 blank-4: chấp nhận thêm "won't you", "would you", "can you", "could you" (câu mệnh lệnh có nhiều đuôi hợp lệ)
B1-13-E07 blank-1: chấp nhận thêm "may", "could" (might/may/could cùng chỉ khả năng)
B1-16-E06 blank-4: chấp nhận thêm "was" ("If only I was" chấp nhận trong văn nói)
B1-18-E07 blank-1: chấp nhận thêm "if" (if và whether đều đúng)
B1-04-E08: chấp nhận thêm "I will be working at ten in the garden tomorrow."
total changes 23
```

## Vòng 2 (sau code review)

Các thay đổi ở vòng này:

- Sửa các hint làm lộ đúng từ cần chọn. Hint hiện ra dưới dạng placeholder trong ô nhập, nên học viên nhìn thấy được.
- Thêm ngữ cảnh cho B1-14-E07 để chỉ còn một đáp án đúng.
- Bổ sung các biến thể đáp án còn thiếu.
- Đổi một câu gần trùng.

Giữ nguyên các hint là động từ gốc, ví dụ "(arrive)", vì học viên vẫn phải chọn đúng thì.

```text
B1-03-E06 blank-4: hint (would) → (would / used to)
B1-03-E07 blank-4: hint (would) → (would / used to)
B1-11-E07 blank-2: hint (whether) → (if / whether)
B1-11-E07 blank-4: hint (if) → (if / whether)
B1-18-E07 blank-1: hint (whether) → (if / whether)
B1-13-E07 blank-1: hint (might) → (might / must / can't)
B1-13-E07 blank-3: hint (can't) → (must / can't)
B1-15-E06 blank-1: hint (so) → (so / such)
B1-15-E06 blank-2: hint (such) → (so / such)
B1-15-E07 blank-1: hint (so) → (so / such)
B1-15-E07 blank-2: hint (that) → (that / than)
B1-16-E06 blank-3: hint (would) → (will / would)
B1-16-E07 blank-2: hint (could) → (can)
B1-17-E06 blank-1: hint (give up) → (bỏ cuộc)
B1-17-E06 blank-3: hint (run out of) → (dùng hết)
B1-17-E06 blank-4: hint (look after) → (chăm sóc)
B1-17-E07 blank-2: hint (run out of) → (dùng hết)
B1-17-E07 blank-3: hint (get on with) → (hòa hợp với)
B1-17-E06 blank-2: hint (look for) → (tìm kiếm)
B1-17-E07 blank-1: hint (come across) → (tình cờ thấy)
B1-17-E07 blank-4: hint (get off) → (xuống xe)
B1-14-E07 blank-4: "We stopped [ ] photos" → "We stopped the car [ ] photos" (trước đó "taking" cũng đúng)
B1-03-E07 blank-2: chấp nhận thêm "got used to", "have got used to", "'ve got used to"
B1-03-E07 blank-3: chấp nhận thêm "had been living", "'d been living"
B1-11-E08: "last year" → "the previous year" (đổi trạng từ thời gian trong câu gián tiếp)
B1-18-E02: đổi câu để không gần trùng B1-08-E02 (passport will be checked)
```

## Toàn bộ bài sau khi sửa

Ký hiệu: `*` là đáp án đúng; `[T]`/`[F]` là nhận định đúng/sai; `[a|b]` là các đáp án được chấp nhận; `⇒` nối cặp ghép hoặc câu đáp án.

```text

##### B1-01 — Present perfect continuous; for and since | Hiện tại hoàn thành tiếp diễn; for và since
E01 MC: My sister ____ for this company since 2019. | *has worked / works / worked / is working
E02 MC: Mai is still at her desk. She ____ the report since 8 a.m. | writes / *has been writing / was writing / has written
E03 MC: I ____ my cousin for ten years, so I know her very well. | have been knowing / know / *have known / was knowing
E04 TF: [T] They have been waiting for the bus since half past six.  [F] I have been knowing Nam for three years.  [T] She has worked at the hospital for six years.  [F] We have lived here since five years.  [T] How long have you been studying Japanese?
E05 MA: How long have you been waiting? ⇒ Since two o'clock. ‖ Why are your shoes muddy? ⇒ I have been walking through the garden. ‖ How long has Lan known Minh? ⇒ For about five years. ‖ Why does the kitchen smell of soap? ⇒ I have been cleaning it. ‖ Is it still raining? ⇒ Yes, it has been raining since dawn.
E06 FB: 1. I [have been waiting|'ve been waiting|have waited|'ve waited] for the bus for twenty minutes, and it is still not here. (wait)  2. He [has known|'s known] his neighbour since 2018. (know)  3. They [have been repairing|'ve been repairing] the road all morning and are still working. (repair)  4. How long [has she lived|has she been living] in Da Nang? (she/live)
E07 FB: 1. My aunt [has taught|'s taught|has been teaching|'s been teaching] at this school since 2015. (teach)  2. We [have been standing|'ve been standing|have stood|'ve stood] in this queue for nearly an hour. (stand)  3. I [haven't seen|have not seen] Minh since Tuesday. (not/see)  4. The children [have been playing|'ve been playing] outside for two hours and are still playing. (play)
E08 SU: {o'clock. / been / since / English / studying / has / nine / She} ⇒ She has been studying English since nine o'clock.
E09 SU: {working / have / this / you / hotel? / long / at / How / been} ⇒ How long have you been working at this hotel?
E10 EC: I have been knowing my best friend for twelve years. ⇒ I have known my best friend for twelve years. ‖ I've known my best friend for twelve years.

##### B1-02 — Past perfect | Quá khứ hoàn thành
E01 MC: When my father arrived home, my mother ____ dinner and the table was ready. | prepared / has prepared / was preparing / *had prepared
E02 MC: By the time the guests arrived, Linh ____ the house. | *had cleaned / cleaned / has cleaned / was cleaning
E03 MC: It was the first time I ____ sushi. | ate / *had eaten / have eaten / was eating
E04 TF: [T] By the time we arrived, the concert had begun.  [F] She had went to bed before ten.  [T] I had never seen snow before I visited Sapa.  [F] We had finish dinner before the guests arrived.  [T] After the rain had stopped, we continued our walk.
E05 MA: By the time Lan called, ⇒ I had already gone to bed. ‖ Nam missed the train ⇒ because he had left his ticket at home. ‖ The children were hungry ⇒ because they had not eaten lunch. ‖ She recognised the song ⇒ because she had heard it before. ‖ When I opened the fridge, ⇒ I found that Minh had finished the milk.
E06 FB: 1. When I reached the airport, the plane [had taken off|'d taken off] already. (take off)  2. By the time we got home, the children [had gone|'d gone] to bed. (go)  3. By 2022, she [had never visited|'d never visited] Hue. (never/visit)  4. It was the first time they [had met|'d met] each other. (meet)
E07 FB: 1. By the time the meeting started, he [hadn't finished|had not finished] the report. (not/finish)  2. When Mai reached the cinema, the film [had already started|'d already started]. (already/start)  3. I [had never tasted|'d never tasted] such spicy food before that trip. (never/taste)  4. Lan realised she [had lost|'d lost] her key before leaving home. (lose)
E08 SU: {friends / Minh / homework / arrived. / his / had / before / finished / his} ⇒ Minh had finished his homework before his friends arrived. ‖ Minh had finished his homework before his friends arrived.
E09 SU: {reached / when / The / left / train / station. / had / the / we} ⇒ The train had left when we reached the station.
E10 EC: She had went to Da Lat before the holiday began. ⇒ She had gone to Da Lat before the holiday began. ‖ She'd gone to Da Lat before the holiday began.

##### B1-03 — used to and would for past habits | used to và would cho thói quen quá khứ
E01 MC: There ____ a small market here, but it closed last year. | would be / was used to be / *used to be / use to be
E02 MC: When my grandfather was young, he ____ us stories every evening. | will tell / was telling / tells / *would tell
E03 MC: After a few weeks, Hoa is getting used to ____ in a big city. | *living / live / lived / be living
E04 TF: [T] I used to walk to school when I was a child.  [F] Did you used to play tennis?  [T] My grandmother would tell us stories every night.  [F] I am used to wake up at five.  [T] There used to be a cinema near my house.
E05 MA: I used to live in Vung Tau. ⇒ I don't live there now. ‖ I'm used to getting up early. ⇒ It is normal for me now. ‖ Every summer, we would visit our cousins. ⇒ This was a repeated past habit. ‖ I didn't use to like coffee. ⇒ I like it now. ‖ She is getting used to her new job. ⇒ She is gradually adapting.
E06 FB: 1. My father [used to] ride a bicycle to work. (use to)  2. I [am used to|'m used to] working night shifts now. (be/use to)  3. Did you [use to] play badminton at school? (use to)  4. In those days, my mother [would|used to] bake bread every Sunday. (would / used to)
E07 FB: 1. I [didn't use to|did not use to] enjoy spicy food, but I love it now. (not/use to)  2. Visitors [are getting used to|'re getting used to|are used to|'re used to|get used to|got used to|have got used to|'ve got used to] the noisy streets. (be/get used to)  3. By the time we moved, we [had lived|'d lived|had been living|'d been living] in Hue for ten years. (live)  4. When we were children, our teacher [would|used to] read us a story after lunch. (would / used to)
E08 SU: {weekend. / used / every / football / I / play / to / lunch / after} ⇒ I used to play football after lunch every weekend.
E09 SU: {in / getting / She / living / is / used / Hanoi. / to} ⇒ She is getting used to living in Hanoi.
E10 EC: I didn't used to enjoy early mornings. ⇒ I didn't use to enjoy early mornings. ‖ I did not use to enjoy early mornings.

##### B1-04 — Future continuous | Tương lai tiếp diễn
E01 MC: At eight tomorrow morning, we ____ over the Pacific. | will flying / *will be flying / will be flew / will been flying
E02 MC: Don't phone Nam at seven; he ____ his piano lesson then. | will having / will have been / *will be having / is have
E03 MC: Will you be ____ the car this evening? | use / used / to use / *using
E04 TF: [T] This time next week, I will be travelling to Hue.  [T] Don't call at nine because we will be having dinner then.  [F] She will being waiting outside.  [F] I will be knowing the answer tomorrow.  [T] Will you be using the computer this afternoon?
E05 MA: What will you be doing at 7 a.m. tomorrow? ⇒ I will be catching the bus to work. ‖ Where will your parents be this time next month? ⇒ They will be lying on a beach in Nha Trang. ‖ Why won't Nam answer the phone at eight? ⇒ He will be attending an online meeting. ‖ Why can't I call Hoa at noon on Saturday? ⇒ She will be playing tennis with Mai. ‖ Will you be using this computer later? ⇒ No, you can borrow it.
E06 FB: 1. This time tomorrow, Lan [will be taking|'ll be taking] her final exam. (take)  2. They [won't be staying|will not be staying] at a hotel during the conference. (not/stay)  3. At eight tonight, I [will be watching|'ll be watching] my favourite programme. (watch)  4. Will you [be working|work] late again tomorrow? (work)
E07 FB: 1. Please call later; I [will be having|'ll be having] dinner at seven. (have)  2. They [will be driving|'ll be driving] to the airport at this time tomorrow. (drive)  3. Don't worry, I [will know|'ll know] the result by then. (know)  4. What [will you be doing] at nine tomorrow morning? (you/do)
E08 SU: {garden / in / working / ten / be / I / will / tomorrow. / at / the} ⇒ I will be working in the garden at ten tomorrow. ‖ I will be working at ten in the garden tomorrow.
E09 SU: {this / your / using / evening? / you / laptop / Will / be} ⇒ Will you be using your laptop this evening?
E10 EC: At eight tomorrow, we will having breakfast. ⇒ At eight tomorrow, we will be having breakfast. ‖ At eight tomorrow, we'll be having breakfast.

##### B1-05 — Zero and first conditionals; unless | Câu điều kiện loại 0 và loại 1; unless
E01 MC: Water ____ at 100°C if you heat it. | *boils / boil / boiling / is boil
E02 MC: If Mai ____ early, she will help us prepare dinner. | will arrive / *arrives / arrived / is arrive
E03 MC: You won't pass the exam ____ you study harder. | if / when / *unless / because
E04 TF: [T] If you mix red and blue, you get purple.  [F] If it will rain tomorrow, we will cancel the trip.  [T] Unless Lan hurries, she will miss the train.  [T] If you need help, call me.  [F] Unless you don't book now, you will lose the room.
E05 MA: If you heat butter, ⇒ it melts. ‖ If you don't take an umbrella, ⇒ you will get wet. ‖ Unless we leave now, ⇒ we will be late for the film. ‖ If Hoa calls, ⇒ tell her I am in a meeting. ‖ If I finish work early, ⇒ I will meet you at the café.
E06 FB: 1. If you leave milk in the sun, it [goes|will go] bad. (go)  2. If we [arrive] late, we will miss the opening scene. (arrive)  3. Unless you [wear] a coat, you will feel cold. (wear)  4. If Minh [calls], I will let you know. (call)
E07 FB: 1. If you press this button, the machine [stops|will stop]. (stop)  2. We will go cycling unless it [rains]. (rain)  3. If she [doesn't hurry|does not hurry], she will miss the train. (not/hurry)  4. Plants die if they [don't get|do not get] enough water. (not/get)
E08 SU: {stay / tomorrow, / at / will / rains / If / home. / it / we} ⇒ If it rains tomorrow, we will stay at home.
E09 SU: {miss / Unless / will / the / you / train. / hurry, / you} ⇒ Unless you hurry, you will miss the train. ‖ Unless you hurry, you will miss the train.
E10 EC: If she will call me, I will explain the plan. ⇒ If she calls me, I will explain the plan. ‖ If she calls me, I'll explain the plan.

##### B1-06 — Second conditional | Câu điều kiện loại 2
E01 MC: If I ____ more time, I would learn the guitar. | have / will have / would have / *had
E02 MC: If I were you, I ____ that old motorbike. | *wouldn't buy / don't buy / won't buy / didn't buy
E03 MC: If we lived closer to the beach, we ____ every morning. | can swim / *could swim / will swim / swam
E04 TF: [T] If they lived nearer, we would visit them more often.  [F] If I would know her number, I would call her.  [T] If he studied harder, he would pass the test.  [F] If I am you, I would accept the offer.  [T] She would travel more if she had enough money.
E05 MA: If Ba had a bike, ⇒ he wouldn't need to take the bus. ‖ If Mai knew his number, ⇒ she would call him. ‖ If they practised more, ⇒ their English would improve. ‖ If I were you, ⇒ I would apologise to Lan. ‖ If the weather were warmer, ⇒ we could eat outside.
E06 FB: 1. If I [knew] the answer, I would tell you. (know)  2. She [would visit|'d visit] her grandparents if she had more time. (visit)  3. If I [were|was] you, I would ask for help. (be)  4. What [would you do] if you found a wallet? (you/do)
E07 FB: 1. If we [didn't live|did not live] so far from the city, we would go out more often. (not/live)  2. Hoa [could speak] French if she took evening classes. (could/speak)  3. If he [weren't|were not|wasn't|was not] so busy, he would join us. (not/be)  4. I would help you if I [had] more free time. (have)
E08 SU: {money, / more / travel / I / had / abroad. / would / If / I} ⇒ If I had more money, I would travel abroad. ‖ If I had more money, I would travel abroad.
E09 SU: {if / abroad? / Minh / live / would / moved / Where / he} ⇒ Where would Minh live if he moved abroad?
E10 EC: If I would have a car, I would drive to work. ⇒ If I had a car, I would drive to work. ‖ If I had a car, I'd drive to work.

##### B1-07 — Passive voice — present and past simple | Câu bị động — hiện tại đơn và quá khứ đơn
E01 MC: This temple ____ more than five hundred years ago. | built / is built / *was built / has built
E02 MC: English and French ____ in many schools in Vietnam. | teach / is taught / was taught / *are taught
E03 MC: The winning photos ____ on the school website every year. | *are displayed / display / were displayed / are displaying
E04 TF: [T] This bridge was repaired last month.  [F] The house built in 1990.  [T] Rice is grown in the Mekong Delta.  [F] The letter was wrote yesterday.  [T] Are these rooms cleaned every morning?
E05 MA: They grow coffee in Buon Ma Thuot. ⇒ Coffee is grown in Buon Ma Thuot. ‖ The postman delivered the letters yesterday. ⇒ The letters were delivered yesterday. ‖ The city opened this museum in 2012. ⇒ This museum was opened in 2012. ‖ People send millions of emails every day. ⇒ Millions of emails are sent every day. ‖ The flood destroyed the old bridge. ⇒ The old bridge was destroyed by the flood.
E06 FB: 1. My bicycle [was stolen] last night. (steal)  2. These cakes [are made] by my aunt every Sunday. (make)  3. [Was] the package delivered yesterday? (be)  4. The trees [were planted] by students last spring. (plant)
E07 FB: 1. The report [wasn't finished|was not finished] yesterday. (not/finish)  2. How often [are] these bins emptied? (be)  3. Portuguese [is spoken] in Brazil. (speak)  4. [Were] these photos taken by your brother? (be)
E08 SU: {by / was / The / opened / mayor. / the / library / new} ⇒ The new library was opened by the mayor.
E09 SU: {every / washed / at / garage / Cars / are / this / Friday.} ⇒ Cars are washed at this garage every Friday.
E10 EC: The report was wrote by Lan yesterday. ⇒ The report was written by Lan yesterday.

##### B1-08 — Passive with modals, future and perfect | Câu bị động với modal, tương lai và hoàn thành
E01 MC: Passports ____ at the entrance. | must show / *must be shown / must been shown / are must shown
E02 MC: The test results ____ tomorrow morning. | will announce / will been announced / *will be announced / are announce
E03 MC: The invitation ____ already ____. | have / been sent / has / sent / is / been sent / *has / been sent
E04 TF: [T] Your order will be delivered tomorrow.  [F] The results has been announced.  [T] This song is being played on the radio now.  [F] The forms must finished today.  [T] The road is going to be repaired next week.
E05 MA: They will send the tickets by email. ⇒ The tickets will be sent by email. ‖ Someone is cleaning the classroom now. ⇒ The classroom is being cleaned now. ‖ We have already completed the project. ⇒ The project has already been completed. ‖ You must sign the documents by Friday. ⇒ The documents must be signed by Friday. ‖ They are going to demolish the building next month. ⇒ The building is going to be demolished next month.
E06 FB: 1. The results [will be posted|'ll be posted|are going to be posted|'re going to be posted] online tomorrow. (post)  2. This old theatre [is being repaired] at the moment. (repair)  3. All essays [must be submitted] by Friday. (must/submit)  4. The parcel [has been delivered|'s been delivered|was delivered] already. (deliver)
E07 FB: 1. The new school [is going to be built|'s going to be built|will be built|'ll be built] next year. (build)  2. These documents [should be checked] before submission. (should/check)  3. I can't find the email; it [hasn't been answered|has not been answered] yet. (not/answer)  4. The cake [is being decorated] by the chef right now. (decorate)
E08 SU: {opened / be / bridge / The / month. / new / will / next} ⇒ The new bridge will be opened next month.
E09 SU: {signed / Friday. / forms / by / must / These / be} ⇒ These forms must be signed by Friday.
E10 EC: The room has cleaned by the staff. ⇒ The room has been cleaned by the staff. ‖ The room's been cleaned by the staff.

##### B1-09 — Defining relative clauses | Mệnh đề quan hệ xác định
E01 MC: The teacher ____ helped me yesterday is Ms Hoa. | *who / which / whose / where
E02 MC: This is the café ____ we first met. | which / *where / who / whose
E03 MC: Câu nào dùng mệnh đề quan hệ đúng? | The man which helped us was very kind. / The man who he helped us was very kind. / *The man who helped us was very kind. / The man whose helped us was very kind.
E04 TF: [F] The woman which lives next door is a doctor.  [T] The book that I borrowed was interesting.  [F] The film that I watched it was boring.  [T] I have a friend whose father is a pilot.  [F] The city which I was born is very old.
E05 MA: The woman who called you ⇒ is my aunt. ‖ The phone that I bought ⇒ stopped working yesterday. ‖ The boy whose bike was stolen ⇒ is looking for his bike everywhere. ‖ The restaurant where we ate ⇒ has closed. ‖ The day when we first met ⇒ was rainy.
E06 FB: 1. This is the singer [who|that] won the competition. (relative pronoun)  2. I found the dog [which|that] lives next door. (relative pronoun)  3. The girl [whose] mother is a nurse is my classmate. (relative pronoun)  4. That is the school [where] my father studied. (relative pronoun)
E07 FB: 1. People [who|that] work here start at seven. (relative pronoun)  2. This is the town [where] my grandparents live. (relative pronoun)  3. I met a writer [whose] books are famous. (relative pronoun)  4. I still remember the day [when|that] we first met. (relative pronoun)
E08 SU: {kind. / helped / very / woman / was / me / The / who} ⇒ The woman who helped me was very kind.
E09 SU: {where / shop / she / the / works? / Do / know / you} ⇒ Do you know the shop where she works?
E10 EC: The girl which lives next door is my cousin. ⇒ The girl who lives next door is my cousin. ‖ The girl that lives next door is my cousin.

##### B1-10 — Reported speech — statements | Câu gián tiếp — câu kể
E01 MC: My teacher ____ me that the test would be on Monday. | said / spoke / asked / *told
E02 MC: Lan said that she ____ the film the day before. | *had watched / watches / has watched / will watch
E03 MC: Mai told me that ____ would help with the project. | her / *she / hers / herself
E04 TF: [T] He told me that he was busy.  [F] She told that she was hungry.  [T] Hoa said she would call the next day.  [F] They told that they had finished the work.  [T] Minh told us that he had lost his key.
E05 MA: "I am tired." ⇒ She said she was tired. ‖ "I will help you." ⇒ He told me he would help me. ‖ "I may be late." ⇒ She said she might be late. ‖ "I can swim." ⇒ Ba said he could swim. ‖ "I don't like coffee." ⇒ My sister told me she didn't like coffee.
E06 FB: 1. Nam [said] that the room was cold. (say)  2. She [told] me that she had finished. (tell)  3. He said that he [would|'d|will|'ll] come back later. (will)  4. Lan said she [had visited|'d visited|visited] Hue the year before. (visit)
E07 FB: 1. She said she [could] drive us to the station. (can)  2. He told me he [could] swim when he was five. (can)  3. Minh said he [had cleaned|'d cleaned|cleaned] the kitchen that day. (clean)  4. They told us they [wanted] to stay another night. (want)
E08 SU: {seen / she / Lan / film. / said / had / that / the} ⇒ Lan said that she had seen the film.
E09 SU: {a / we / test. / that / would / teacher / have / My / us / told} ⇒ My teacher told us that we would have a test.
E10 EC: He said me that he was tired. ⇒ He told me that he was tired. ‖ He said to me that he was tired.

##### B1-11 — Reported speech — questions | Câu gián tiếp — câu hỏi
E01 MC: She asked me if I ____ ready to leave. | am / were / *was / be
E02 MC: He asked me where I ____ the previous summer. | go / did go / have gone / *had gone
E03 MC: Could you tell me where ____? | *the station is / is the station / does the station be / the station does
E04 TF: [T] She asked me whether I liked tea.  [F] She asked me where was I going.  [T] They asked what time the train had left.  [F] She asked me was I tired.  [T] Do you know if the shop is open?
E05 MA: "Are you coming?" ⇒ He asked if I was coming. ‖ "Where is the bank?" ⇒ She asked where the bank was. ‖ "Did you finish?" ⇒ They asked whether I had finished. ‖ "What time does it start?" ⇒ Minh asked what time it started. ‖ "Can you swim?" ⇒ Lan asked if I could swim.
E06 FB: 1. Lan asked me [if|whether] I could help her. (if/whether)  2. He asked where I [lived] when I was a child. (live)  3. They asked whether the bus [had already left|'d already left|already left]. (already/leave)  4. She asked what time the film [started|had started|'d started]. (start)
E07 FB: 1. Can you tell me where [the museum is]? (the museum/be)  2. He asked [whether|if] I wanted tea or coffee. (if / whether)  3. Mi asked what I [was doing] at that time. (do)  4. I wonder [if|whether] she will come. (if / whether)
E08 SU: {lived / where / me / the previous / year. / asked / I / She} ⇒ She asked me where I lived the previous year.
E09 SU: {could / help / asked / him. / if / I / He} ⇒ He asked if I could help him.
E10 EC: She asked me where did I live. ⇒ She asked me where I lived.

##### B1-12 — Question tags | Câu hỏi đuôi
E01 MC: You have visited Hue, ____? | did you / *haven't you / have you / don't you
E02 MC: Lan doesn't like spicy food, ____? | doesn't she / is she / *does she / has she
E03 MC: Let's take a short break, ____? | will we / do we / aren't we / *shall we
E04 TF: [T] It's cold outside, isn't it?  [F] You like coffee, isn't it?  [T] She has never eaten sushi, has she?  [F] They can swim, can he?  [T] Let's meet at six, shall we?
E05 MA: You are from Da Nang, ⇒ aren't you? ‖ Mai went home early, ⇒ didn't she? ‖ They won't be late, ⇒ will they? ‖ I'm next in line, ⇒ aren't I? ‖ Close the window, ⇒ will you?
E06 FB: 1. You like green tea, [don't you]?  2. She didn't call, [did she]?  3. We've met before, [haven't we]?  4. Everyone enjoyed the trip, [didn't they]?
E07 FB: 1. I'm late, [aren't I]?  2. Let's book the tickets, [shall we]?  3. He hardly ever complains, [does he]?  4. Bring your notebook, [will you|won't you|would you|can you|could you]?
E08 SU: {haven't / been / Hue, / to / You / have / you?} ⇒ You have been to Hue, haven't you?
E09 SU: {does / meat, / Mai / she? / eat / doesn't} ⇒ Mai doesn't eat meat, does she?
E10 EC: You have visited Sapa, did you? ⇒ You have visited Sapa, haven't you? ‖ You've visited Sapa, haven't you?

##### B1-13 — Modals of deduction (present) | Modal chỉ suy luận: must, might, can't
E01 MC: The lights are on and her car is outside. I'm almost certain she ____ at home. | *must be / might be / can't be / mustn't be
E02 MC: That ____ be Minh's car; he sold it last month. | mustn't / *can't / shouldn't / needn't
E03 MC: Câu nào chỉ nêu khả năng mà không khẳng định chắc chắn? | Nam must be in a meeting. / Nam can't be in a meeting. / *Nam might be in a meeting. / Nam mustn't be in a meeting.
E04 TF: [T] She must be tired after working all day.  [F] It mustn't be true because those photos are clearly fake.  [T] That can't be Lan because she is in Hue.  [T] The keys might be in your bag.  [F] It may is true.
E05 MA: His car is outside and the lights are on. ⇒ He must be at home. ‖ Mai has a high fever. ⇒ She must be ill. ‖ The shop is dark and has a closed sign. ⇒ It can't be open. ‖ She left work an hour ago. ⇒ She might be on her way home. ‖ You have worked since dawn. ⇒ You must be exhausted.
E06 FB: 1. The lights are off, so Lan [can't be|cannot be] at home. (be)  2. She has worked all day; she [must be] tired. (be)  3. Minh's bike is missing; he [might be|may be|could be] in the park. (be)  4. That [can't|cannot] be true! (can/not)
E07 FB: 1. The phone [might|may|could] be on the table, but I am not sure. (might / must / can't)  2. You [must be] joking! That news is unbelievable. (must)  3. It [can't|cannot] be five already; my watch says four. (must / can't)  4. She [may not know|might not know] about the change yet. (may/not/know)
E08 SU: {after / tired / long / be / journey. / must / the / She} ⇒ She must be tired after the long journey.
E09 SU: {easy / finished / test / be / can't / nobody / The / because / it.} ⇒ The test can't be easy because nobody finished it.
E10 EC: He mustn't be at home because all the lights are off. ⇒ He can't be at home because all the lights are off. ‖ He cannot be at home because all the lights are off.

##### B1-14 — Gerunds and infinitives (extended) | Danh động từ và động từ nguyên mẫu (mở rộng)
E01 MC: She is interested in ____ Vietnamese food. | cook / to cook / cooked / *cooking
E02 MC: I have stopped ____ coffee completely because it gives me headaches. | *drinking / to drink / drink / drunk
E03 MC: We stopped at a café ____ some water. | buying / *to buy / buy / bought
E04 TF: [T] Learning English is fun.  [T] I look forward to meeting your family.  [F] I look forward to see you.  [T] He stopped smoking last year.  [F] I went there for buying a ticket.
E05 MA: Ngừng hẳn một thói quen ⇒ stop + V-ing ‖ Dừng lại để làm việc gì ⇒ stop + to V ‖ Việc cần nhớ trong tương lai ⇒ remember + to V ‖ Việc đã xảy ra trong quá khứ ⇒ remember + V-ing ‖ Mục đích của một hành động ⇒ to + V
E06 FB: 1. She is good at [solving] difficult problems. (solve)  2. I went to the shop [to buy] some milk. (buy)  3. Don't forget [to lock] the door tonight. (lock)  4. It is hard to stop [smoking] after twenty years. (smoke)
E07 FB: 1. [Reading] books in English helps improve vocabulary. (read)  2. I look forward to [hearing] from you. (hear)  3. She tried [to open] the jar, but the lid was too tight. (open)  4. We stopped the car [to take] photos of the beautiful lake. (take)
E08 SU: {you / to / week. / seeing / look / next / forward / I} ⇒ I look forward to seeing you next week.
E09 SU: {smoking / doctor / He / after / stopped / spoke / his / to / him.} ⇒ He stopped smoking after his doctor spoke to him.
E10 EC: She is interested in learn Japanese. ⇒ She is interested in learning Japanese.

##### B1-15 — so/such … that; so do I / neither do I | so/such … that; so do I / neither do I
E01 MC: It was ____ difficult exam that many students failed. | so / so a / *such a / such
E02 MC: The soup was ____ salty that we couldn't eat it. | such / too / enough / *so
E03 MC: I enjoy watching football. – ____ | *So do I. / So am I. / Neither do I. / So I do.
E04 TF: [T] The book was so dull that I stopped reading it.  [F] It was so a good day.  [T] They are such kind people.  [F] I don't like coffee. – So don't I.  [T] Lan can swim. – So can I.
E05 MA: I love Vietnamese coffee. ⇒ So do I. ‖ Nam can't drive. ⇒ Neither can I. ‖ Mai has visited Hue. ⇒ So have I. ‖ We won't be late. ⇒ Neither will I. ‖ Lan is a doctor. ⇒ So is Minh.
E06 FB: 1. The box was [so] heavy that nobody could lift it.  2. It was [such] a beautiful morning.  3. I can play the guitar. – So [can] I.  4. They didn't come. – Neither [did] I.
E07 FB: 1. There were [so] many people that we couldn't enter.  2. The film was so long [that] we left early.  3. Hoa has finished her work. – So [have] I.  4. I don't eat meat. – Neither [do] I.
E08 SU: {that / fell / film / boring / asleep. / I / was / so / The} ⇒ The film was so boring that I fell asleep.
E09 SU: {French, / can / her / speak / so / She / and / brother. / can} ⇒ She can speak French, and so can her brother. ‖ She can speak French, and so can her brother.
E10 EC: It was so a beautiful day that we went outside. ⇒ It was such a beautiful day that we went outside.

##### B1-16 — wish about the present | wish ở hiện tại
E01 MC: I wish I ____ more free time. | have / *had / will have / would have
E02 MC: She wishes she ____ speak Korean. | can / would / *could / will
E03 MC: I wish you ____ making that noise. | stop / would stopping / will stop / *would stop
E04 TF: [T] I wish I had a larger garden.  [F] I wish I have a car.  [T] She wishes she could play the piano.  [F] I wish I would be rich.  [T] I wish you would listen to me.
E05 MA: I wish I knew Lan's number. ⇒ I don't know her number. ‖ She wishes she were taller. ⇒ She is short. ‖ We wish we could stay longer. ⇒ We have to leave soon. ‖ He wishes he had a car. ⇒ He does not have a car. ‖ I wish it would stop raining. ⇒ The rain is continuing.
E06 FB: 1. I wish I [had] a larger flat. (have)  2. He wishes he [could] cook better. (can)  3. We wish it [would] stop raining. (will / would)  4. If only I [were|was] taller. (be)
E07 FB: 1. I wish I [didn't have|did not have] to work every Saturday. (not/have)  2. He wishes he [could] afford a new laptop. (can)  3. They wish they [lived] closer to the beach. (live)  4. I wish you [would stop] leaving your shoes there. (would/stop)
E08 SU: {speak / fluently. / Japanese / could / wish / I / I} ⇒ I wish I could speak Japanese fluently. ‖ I wish I could speak Japanese fluently.
E09 SU: {than / taller / brother. / wishes / her / were / She / she} ⇒ She wishes she were taller than her brother.
E10 EC: I wish I have a bigger room. ⇒ I wish I had a bigger room.

##### B1-17 — Common phrasal verbs | Cụm động từ thông dụng
E01 MC: Please turn ____; the screen is too bright. | *it off / off it / it on / on it
E02 MC: I'm looking ____ my glasses everywhere. | after / *for / up / into
E03 MC: We have run ____ milk, so I need to buy some. | out / of / *out of / away from
E04 TF: [T] Turn it off before you leave.  [F] Turn off it.  [T] She looks after her elderly neighbour.  [F] I'm looking my keys for.  [T] We've run out of rice.
E05 MA: look after ⇒ take care of ‖ look for ⇒ try to find ‖ give up ⇒ stop doing something ‖ run out of ⇒ have no more of something ‖ look forward to ⇒ feel excited about a future event
E06 FB: 1. Don't [give up] learning English. (bỏ cuộc)  2. I am [looking for] my passport. (tìm kiếm)  3. We have [run out of] milk. (dùng hết)  4. I will [look after] your dog while you are away. (chăm sóc)
E07 FB: 1. She [came across] an old photo while cleaning the attic. (tình cờ thấy)  2. We have [run out of] sugar, so I will buy some. (dùng hết)  3. I [get on with] my new colleagues very well. (hòa hợp với)  4. They [got off] the bus at the wrong stop yesterday. (xuống xe)
E08 SU: {before / pick / leave. / up / you / it / Please} ⇒ Please pick it up before you leave.
E09 SU: {her / today. / little / is / after / brother / looking / She} ⇒ She is looking after her little brother today.
E10 EC: I am looking my glasses for. ⇒ I am looking for my glasses. ‖ I'm looking for my glasses.

##### B1-18 — B1 review and level test | Ôn tập B1 và kiểm tra level
E01 MC: Mai ____ Spanish since 2022 and is still taking lessons. | learned / has learned / was learning / *has been learning
E02 MC: Your passport ____ at the airport tomorrow, so keep it ready. | *will be checked / will check / are checking / will been checked
E03 MC: She asked me where I ____ before moving to Hue. | live / *had lived / did live / have lived
E04 TF: [T] The report had been sent before the meeting began.  [F] I wish I can speak French.  [T] The tickets must be collected at the desk.  [F] She asked me where did I work.  [T] Unless you leave now, you will miss the bus.
E05 MA: By the time we arrived, ⇒ the film had already started. ‖ The letter was ⇒ written by my grandmother. ‖ The project must ⇒ be completed by Friday. ‖ The woman who called ⇒ is my new manager. ‖ Mai can't be at home ⇒ because her car is missing.
E06 FB: 1. My grandfather [used to] walk five kilometres to school. (use to)  2. This bridge [was built] in 1998. (build)  3. If I [had] more time, I would join the club. (have)  4. The files [had been sent|'d been sent|were sent] before the meeting started. (send)
E07 FB: 1. The teacher asked me [whether|if] I had finished the task. (if / whether)  2. I wish I [could] drive a car. (can)  3. Don't forget [to turn off] the lights. (turn off)  4. She [gave up] her old job last month. (give up)
E08 SU: {visit / weather / Hue / were / if / could / warmer. / We / the} ⇒ We could visit Hue if the weather were warmer.
E09 SU: {where / had / teacher / us / parked. / The / asked / we} ⇒ The teacher asked us where we had parked.
E10 EC: The mechanic which repaired my car was very friendly. ⇒ The mechanic who repaired my car was very friendly. ‖ The mechanic that repaired my car was very friendly.
```
