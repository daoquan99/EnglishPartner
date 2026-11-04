# Việc giao cho AGY: P6b — snapshot contract API

## Bối cảnh (session mới)

- P6a đã tách test theo module; project `tests/EnglishLiveTutor.Api.IntegrationTests` dùng `ApiTestFactory` (WebApplicationFactory) từ `tests/EnglishLiveTutor.Tests.Common`. Làm trên `development`, không commit. Không thêm package mới (dùng xunit + System.Text.Json có sẵn). Không mạng: `--no-restore`.
- Mục tiêu: chứng minh refactor không đổi contract HTTP/SignalR mà FE (`EnglishPartner.FE/src/shared/api/openapi.types.ts`) đang dùng.

## Yêu cầu

1. Test `ApiContractSnapshotTests.EndpointInventory_MatchesSnapshot`: dựng app qua `ApiTestFactory`, lấy `EndpointDataSource` từ DI, với mọi `RouteEndpoint` ghi một dòng `<HTTP methods> <route pattern> | auth=<RequireAuthorization/AllowAnonymous/policy names> | rateLimit=<policy hoặc -> | permission=<RequirePermission nếu đọc được từ metadata hoặc ->`, sắp xếp ordinal; hub SignalR (route pattern của 3 hub) cũng có mặt. So với file `tests/EnglishLiveTutor.Api.IntegrationTests/Snapshots/endpoint-inventory.txt`.
2. Sinh snapshot trên code hiện tại (chế độ ghi). Test phải **tự chứa**: chỉ phụ thuộc `ApiTestFactory` + BCL, để orchestrator có thể chép nguyên file test sang commit cũ `8884da1` và sinh inventory để so (orchestrator làm bước so sánh này, không phải worker).
3. Chế độ ghi chỉ khi `API_SNAPSHOT_WRITE=1`; bình thường test fail khi lệch, message in diff dòng thêm/bớt.
4. Test `ResponseShape_*` cho ~5 endpoint đại diện không cần DB thật ngoài InMemory seed sẵn của `ApiTestFactory` (ví dụ list AI models, list topics, list voices, health, lỗi ProblemDetails 404): so tên property JSON (đệ quy, không so giá trị) với snapshot `Snapshots/response-shapes.json`, cùng chế độ ghi.
5. Không đổi code `src/`.

## Kiểm chứng

`dotnet build --no-restore`, `dotnet test` toàn bộ pass. Report: số endpoint trong inventory, khác biệt với `8884da1` (phải là "không có"), danh sách endpoint có shape snapshot.
