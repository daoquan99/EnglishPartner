---
name: api-contract-sync
description: Thay đổi hoặc kiểm tra contract giữa EnglishPartner.BE và EnglishPartner.FE — route, DTO, field JSON, header phân trang, ProblemDetails, SignalR hub method/event — để BE DTO, openapi.types.ts, API adapter và query keys FE luôn khớp. Dùng khi một task sửa endpoint/hub hoặc FE gọi API mới.
---

# Đồng bộ contract BE ↔ FE

Đọc [AGENTS.md gốc](../../../AGENTS.md) (Luật 10). Contract gồm mọi thứ client quan sát được: route + method, tên/kiểu field JSON (camelCase), mã HTTP, header `X-Total-Count`/`X-Page`/`X-Page-Size`/`X-Total-Pages`, body lỗi, tên hub method và event SignalR cùng payload.

## Vị trí

| Lớp | BE | FE |
| --- | --- | --- |
| Route | `EnglishPartner.BE/src/EnglishLiveTutor.Api/Endpoints/*Endpoints.cs` | `src/features/<feature>/api/*Api.ts` |
| DTO | `Application/Common/Models/*Dtos.cs` hoặc trong feature | `src/shared/api/openapi.types.ts`, `src/features/<feature>/types/*.types.ts` |
| Phân trang | `Api/Endpoints/PaginationEndpointExtensions.cs`, `PagedResultDto` | adapter đọc header/`paged` |
| Lỗi | ProblemDetails ([ADR-0005](../../../docs/adr/0005-problem-details-error-contract.md)) | `extractApiErrorMessage` trong `src/shared/api/httpClient.ts` |
| Hub | `Api/Hubs/*Hub.cs`, `Application/Common/Models/LiveSessionDtos.cs` | `src/features/tutor/hooks/useThreeHubLiveSession.ts` |
| Cache | | query keys trong `src/features/<feature>/hooks/*` |

## Quy trình

1. Liệt kê contract đổi trong `plan.md`: thêm, đổi tên, xóa, đổi nghĩa. Đổi nghĩa (ví dụ bắt buộc phân trang) cũng là thay đổi contract.
2. Ưu tiên thay đổi tương thích ngược (thêm field optional). Breaking change ghi rõ và sửa BE + FE trong cùng task.
3. Cập nhật đủ: BE DTO → `openapi.types.ts`/feature types → API adapter → hook/query key → component dùng field → test FE liên quan.
4. Kiểm tra bằng grep tên field/route ở cả hai phía; không để lại field FE mà BE không trả.
5. `npm run build` (type-check) và `dotnet build` phải pass. Build không chứng minh API đang chạy khớp — ghi rõ trong report.

Không bịa endpoint hay field mà BE chưa có; ghi gap và contract cần thêm.
