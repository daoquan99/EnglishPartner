---
trigger: manual
---

# Curriculum — Languages

> **Scope:** Add, Update, Delete, Get supported Languages.
> **@mention khi làm:** Language, Curriculum, Language API

---

## 1. Mục đích
- Quản lý danh sách các ngôn ngữ được hỗ trợ trong hệ thống.
- Cung cấp dữ liệu cho module `Identity` (qua `ILanguageQueryService`) để sử dụng trong màn hình Onboarding (chọn Native Language, Target Language).
- Hỗ trợ đầy đủ CRUD cho trang Admin quản lý ngôn ngữ.

## 2. Entity & Aggregates
**Language (`Domain/Languages/Language.cs`)**
- `Code`: Mã ngôn ngữ (ví dụ `en`, `vi`, `ja`). Thường theo chuẩn ISO 639-1. Được lưu ở dạng lowercase.
- `Name`: Tên tiếng Anh của ngôn ngữ (ví dụ `English`, `Vietnamese`).
- `NativeName`: Tên bản địa của ngôn ngữ (ví dụ `English`, `Tiếng Việt`, `日本語`).
- `SortOrder`: Số thứ tự để sắp xếp danh sách hiển thị trên UI.

## 3. Endpoints (CRUD)
- `GET /api/v1/curriculum/languages` — Authenticated (Ai cũng gọi được, phục vụ Dropdown)
- `POST /api/v1/curriculum/languages` — Require `curriculum.topics.write`
- `PUT /api/v1/curriculum/languages/{id}` — Require `curriculum.topics.write`
- `DELETE /api/v1/curriculum/languages/{id}` — Require `curriculum.topics.write`

*(Lưu ý: CRUD API hiện đang dùng tạm claim `curriculum.topics.write`, có thể sau này tách claim riêng nếu cần)*

## 4. Cross-Module Service
- Service: `ILanguageQueryService` (tại `Curriculum.Contracts`).
- Implementation: `LanguageQueryService` (tại `Curriculum.Infrastructure`).
- Consumed by: `Identity.Api` (`OnboardingEndpoints.cs`) để trả về options cho list questions.

## 5. Dữ liệu mặc định (Seeder)
- Module đã có `LanguageSeeder` chạy lúc app khởi động để đảm bảo các ngôn ngữ chính (English, Vietnamese, Japanese, Korean, Chinese) luôn có sẵn.
