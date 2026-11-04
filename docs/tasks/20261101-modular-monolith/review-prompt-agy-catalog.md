# Rà soát module Catalog (chỉ đọc)

Module Catalog đã tách thành 5 project (`EnglishPartner.BE/src/Modules/Catalog/EnglishLiveTutor.Modules.Catalog.{Domain,Application,Infrastructure,Presentation,Contracts}`) và là **mẫu** cho 6 module còn lại. Đọc `AGENTS.md`, `docs/adr/0006-modular-monolith.md`, skill `EnglishPartner.BE/.agents/skills/modular-monolith/SKILL.md` (mục "Project layout", "Module map") rồi rà soát. **Không sửa file nào.**

Kiểm:
1. Mỗi project chỉ chứa đúng layer của nó (Domain: entity/value object, không EF/MediatR; Application: command/query/handler/validator/DTO nội bộ, `ICatalogDbContext`; Infrastructure: DbContext, configuration, repository, migration, adapter; Presentation: endpoint; Contracts: interface/DTO/enum cho module khác).
2. `ProjectReference` đúng hướng (Domain → Shared.Domain; Contracts → Shared.Domain, Shared.Application; Application → Catalog.Domain, Catalog.Contracts, Shared.*, Identity.Contracts; Infrastructure → Catalog.Application/Domain/Contracts, Shared.Infrastructure; Presentation → Catalog.Application/Contracts, Shared.Presentation).
3. Còn file thuộc Catalog nằm ngoài `src/Modules/Catalog/` không (tìm trong `src/EnglishLiveTutor.Infrastructure`, `src/EnglishLiveTutor.Api`, `src/EnglishLiveTutor.Application`)? Có file nào trong Catalog thực ra thuộc module khác hoặc Shared?
4. DI: `CatalogModuleServiceCollectionExtensions`, `CatalogInfrastructureServiceCollectionExtensions` đăng ký đủ, lifetime hợp lý; composition gọi đúng chỗ.
5. Namespace của file di chuyển giữ nguyên; migration `MoveToModuleSchema_Catalog` ở assembly Catalog.Infrastructure, `MigrationsAssembly` đúng.
6. Điểm nào của Catalog **không nên** làm mẫu cho module khác (sai lệch, trùng lặp, dead code).

Trả về danh sách finding `file:dòng` + mức độ (cao/trung bình/thấp) + đề xuất, và kết luận ĐẠT / CẦN SỬA. Kết thúc bằng phần "Worker report".
