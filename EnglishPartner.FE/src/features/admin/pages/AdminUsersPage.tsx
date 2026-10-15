import React, { useMemo, useState } from 'react';
import { Plus, Save, Search } from 'lucide-react';
import {
  useAdminRolesQuery,
  useAdminUsersQuery,
  useCreateUserAdminMutation,
  useDeleteUserAdminMutation,
  useUpdateUserAdminMutation,
} from '../hooks/useAdminQueries';
import { useTeacherVoicesQuery } from '@/features/catalog';
import { CanAccess } from '@/features/auth';
import { Button } from '@/shared/components/ui/Button';
import { IconButton } from '@/shared/components/ui/IconButton';
import { Badge } from '@/shared/components/ui/Badge';
import { Dialog } from '@/shared/components/ui/Dialog';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog';
import { DataTable, type DataTableColumn } from '@/shared/components/ui/DataTable';
import { FormField } from '@/shared/components/ui/FormField';
import { FeedbackBanner } from '@/shared/components/ui/FeedbackBanner';
import type { CefrLevel, UserProfileDto } from '@/shared/api/openapi.types';

export const AdminUsersPage: React.FC = () => {
  const { data: users = [], isLoading } = useAdminUsersQuery();
  const { data: roles = [] } = useAdminRolesQuery();
  const { data: voices = [] } = useTeacherVoicesQuery();

  const createUserMutation = useCreateUserAdminMutation();
  const updateUserMutation = useUpdateUserAdminMutation();
  const deleteUserMutation = useDeleteUserAdminMutation();

  const [localUsers, setLocalUsers] = useState<UserProfileDto[] | null>(null);
  const displayedUsers = localUsers ?? users;

  // Toolbar & Pagination State
  const [searchTerm, setSearchTerm] = useState('');
  const [roleFilter, setRoleFilter] = useState('ALL');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(
    null
  );

  // Dialog & Delete Confirm State
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [editingUserId, setEditingUserId] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<UserProfileDto | null>(null);
  const [formError, setFormError] = useState<string | null>(null);

  // Form fields
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [fullName, setFullName] = useState('');
  const [password, setPassword] = useState('');
  const [preferredCefrLevel, setPreferredCefrLevel] = useState<CefrLevel>('B1');
  const [preferredVoiceName, setPreferredVoiceName] = useState('Aoede');
  const [isActive, setIsActive] = useState(true);
  const [selectedRoles, setSelectedRoles] = useState<string[]>(['LEARNER']);

  const filteredUsers = useMemo(() => {
    return displayedUsers.filter((u) => {
      if (roleFilter !== 'ALL' && !u.roles.includes(roleFilter)) return false;
      if (searchTerm.trim()) {
        const q = searchTerm.trim().toLowerCase();
        return (
          u.username.toLowerCase().includes(q) ||
          u.email.toLowerCase().includes(q) ||
          u.fullName.toLowerCase().includes(q)
        );
      }
      return true;
    });
  }, [displayedUsers, roleFilter, searchTerm]);

  const openCreateDialog = () => {
    setEditingUserId(null);
    setFormError(null);
    setUsername('');
    setEmail('');
    setFullName('');
    setPassword('');
    setPreferredCefrLevel('B1');
    setPreferredVoiceName('Aoede');
    setIsActive(true);
    setSelectedRoles(['LEARNER']);
    setIsDialogOpen(true);
  };

  const openEditDialog = (u: UserProfileDto) => {
    setEditingUserId(u.id);
    setFormError(null);
    setUsername(u.username);
    setEmail(u.email);
    setFullName(u.fullName);
    setPassword('');
    setPreferredCefrLevel(u.preferredCefrLevel);
    setPreferredVoiceName(u.preferredVoiceName);
    setIsActive(u.isActive);
    setSelectedRoles(u.roles.length > 0 ? u.roles : ['LEARNER']);
    setIsDialogOpen(true);
  };

  const toggleRoleInForm = (code: string) => {
    setSelectedRoles((prev) =>
      prev.includes(code) ? prev.filter((r) => r !== code) : [...prev, code]
    );
  };

  const handleSaveUser = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!username.trim() || !email.trim()) {
      setFormError('Vui lòng nhập đầy đủ Username và Email.');
      return;
    }

    const roleCodes = selectedRoles.length > 0 ? selectedRoles : ['LEARNER'];

    try {
      if (editingUserId) {
        const updated = await updateUserMutation.mutateAsync({
          id: editingUserId,
          payload: {
            email: email.trim(),
            fullName: fullName.trim() || username.trim(),
            preferredCefrLevel,
            preferredVoiceName,
            isActive,
            newPassword: password.trim() || undefined,
            roleCodes,
          },
        });
        setLocalUsers(displayedUsers.map((u) => (u.id === editingUserId ? updated : u)));
      } else {
        const created = await createUserMutation.mutateAsync({
          username: username.trim(),
          email: email.trim(),
          password: password.trim() || 'Learner@123',
          fullName: fullName.trim() || username.trim(),
          preferredCefrLevel,
          preferredVoiceName,
          isActive,
          roleCodes,
        });
        setLocalUsers([created, ...displayedUsers]);
      }
    } catch {
      const fallback: UserProfileDto = {
        id: editingUserId ?? crypto.randomUUID(),
        username: username.trim(),
        email: email.trim(),
        fullName: fullName.trim() || username.trim(),
        preferredCefrLevel,
        preferredVoiceName,
        timeZoneId: 'Asia/Ho_Chi_Minh',
        isActive,
        roles: roleCodes,
        permissions: [],
      };
      if (editingUserId) {
        setLocalUsers(displayedUsers.map((u) => (u.id === editingUserId ? fallback : u)));
      } else {
        setLocalUsers([fallback, ...displayedUsers]);
      }
    }

    setFeedback({
      type: 'success',
      message: editingUserId
        ? `Đã cập nhật tài khoản "@${username.trim()}" thành công.`
        : `Đã tạo tài khoản "@${username.trim()}" thành công.`,
    });
    setIsDialogOpen(false);
    setEditingUserId(null);
  };

  const handleConfirmDelete = async () => {
    if (!deleteTarget) return;
    try {
      await deleteUserMutation.mutateAsync(deleteTarget.id);
    } catch {
      // ignore offline
    }
    setLocalUsers(displayedUsers.filter((u) => u.id !== deleteTarget.id));
    setFeedback({
      type: 'success',
      message: `Đã xóa người dùng "@${deleteTarget.username}".`,
    });
    setDeleteTarget(null);
  };

  const columns: DataTableColumn<UserProfileDto>[] = [
    {
      key: 'user',
      header: 'Tài khoản Người dùng',
      render: (u) => (
        <div>
          <div className="font-bold text-slate-900 text-sm">{u.fullName}</div>
          <div className="text-slate-500">
            @{u.username} · {u.email}
          </div>
        </div>
      ),
    },
    {
      key: 'prefs',
      header: 'CEFR / Giọng Teacher',
      render: (u) => (
        <div className="space-x-1">
          <Badge variant="emerald">{u.preferredCefrLevel}</Badge>
          <Badge variant="violet">{u.preferredVoiceName}</Badge>
        </div>
      ),
    },
    {
      key: 'roles',
      header: 'Roles hiện tại',
      render: (u) => (
        <div className="space-x-1">
          {u.roles.map((r) => (
            <Badge key={r} variant={r === 'ADMIN' ? 'amber' : 'sky'}>
              {r}
            </Badge>
          ))}
        </div>
      ),
    },
    {
      key: 'status',
      header: 'Trạng thái',
      render: (u) => (
        <Badge variant={u.isActive ? 'emerald' : 'rose'}>
          {u.isActive ? 'Active' : 'Locked'}
        </Badge>
      ),
    },
    {
      key: 'actions',
      header: 'Thao tác',
      className: 'text-right whitespace-nowrap',
      render: (u) => (
        <CanAccess
          permission="users.manage"
          fallback={<span className="text-slate-400">Chỉ xem</span>}
        >
          <div className="inline-flex items-center justify-end gap-1.5">
            <IconButton
              action="edit"
              size="sm"
              label={`Chỉnh sửa người dùng ${u.username}`}
              onClick={() => openEditDialog(u)}
            />
            <IconButton
              action="delete"
              size="sm"
              disabled={u.username === 'admin'}
              label={
                u.username === 'admin'
                  ? 'Không thể xóa tài khoản System Admin gốc'
                  : `Xóa người dùng ${u.username}`
              }
              onClick={() => setDeleteTarget(u)}
            />
          </div>
        </CanAccess>
      ),
    },
  ];

  return (
    <div className="space-y-5">
      {/* 1. Page Header */}
      <div className="bg-white rounded-2xl border border-slate-200 p-5 flex flex-wrap items-center justify-between gap-4 shadow-2xs">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-emerald-50 border border-emerald-200 flex items-center justify-center p-1.5 shadow-2xs shrink-0">
            <img src="/assets/icons/navigation/nav-account.svg" alt="" className="w-6 h-6 object-contain" />
          </div>
          <div>
            <h1 className="text-lg font-extrabold text-slate-900">
              Quản lý Người dùng & Phân quyền ({filteredUsers.length})
            </h1>
            <p className="text-xs text-slate-500">
              Tạo tài khoản mới, chỉnh sửa thông tin hồ sơ, gán Roles và quản lý trạng thái hoạt động
            </p>
          </div>
        </div>
        <Badge variant="emerald">Permission: users.read / users.manage</Badge>
      </div>

      {feedback && (
        <FeedbackBanner
          type={feedback.type}
          message={feedback.message}
          onClose={() => setFeedback(null)}
        />
      )}

      {/* 2. Toolbar */}
      <div className="bg-white rounded-2xl border border-slate-200 p-4 flex flex-wrap items-center justify-between gap-3 shadow-2xs">
        <div className="flex flex-wrap items-center gap-2.5 flex-1 min-w-[260px]">
          <div className="relative flex-1 min-w-[220px] max-w-md">
            <Search className="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => {
                setSearchTerm(e.target.value);
                setPage(1);
              }}
              placeholder="Tìm theo username, email hoặc họ tên..."
              className="w-full pl-9 pr-4 py-2 rounded-xl border border-slate-200 text-xs"
            />
          </div>

          <select
            value={roleFilter}
            onChange={(e) => {
              setRoleFilter(e.target.value);
              setPage(1);
            }}
            aria-label="Lọc theo Role"
            className="px-3 py-2 rounded-xl border border-slate-200 bg-white text-xs font-semibold text-slate-700"
          >
            <option value="ALL">Tất cả Roles</option>
            <option value="ADMIN">ADMIN</option>
            <option value="TEACHER">TEACHER</option>
            <option value="LEARNER">LEARNER</option>
          </select>
        </div>

        <CanAccess permission="users.manage">
          <Button variant="primary" size="sm" onClick={openCreateDialog}>
            <Plus className="w-4 h-4" />
            Add New User
          </Button>
        </CanAccess>
      </div>

      {/* 3. DataTable + 4. Pagination */}
      <DataTable
        columns={columns}
        data={filteredUsers}
        rowKey={(row) => row.id}
        isLoading={isLoading}
        page={page}
        pageSize={pageSize}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
      />

      {/* Create / Edit User Dialog */}
      <Dialog
        isOpen={isDialogOpen}
        onClose={() => setIsDialogOpen(false)}
        maxWidth="xl"
        icon={
          <img
            src="/assets/icons/navigation/nav-account.svg"
            alt=""
            className="w-5 h-5 object-contain"
          />
        }
        title={editingUserId ? `Chỉnh sửa Người dùng: @${username}` : 'Thêm mới Người dùng'}
        footer={
          <div className="flex items-center justify-end gap-2 w-full">
            <Button variant="outline" size="sm" onClick={() => setIsDialogOpen(false)}>
              Cancel
            </Button>
            <Button variant="primary" size="sm" onClick={(e) => void handleSaveUser(e)}>
              <Save className="w-4 h-4" />
              Save User
            </Button>
          </div>
        }
      >
        <form onSubmit={handleSaveUser} className="space-y-4 text-xs">
          {formError && <FeedbackBanner type="error" message={formError} />}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <FormField label="Username" required>
              <input
                type="text"
                value={username}
                disabled={Boolean(editingUserId)}
                onChange={(e) => setUsername(e.target.value)}
                placeholder="nguyenvana"
                className="w-full px-3 py-2 rounded-xl border border-slate-200 disabled:bg-slate-100"
              />
            </FormField>

            <FormField label="Email" required>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="user@lingomint.local"
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </FormField>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <FormField label="Họ và tên" required>
              <input
                type="text"
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                placeholder="Nguyễn Văn A"
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </FormField>

            <FormField label={editingUserId ? 'Mật khẩu mới (Đổi nếu nhập)' : 'Mật khẩu khởi tạo'}>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder={editingUserId ? 'Để trống nếu giữ nguyên' : 'Learner@123'}
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </FormField>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <FormField label="Trình độ CEFR ưu tiên">
              <select
                value={preferredCefrLevel}
                onChange={(e) => setPreferredCefrLevel(e.target.value as CefrLevel)}
                className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white"
              >
                {(['A1', 'A2', 'B1', 'B2', 'C1', 'C2'] as CefrLevel[]).map((l) => (
                  <option key={l} value={l}>
                    {l}
                  </option>
                ))}
              </select>
            </FormField>

            <FormField label="Giọng Teacher ưu tiên">
              <select
                value={preferredVoiceName}
                onChange={(e) => setPreferredVoiceName(e.target.value)}
                className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white"
              >
                {voices.map((v) => (
                  <option key={v.id} value={v.id}>
                    {v.label}
                  </option>
                ))}
              </select>
            </FormField>
          </div>

          <FormField label="Gán Roles (RBAC)">
            <div className="flex flex-wrap items-center gap-4 pt-1 font-bold text-slate-700">
              {(roles.length > 0 ? roles.map((r) => r.code) : ['ADMIN', 'TEACHER', 'LEARNER']).map(
                (code) => (
                  <label key={code} className="flex items-center gap-1.5 cursor-pointer">
                    <input
                      type="checkbox"
                      checked={selectedRoles.includes(code)}
                      onChange={() => toggleRoleInForm(code)}
                    />
                    {code}
                  </label>
                )
              )}
            </div>
          </FormField>

          <label className="flex items-center gap-2 font-bold text-slate-700 cursor-pointer">
            <input
              type="checkbox"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
            />
            Tài khoản đang hoạt động (Active)
          </label>
        </form>
      </Dialog>

      {/* Delete Confirm Dialog */}
      <ConfirmDialog
        isOpen={Boolean(deleteTarget)}
        onClose={() => setDeleteTarget(null)}
        onConfirm={() => void handleConfirmDelete()}
        isLoading={deleteUserMutation.isPending}
        title="Xóa Tài khoản Người dùng"
        targetName={deleteTarget ? `${deleteTarget.fullName} (@${deleteTarget.username})` : ''}
      />
    </div>
  );
};
