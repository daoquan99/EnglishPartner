# Việc giao cho AGY: P9 — `Program.cs` map endpoint theo module

## Bối cảnh (session mới — đọc kỹ)

- Luật: `AGENTS.md` (luật 15: refactor chỉ di chuyển), `EnglishPartner.BE/AGENTS.md`, `docs/adr/0006-modular-monolith.md`, skill `EnglishPartner.BE/.agents/skills/api-realtime/SKILL.md`.
- Solution `EnglishPartner.BE/EnglishLiveTutor.slnx`. Làm trực tiếp trên `development`, **không commit**. Không mạng: `dotnet restore --ignore-failed-sources`, rồi `--no-restore`.
- Mỗi module đã có hàm gom endpoint trong `src/Modules/<X>/EnglishLiveTutor.Modules.<X>.Presentation/<X>EndpointRouteBuilderExtensions.cs`:
  `MapCatalogEndpoints` (Prompt, Voice, Topic, AiModel), `MapLiveTutorEndpoints` (Conversation), `MapVocabularyEndpoints` (VocabularyCore, DailyLearning, VocabularyGames), `MapSpeechEndpoints` (Pronunciation, LessonRubric), `MapLessonsEndpoints` (Lesson, LessonAttempt, LessonMedia), `MapGrammarEndpoints` (GrammarCore, AdminGrammar). Identity chỉ có `MapIdentityEndpoints`.
- `src/EnglishLiveTutor.Api/Program.cs` dòng ~216–232 vẫn gọi 17 hàm rời. ASP.NET Core chọn endpoint theo độ ưu tiên route, không theo thứ tự đăng ký; thứ tự chỉ có ý nghĩa nếu hai endpoint trùng **cùng method và cùng template**.

## Yêu cầu

1. **Test trước** — trong `tests/EnglishLiveTutor.Api.IntegrationTests/`, thêm test (file mới `EndpointRegistrationTests.cs`, dùng `ApiTestFactory` và `EndpointDataSource` giống `ApiContractSnapshotTests`):
   - `NoTwoEndpoints_ShareMethodAndRouteTemplate`: với mọi `RouteEndpoint`, khóa = từng HTTP method (hoặc `*` nếu không có metadata method) + template đã chuẩn hóa (bỏ tên tham số và constraint: `{id:guid}` → `{}`, so sánh không phân biệt hoa thường, bỏ `/` cuối). Không có khóa trùng; khi fail in danh sách trùng.
   Chạy test này **trước khi sửa `Program.cs`**, nó phải pass (ghi kết quả vào report).
2. **Sửa `Program.cs`**: thay 17 lời gọi rời bằng
   ```csharp
   app.MapHealthEndpoints();
   app.MapIdentityEndpoints();
   app.MapCatalogEndpoints();
   app.MapLiveTutorEndpoints();
   app.MapVocabularyEndpoints();
   app.MapSpeechEndpoints();
   app.MapLessonsEndpoints();
   app.MapGrammarEndpoints();
   ```
   Thêm/bỏ `using` cho đúng. Không đổi phần map hub, middleware, hay bất kỳ dòng nào khác. Không sửa file nào trong `src/Modules`.
3. Nếu có hàm `Map*Endpoints` con nào **không** nằm trong hàm gom của module (hoặc nằm ở hai nơi), dừng lại và ghi vào report, không tự sửa.

## Kiểm chứng (trong `EnglishPartner.BE`)

```bash
dotnet restore EnglishLiveTutor.slnx --ignore-failed-sources
dotnet build EnglishLiveTutor.slnx --no-restore
dotnet test EnglishLiveTutor.slnx --no-build
```

Kết quả bắt buộc: build 0 lỗi; toàn bộ test pass (hiện 680 + test mới); `ApiContractSnapshotTests` (inventory 174 endpoint + shape) pass **không sửa file snapshot nào**.

## Không được

Sửa file snapshot, route, DTO, endpoint trong module, thêm package, commit.

## Worker report

File đã sửa/tạo; kết quả test mới trước và sau khi sửa `Program.cs`; lệnh + số test pass/fail.
