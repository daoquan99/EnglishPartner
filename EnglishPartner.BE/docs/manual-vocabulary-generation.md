# Sinh từ vựng bằng web AI và nhập JSON

Luồng: lấy prompt từ BE → copy sang web AI → copy response JSON → gửi response về BE để lưu. Hai API mới không cần Google API key và không gọi Google; có thể dùng response từ bất kỳ web AI nào đáp ứng schema trong prompt.

## 1. Lấy prompt

`POST /api/vocabularies/generation/prompt`

```json
{
  "requestedCount": 3,
  "cefrLevel": "B2",
  "topic": "Workplace communication",
  "excludedWords": ["hello"]
}
```

Response HTTP 200 có các fields:

```json
{
  "prompt": "You are an expert English lexicographer...",
  "promptVersion": "PROMPT_VOCAB_GEN_V1",
  "requestedCount": 3,
  "cefrLevel": "B2",
  "topic": "Workplace communication",
  "excludedWords": ["hello", "other words already stored"]
}
```

Copy toàn bộ `prompt` sang web AI. Prompt chứa schema JSON và tối đa 80 từ cần tránh, kết hợp danh sách người dùng nhập với vocabulary hiện có. RequestedCount được giới hạn 1–10 giống luồng gọi AI trực tiếp. API này chỉ đọc DB, không lưu vocabulary hay tạo audit sinh từ.

## 2. Nhập response JSON

`POST /api/vocabularies/generation/import`

```json
{
  "jsonText": "[{\"word\":\"resilient\",\"partOfSpeech\":\"adjective\",\"definitionEn\":\"Able to recover quickly\",\"meaningVi\":\"Kiên cường\",\"synonyms\":[\"strong\"],\"antonyms\":[\"fragile\"]}]",
  "modelCode": "manual-web-ai",
  "cefrLevel": "B2",
  "topic": "Workplace communication",
  "assignToTodayPlan": true
}
```

- `jsonText` là string chứa nguyên mảng JSON AI trả về. Có thể giữ cả khối Markdown có dấu ```json mở và dấu ``` đóng; BE sẽ bỏ fence. Không gửi nguyên envelope `candidates` của Google hay đoạn văn giải thích bên ngoài JSON.
- FE nên gửi object bằng Axios/fetch để thư viện tự escape jsonText, không tự nối chuỗi JSON thủ công.
- Mảng gồm 1–10 objects. Mỗi object phải có `word`, `definitionEn`, `meaningVi` là string không rỗng. Các mảng chi tiết dùng schema từ prompt: synonyms, antonyms, wordFamilies, collocations, examples, grammarPatterns, cùng IPA US/UK nếu có.
- CEFR lấy từ `cefrLevel` trong request, giống parser của luồng gọi Google hiện có. Giữ cùng CEFR/topic khi lấy prompt và import response. CEFR hợp lệ: A1, A2, B1, B2, C1, C2. Topic tối đa 128 ký tự.
- `modelCode` là nhãn nguồn để lưu audit/SourceModelId, mặc định `manual-web-ai`, tối đa 64 ký tự. Không yêu cầu model tồn tại trong AI Model Catalog vì response có thể đến từ web AI khác.
- UserId lấy từ cơ chế current-user của BE giống `/generate`; không lấy userId tùy ý từ payload. Khi không có current user, giữ fallback `default-learner` hiện có.
- `assignToTodayPlan=true` tạo hoặc bổ sung kế hoạch hôm nay (Asia/Ho_Chi_Minh) cho user. Đặt false nếu chỉ muốn lưu vocabulary.

Response HTTP 200 là `VocabularyFlashcardDto[]`, cùng contract với `/api/vocabularies/generate`. Nội dung gồm vocabulary/sense IDs và các chi tiết đã lưu; FE có thể dùng kết quả để hiển thị flashcards và invalidate today-plan query.

JSON sai cú pháp, sai root/schema, thiếu fields bắt buộc hoặc sai metadata trả HTTP 400:

```json
{ "error": "Mỗi từ vựng phải có word, definitionEn và meaningVi không rỗng." }
```

Toàn bộ JSON được parse trước khi lưu vocabulary. Vocabulary, các entity con, today-plan và audit hoàn tất được lưu trong cùng SaveChanges; audit lỗi dùng context riêng để không vô tình lưu một phần dữ liệu chưa hợp lệ.

Nhập lại cùng từ + part of speech + CEFR + definitionEn + meaningVi sẽ tái sử dụng sense hiện có, không thêm lại cùng sense/plan item. Response vẫn trả flashcard tương ứng; audit đếm các items trùng là rejected. Một nghĩa khác của cùng từ sẽ tạo sense mới dưới vocabulary hiện có. Cơ chế này xử lý các lần import tuần tự; không phải khóa idempotency cho các request đồng thời.

## Tích hợp FE

```ts
const { data: prompt } = await httpClient.post(
  '/api/vocabularies/generation/prompt',
  { requestedCount: 3, cefrLevel: 'B2', topic: 'Workplace communication' }
);

// Hiển thị prompt.prompt cho người dùng copy; lưu lại metadata cùng prompt.
const { data: cards } = await httpClient.post(
  '/api/vocabularies/generation/import',
  {
    jsonText: pastedResponse,
    cefrLevel: prompt.cefrLevel,
    topic: prompt.topic,
    modelCode: 'manual-web-ai',
    assignToTodayPlan: true,
  }
);
```

Không yêu cầu API key trong hai thao tác này. Nếu người dùng thay đổi lựa chọn CEFR/topic sau khi copy prompt, giữ metadata của prompt đã copy khi import để không gán nhầm dữ liệu.

Luồng cũ `POST /api/vocabularies/generate` vẫn hoạt động: tiếp tục gọi provider, nhưng dùng chung prompt builder, parser JSON và phần lưu dữ liệu với luồng thủ công. Không có thay đổi schema DB/migration cho tính năng này.

Có request mẫu chạy được trong Visual Studio tại `requests/manual-vocabulary-generation.http`. File đó gồm request ghi dữ liệu; chỉ chạy khi đã chọn đúng môi trường DB.
