# Permissions (Mã Quyền Hạn)

> Tài liệu này liệt kê toàn bộ các mã quyền (permissions) có trong hệ thống EnglishPartner. Các mã này được sử dụng trên FE để kiểm tra quyền hiển thị menu (AdminSidebar) hoặc kiểm soát các nút thao tác.
> 
> Nó tương ứng với file backend: `EnglishPartner.Modules.Identity.Domain.Roles.Permissions`

## Quy ước
- Định dạng: `module.resource.action`
- Các mã quyền được truyền xuống FE thông qua trường `permissions` (mảng string) trong JWT Access Token. FE có thể lấy qua `user.permissions` trong Zustand store.

---

## Danh sách Permissions

### 1. Curriculum (Quản lý Chương trình học)
| Mã quyền (Code) | Mô tả / Sử dụng cho màn hình nào |
|---|---|
| `curriculum.topics.write` | Quản lý Chủ đề (Thêm, sửa, xóa, duyệt Topic) |
| `curriculum.vocabulary.write` | Quản lý Từ vựng |
| `curriculum.grammar.write` | Quản lý Ngữ pháp |
| `curriculum.exercise.write` | Quản lý Bài tập |
| `curriculum.conversation.write` | Quản lý Hội thoại |
| `curriculum.sentence.write` | Quản lý Mẫu câu |
| `curriculum.generate` | Sử dụng AI để tự động tạo nội dung (Dịch, Sinh từ vựng, Ngữ pháp, Bài tập) |

### 2. Identity (Quản lý User & Phân quyền)
| Mã quyền (Code) | Mô tả / Sử dụng cho màn hình nào |
|---|---|
| `identity.users.manage` | Quản lý User (Xem danh sách, Khóa/Mở khóa tài khoản, Reset mật khẩu) |
| `identity.roles.manage` | Quản lý Role (Tạo, Sửa Role và Phân quyền cho User) |

### 3. Learning (Quản lý Quá trình học của User)
| Mã quyền (Code) | Mô tả / Sử dụng cho màn hình nào |
|---|---|
| `learning.admin.view` | Xem dữ liệu học tập của User trên trang Admin (Leaderboard, XP, Level, Lịch sử học) |
| `learning.admin.write` | Can thiệp vào dữ liệu học tập của User (Cộng/Trừ điểm XP, Đổi cấp độ thủ công) |

### 4. Notifications (Quản lý Thông báo)
| Mã quyền (Code) | Mô tả / Sử dụng cho màn hình nào |
|---|---|
| `notifications.admin` | Gửi Push Notifications hoặc Thông báo hệ thống thủ công cho User |

### 5. AI (Quản lý Log AI)
| Mã quyền (Code) | Mô tả / Sử dụng cho màn hình nào |
|---|---|
| `ai.logs.read` | Xem Log lịch sử gọi AI và Token đã sử dụng để tính chi phí |

---

## Ví dụ sử dụng ở Frontend (React/Zustand)

Sử dụng trong Menu / Component (giống `AdminSidebar.tsx`):
```tsx
const { user } = useAuthStore()
const userPermissions = user?.permissions || []

// Hàm kiểm tra
const hasPermission = (requiredPermission: string | string[]) => {
  if (Array.isArray(requiredPermission)) {
    return requiredPermission.some(p => userPermissions.includes(p))
  }
  return userPermissions.includes(requiredPermission)
}

// Render component
{hasPermission('curriculum.topics.write') && (
  <button>Thêm Topic mới</button>
)}
```
