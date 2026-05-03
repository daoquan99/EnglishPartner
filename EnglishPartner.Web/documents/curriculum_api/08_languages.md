# Curriculum API — Languages

> **Mục tiêu:** Cung cấp danh sách các ngôn ngữ được hỗ trợ trong hệ thống (để dùng cho dropdown lúc user chọn Native Language / Target Language).

---

## 1. Lấy danh sách ngôn ngữ
`GET /api/v1/curriculum/languages`

**Quyền yêu cầu:** Người dùng đã đăng nhập (Authenticated).

**Response 200 OK:**
```json
[
  {
    "id": "guid",
    "code": "en",
    "name": "English",
    "nativeName": "English",
    "sortOrder": 1
  },
  {
    "id": "guid",
    "code": "vi",
    "name": "Vietnamese",
    "nativeName": "Tiếng Việt",
    "sortOrder": 2
  }
]
```

### Note:
- Dữ liệu được trả về theo dạng mảng phẳng.
- Danh sách đã được sắp xếp ưu tiên theo trường `sortOrder` tăng dần, sau đó theo `name`.

---

## 2. Thêm ngôn ngữ mới
`POST /api/v1/curriculum/languages`

**Quyền yêu cầu:** Admin (`curriculum.topics.write`).

**Body:**
```json
{
  "code": "ja",
  "name": "Japanese",
  "nativeName": "日本語",
  "sortOrder": 3
}
```

**Response 201 Created:**
```json
{
  "id": "guid"
}
```

---

## 3. Cập nhật ngôn ngữ
`PUT /api/v1/curriculum/languages/{id}`

**Quyền yêu cầu:** Admin (`curriculum.topics.write`).

**Body:**
```json
{
  "name": "Japanese Updated",
  "nativeName": "日本語",
  "sortOrder": 4
}
```

**Response 204 NoContent**

---

## 4. Xóa ngôn ngữ
`DELETE /api/v1/curriculum/languages/{id}`

**Quyền yêu cầu:** Admin (`curriculum.topics.write`).

**Response 204 NoContent**
