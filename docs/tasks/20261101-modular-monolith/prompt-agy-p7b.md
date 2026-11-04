# Việc giao cho AGY: P7b — snapshot shape JSON cho 6 module còn lại

## Bối cảnh (session mới)

- `tests/EnglishLiveTutor.Api.IntegrationTests/ApiContractSnapshotTests.cs` đã có inventory 174 endpoint và 5 response shape (health, ai-models, topics, voices, ProblemDetails 404) — chỉ phủ Catalog/host. Snapshot ở `tests/EnglishLiveTutor.Api.IntegrationTests/Snapshots/response-shapes.json`, chế độ ghi `API_SNAPSHOT_WRITE=1`. Dựng app qua `ApiTestFactory` (`tests/EnglishLiveTutor.Tests.Common`), DB InMemory.
- Solution: `EnglishPartner.BE/EnglishLiveTutor.slnx`. Làm trên worktree riêng, không commit. Không thêm package. Không mạng: `--no-restore`.

## Yêu cầu

1. Thêm test `ResponseShape_<Module>_<Endpoint>` cho **ít nhất một GET có xác thực** của mỗi module Identity, Speech, Vocabulary, Lessons, Grammar, LiveTutor (ví dụ: hồ sơ user hiện tại; lịch sử phát âm hoặc rubric đang active; danh sách flashcard hoặc kế hoạch học hôm nay; danh sách lesson; roadmap/step grammar; danh sách conversation), và **một 404 ProblemDetails** của một module khác Catalog. Chọn endpoint mà `ApiTestFactory` dựng được dữ liệu (seed qua DbContext InMemory trong test nếu cần — theo cách các test integration hiện có làm; dùng user đăng nhập như các test hiện có).
2. Shape = tên property JSON đệ quy (giống cách test hiện có), không so giá trị. Với mảng: shape của phần tử đầu (seed ít nhất 1 phần tử để shape có nghĩa).
3. Ghi snapshot bằng `API_SNAPSHOT_WRITE=1`, chạy lại không biến → pass. Không đổi code `src/`, không đổi test cũ.

## Kiểm chứng

`dotnet build EnglishLiveTutor.slnx --no-restore`, `dotnet test EnglishLiveTutor.slnx --no-build` pass. Report: danh sách endpoint mới có shape (method + route + module), số test thêm. Test phải **tự chứa** (chỉ dùng `ApiTestFactory`, DbContext InMemory, BCL) để orchestrator chép sang commit trước refactor `8884da1` và so shape.
