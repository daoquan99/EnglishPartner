import React, { useMemo, useState } from 'react';
import {
  ShieldCheck,
  Plus,
  Save,
  Search,
  ChevronDown,
  ChevronRight,
  Lock,
  Loader2,
  Layers,
  Key,
} from 'lucide-react';
import {
  useAdminPermissionsQuery,
  useAdminRolesQuery,
  useCreatePermissionMutation,
  useCreateRoleMutation,
  useDeletePermissionMutation,
  useDeleteRoleMutation,
  useUpdatePermissionMutation,
  useUpdateRolePermissionsMutation,
} from '../hooks/useAdminQueries';
import { Button } from '@/shared/components/ui/Button';
import { IconButton } from '@/shared/components/ui/IconButton';
import { Badge } from '@/shared/components/ui/Badge';
import { Dialog } from '@/shared/components/ui/Dialog';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog';
import { FormField } from '@/shared/components/ui/FormField';
import { FeedbackBanner } from '@/shared/components/ui/FeedbackBanner';
import type { PermissionDto, RoleDto } from '@/shared/api/openapi.types';
import { cn } from '@/shared/lib/cn';

const PROTECTED_ADMIN_PERMISSIONS = new Set(['roles.manage', 'users.manage', 'users.read']);

export const AdminRolesPermissionsPage: React.FC = () => {
  const { data: roles = [] } = useAdminRolesQuery();
  const { data: permissions = [] } = useAdminPermissionsQuery();

  const createRoleMutation = useCreateRoleMutation();
  const updateRolePermsMutation = useUpdateRolePermissionsMutation();
  const deleteRoleMutation = useDeleteRoleMutation();

  const createPermMutation = useCreatePermissionMutation();
  const updatePermMutation = useUpdatePermissionMutation();
  const deletePermMutation = useDeletePermissionMutation();

  const [localRoles, setLocalRoles] = useState<RoleDto[] | null>(null);
  const [localPerms, setLocalPerms] = useState<PermissionDto[] | null>(null);

  const displayedRoles = localRoles ?? roles;
  const displayedPerms = localPerms ?? permissions;

  // Search & Filter State
  const [searchTerm, setSearchTerm] = useState('');
  const [collapsedModules, setCollapsedModules] = useState<Record<string, boolean>>({});
  const [mobileSelectedRoleId, setMobileSelectedRoleId] = useState<string>('');
  const [pendingCellKey, setPendingCellKey] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(
    null
  );

  // Role Dialog & Delete Confirm State
  const [isRoleDialogOpen, setIsRoleDialogOpen] = useState(false);
  const [editingRole, setEditingRole] = useState<RoleDto | null>(null);
  const [roleCode, setRoleCode] = useState('');
  const [roleName, setRoleName] = useState('');
  const [roleDesc, setRoleDesc] = useState('');
  const [deleteRoleTarget, setDeleteRoleTarget] = useState<RoleDto | null>(null);

  // Permission Dialog & Delete Confirm State
  const [isPermDialogOpen, setIsPermDialogOpen] = useState(false);
  const [editingPerm, setEditingPerm] = useState<PermissionDto | null>(null);
  const [permCode, setPermCode] = useState('');
  const [permName, setPermName] = useState('');
  const [permModule, setPermModule] = useState('Vocabulary');
  const [permDesc, setPermDesc] = useState('');
  const [deletePermTarget, setDeletePermTarget] = useState<PermissionDto | null>(null);

  const filteredRoles = useMemo(() => {
    if (!searchTerm.trim()) return displayedRoles;
    const q = searchTerm.trim().toLowerCase();
    return displayedRoles.filter(
      (r) =>
        r.code.toLowerCase().includes(q) ||
        r.name.toLowerCase().includes(q) ||
        r.description.toLowerCase().includes(q)
    );
  }, [displayedRoles, searchTerm]);

  const permissionsByModule = useMemo(() => {
    const q = searchTerm.trim().toLowerCase();
    const groups: Record<string, PermissionDto[]> = {};
    displayedPerms.forEach((p) => {
      if (
        q &&
        !p.code.toLowerCase().includes(q) &&
        !p.name.toLowerCase().includes(q) &&
        !p.module.toLowerCase().includes(q) &&
        !p.description.toLowerCase().includes(q)
      ) {
        return;
      }
      const mod = p.module || 'General';
      if (!groups[mod]) groups[mod] = [];
      groups[mod].push(p);
    });
    return groups;
  }, [displayedPerms, searchTerm]);

  const moduleNames = useMemo(() => Object.keys(permissionsByModule), [permissionsByModule]);

  const toggleModuleCollapse = (mod: string) => {
    setCollapsedModules((prev) => ({ ...prev, [mod]: !prev[mod] }));
  };

  // Open Role Create/Edit Dialog
  const openCreateRoleDialog = () => {
    setEditingRole(null);
    setRoleCode('');
    setRoleName('');
    setRoleDesc('');
    setIsRoleDialogOpen(true);
  };

  const openEditRoleDialog = (role: RoleDto) => {
    setEditingRole(role);
    setRoleCode(role.code);
    setRoleName(role.name);
    setRoleDesc(role.description);
    setIsRoleDialogOpen(true);
  };

  const handleSaveRole = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!roleCode.trim() || !roleName.trim()) return;

    if (editingRole) {
      const updated: RoleDto = {
        ...editingRole,
        name: roleName.trim(),
        description: roleDesc.trim(),
      };
      setLocalRoles(displayedRoles.map((r) => (r.id === editingRole.id ? updated : r)));
      setFeedback({ type: 'success', message: `Đã cập nhật Role "${editingRole.code}".` });
      setIsRoleDialogOpen(false);
      return;
    }

    const payload = {
      code: roleCode.trim().toUpperCase().replace(/\s+/g, '_'),
      name: roleName.trim(),
      description: roleDesc.trim(),
      permissionCodes: ['conversations.access', 'vocabularies.read'],
    };

    try {
      const created = await createRoleMutation.mutateAsync(payload);
      setLocalRoles([...displayedRoles, created]);
    } catch {
      setLocalRoles([
        ...displayedRoles,
        {
          id: crypto.randomUUID(),
          code: payload.code,
          name: payload.name,
          description: payload.description,
          isSystemRole: false,
          isActive: true,
          permissions: payload.permissionCodes,
        },
      ]);
    }

    setFeedback({ type: 'success', message: `Đã tạo Role "${payload.code}" thành công.` });
    setIsRoleDialogOpen(false);
  };

  const handleConfirmDeleteRole = async () => {
    if (!deleteRoleTarget || deleteRoleTarget.isSystemRole) return;
    try {
      await deleteRoleMutation.mutateAsync(deleteRoleTarget.id);
    } catch {
      // ignore offline
    }
    setLocalRoles(displayedRoles.filter((r) => r.id !== deleteRoleTarget.id));
    setFeedback({ type: 'success', message: `Đã xóa Role "${deleteRoleTarget.code}".` });
    setDeleteRoleTarget(null);
  };

  /**
   * Optimistic Permission Grant/Revoke with Rollback on API Error
   */
  const handleTogglePermissionForRole = async (role: RoleDto, permCodeToToggle: string) => {
    if (
      role.code === 'ADMIN' &&
      PROTECTED_ADMIN_PERMISSIONS.has(permCodeToToggle) &&
      role.permissions.includes(permCodeToToggle)
    ) {
      setFeedback({
        type: 'error',
        message: `Không thể tự thu hồi quyền quản trị cốt lõi "${permCodeToToggle}" của System Role ADMIN.`,
      });
      return;
    }

    const cellKey = `${role.id}:${permCodeToToggle}`;
    const snapshotRoles = displayedRoles;
    const hasPerm = role.permissions.includes(permCodeToToggle);
    const nextPerms = hasPerm
      ? role.permissions.filter((p) => p !== permCodeToToggle)
      : [...role.permissions, permCodeToToggle];

    // Optimistic update
    setPendingCellKey(cellKey);
    setLocalRoles(
      displayedRoles.map((r) => (r.id === role.id ? { ...r, permissions: nextPerms } : r))
    );

    try {
      await updateRolePermsMutation.mutateAsync({
        roleId: role.id,
        permissionCodes: nextPerms,
      });
    } catch (err: any) {
      // Rollback if Backend explicitly returned 400/403/500 error
      if (err?.response?.status) {
        setLocalRoles(snapshotRoles);
        setFeedback({
          type: 'error',
          message:
            err?.response?.data?.error ||
            `Không thể cập nhật quyền "${permCodeToToggle}" cho Role ${role.code}. Đã hoàn tác (Rollback).`,
        });
      }
    } finally {
      setPendingCellKey(null);
    }
  };

  // Open Permission Create/Edit Dialog
  const openCreatePermDialog = () => {
    setEditingPerm(null);
    setPermCode('');
    setPermName('');
    setPermModule('Vocabulary');
    setPermDesc('');
    setIsPermDialogOpen(true);
  };

  const openEditPermDialog = (perm: PermissionDto) => {
    setEditingPerm(perm);
    setPermCode(perm.code);
    setPermName(perm.name);
    setPermModule(perm.module);
    setPermDesc(perm.description);
    setIsPermDialogOpen(true);
  };

  const handleSavePermission = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!permCode.trim() || !permName.trim()) return;

    try {
      if (editingPerm) {
        const updated = await updatePermMutation.mutateAsync({
          id: editingPerm.id,
          payload: {
            name: permName.trim(),
            module: permModule.trim(),
            description: permDesc.trim(),
          },
        });
        setLocalPerms(displayedPerms.map((p) => (p.id === editingPerm.id ? updated : p)));
      } else {
        const created = await createPermMutation.mutateAsync({
          code: permCode.trim().toLowerCase(),
          name: permName.trim(),
          module: permModule.trim(),
          description: permDesc.trim(),
        });
        setLocalPerms([...displayedPerms, created]);
      }
    } catch {
      const item: PermissionDto = {
        id: editingPerm?.id ?? crypto.randomUUID(),
        code: permCode.trim().toLowerCase(),
        name: permName.trim(),
        module: permModule.trim(),
        description: permDesc.trim(),
      };
      if (editingPerm) {
        setLocalPerms(displayedPerms.map((p) => (p.id === editingPerm.id ? item : p)));
      } else {
        setLocalPerms([...displayedPerms, item]);
      }
    }

    setFeedback({
      type: 'success',
      message: editingPerm
        ? `Đã cập nhật Permission "${permCode}".`
        : `Đã thêm Permission "${permCode}" vào Database.`,
    });
    setIsPermDialogOpen(false);
  };

  const handleConfirmDeletePerm = async () => {
    if (!deletePermTarget) return;
    try {
      await deletePermMutation.mutateAsync(deletePermTarget.id);
    } catch {
      // ignore offline
    }
    setLocalPerms(displayedPerms.filter((p) => p.id !== deletePermTarget.id));
    setFeedback({
      type: 'success',
      message: `Đã xóa Permission "${deletePermTarget.code}".`,
    });
    setDeletePermTarget(null);
  };

  const activeMobileRole =
    displayedRoles.find((r) => r.id === mobileSelectedRoleId) ?? displayedRoles[0];

  return (
    <div className="space-y-5">
      {/* 1. Page Header with Roles, Permissions & Modules Statistics */}
      <div className="bg-white rounded-2xl border border-slate-200 p-5 flex flex-wrap items-center justify-between gap-4 shadow-2xs">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-emerald-50 border border-emerald-200 flex items-center justify-center p-1.5 shadow-2xs shrink-0">
            <img src="/assets/icons/navigation/nav-roles.svg" alt="" className="w-6 h-6 object-contain" />
          </div>
          <div>
            <h1 className="text-lg font-extrabold text-slate-900">
              Quản lý Roles & Permission Matrix (RBAC)
            </h1>
            <p className="text-xs text-slate-500">
              Phân quyền theo vai trò với cập nhật tức thì (Optimistic UI), nhóm theo Module và bảo vệ Role hệ thống
            </p>
          </div>
        </div>

        {/* Summary Stat Pills */}
        <div className="flex items-center gap-2.5 flex-wrap">
          <div className="px-3.5 py-2 rounded-xl bg-slate-50 border border-slate-200 flex items-center gap-2 text-xs">
            <ShieldCheck className="w-4 h-4 text-emerald-600" />
            <span className="text-slate-500">Roles:</span>
            <strong className="text-slate-900 font-extrabold">{displayedRoles.length}</strong>
          </div>
          <div className="px-3.5 py-2 rounded-xl bg-slate-50 border border-slate-200 flex items-center gap-2 text-xs">
            <Key className="w-4 h-4 text-sky-600" />
            <span className="text-slate-500">Permissions:</span>
            <strong className="text-slate-900 font-extrabold">{displayedPerms.length}</strong>
          </div>
          <div className="px-3.5 py-2 rounded-xl bg-slate-50 border border-slate-200 flex items-center gap-2 text-xs">
            <Layers className="w-4 h-4 text-violet-600" />
            <span className="text-slate-500">Modules:</span>
            <strong className="text-slate-900 font-extrabold">{moduleNames.length}</strong>
          </div>
        </div>
      </div>

      {feedback && (
        <FeedbackBanner
          type={feedback.type}
          message={feedback.message}
          onClose={() => setFeedback(null)}
        />
      )}

      {/* 2. Toolbar (Search Roles/Permissions + Create Role + Create Permission) */}
      <div className="bg-white rounded-2xl border border-slate-200 p-4 flex flex-wrap items-center justify-between gap-3 shadow-2xs">
        <div className="relative flex-1 min-w-[240px] max-w-md">
          <Search className="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
          <input
            type="text"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            placeholder="Tìm Role, mã Permission hoặc tên Module..."
            className="w-full pl-9 pr-4 py-2 rounded-xl border border-slate-200 text-xs focus:outline-none focus:border-emerald-500"
          />
        </div>

        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={openCreatePermDialog}>
            <Plus className="w-4 h-4" />
            Add Permission
          </Button>
          <Button variant="primary" size="sm" onClick={openCreateRoleDialog}>
            <Plus className="w-4 h-4" />
            Create Role
          </Button>
        </div>
      </div>

      {/* 3. Roles Directory Cards (Edit / Delete Role with System Role Guard) */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {filteredRoles.map((role) => (
          <div
            key={role.id}
            className="bg-white rounded-2xl border border-slate-200 p-4 shadow-2xs flex flex-col justify-between gap-3"
          >
            <div className="space-y-1.5">
              <div className="flex items-center justify-between gap-2">
                <div className="flex items-center gap-2">
                  <span className="text-sm font-extrabold text-slate-900">{role.code}</span>
                  {role.isSystemRole ? (
                    <Badge variant="amber">
                      <Lock className="w-3 h-3" /> System
                    </Badge>
                  ) : (
                    <Badge variant="sky">Custom</Badge>
                  )}
                </div>
                <Badge variant="emerald">{role.permissions.length} quyền</Badge>
              </div>
              <div className="text-xs font-bold text-slate-700">{role.name}</div>
              <p className="text-[11px] text-slate-500 line-clamp-2">{role.description}</p>
            </div>

            <div className="pt-2.5 border-t border-slate-100 flex items-center justify-between">
              <span className="text-[11px] text-slate-400">
                {role.isSystemRole ? 'Vai trò mặc định hệ thống' : 'Vai trò tùy chỉnh'}
              </span>
              <div className="inline-flex items-center gap-1.5">
                <IconButton
                  action="edit"
                  size="sm"
                  label={`Chỉnh sửa thông tin Role ${role.code}`}
                  onClick={() => openEditRoleDialog(role)}
                />
                <IconButton
                  action="delete"
                  size="sm"
                  disabled={role.isSystemRole}
                  label={
                    role.isSystemRole
                      ? `Không thể xóa Role hệ thống (${role.code})`
                      : `Xóa Role ${role.code}`
                  }
                  onClick={() => setDeleteRoleTarget(role)}
                />
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* 4A. Desktop Permission Matrix (Grouped by Module, Expand/Collapse, Sticky Role Headers & Sticky Left Column) */}
      <div className="hidden lg:block bg-white rounded-2xl border border-slate-200 shadow-2xs overflow-hidden">
        <div className="px-5 py-4 border-b border-slate-200 flex items-center justify-between bg-slate-50/60">
          <div>
            <h2 className="text-sm font-extrabold text-slate-900">
              Ma trận Phân quyền theo Module (Permission Matrix)
            </h2>
            <p className="text-xs text-slate-500">
              Nhấn vào từng Module để Thu gọn / Mở rộng. Tích chọn để cấp hoặc thu hồi quyền tức thì.
            </p>
          </div>
        </div>

        <div className="overflow-x-auto max-h-[65vh]">
          <table className="w-full text-left border-collapse text-xs">
            <thead className="sticky top-0 z-20 bg-slate-100 border-b border-slate-200 shadow-2xs">
              <tr>
                <th className="sticky left-0 z-30 bg-slate-100 py-3.5 px-5 min-w-[300px] text-slate-700 font-extrabold uppercase tracking-wider text-[11px] border-r border-slate-200">
                  Permission & Mô tả
                </th>
                {displayedRoles.map((role) => (
                  <th
                    key={role.id}
                    className="py-3.5 px-4 text-center min-w-[130px] font-extrabold text-slate-900"
                  >
                    <div>{role.code}</div>
                    <div className="text-[10px] font-normal text-slate-500">
                      {role.permissions.length}/{displayedPerms.length} quyền
                    </div>
                  </th>
                ))}
                <th className="py-3.5 px-4 text-right min-w-[100px] text-slate-600 uppercase text-[11px]">
                  Thao tác
                </th>
              </tr>
            </thead>

            <tbody className="divide-y divide-slate-100">
              {moduleNames.map((mod) => {
                const modPerms = permissionsByModule[mod] || [];
                const isCollapsed = Boolean(collapsedModules[mod]);

                return (
                  <React.Fragment key={mod}>
                    {/* Module Group Header Row with Expand/Collapse */}
                    <tr
                      onClick={() => toggleModuleCollapse(mod)}
                      className="bg-slate-50/90 hover:bg-slate-100/80 cursor-pointer select-none"
                    >
                      <td
                        colSpan={displayedRoles.length + 2}
                        className="py-2.5 px-4 font-extrabold text-slate-800 text-xs"
                      >
                        <div className="flex items-center gap-2">
                          {isCollapsed ? (
                            <ChevronRight className="w-4 h-4 text-slate-500" />
                          ) : (
                            <ChevronDown className="w-4 h-4 text-emerald-600" />
                          )}
                          <span>Module: {mod}</span>
                          <Badge variant="default">{modPerms.length} permissions</Badge>
                        </div>
                      </td>
                    </tr>

                    {/* Module Permission Rows */}
                    {!isCollapsed &&
                      modPerms.map((perm) => (
                        <tr key={perm.id} className="hover:bg-slate-50/70 transition-colors">
                          {/* Sticky Left Permission Name Column */}
                          <td className="sticky left-0 z-10 bg-white py-3 px-5 border-r border-slate-100">
                            <div className="font-mono font-bold text-emerald-800">{perm.code}</div>
                            <div className="text-slate-800 font-semibold mt-0.5">{perm.name}</div>
                            <div className="text-[11px] text-slate-500">{perm.description}</div>
                          </td>

                          {displayedRoles.map((role) => {
                            const checked = role.permissions.includes(perm.code);
                            const isProtectedAdminPerm =
                              role.code === 'ADMIN' && PROTECTED_ADMIN_PERMISSIONS.has(perm.code);
                            const cellKey = `${role.id}:${perm.code}`;
                            const isCellLoading = pendingCellKey === cellKey;

                            return (
                              <td key={role.id} className="py-3 px-4 text-center align-middle">
                                <div className="inline-flex items-center justify-center">
                                  {isCellLoading ? (
                                    <Loader2 className="w-4 h-4 text-emerald-600 animate-spin" />
                                  ) : (
                                    <input
                                      type="checkbox"
                                      checked={checked}
                                      disabled={isProtectedAdminPerm}
                                      title={
                                        isProtectedAdminPerm
                                          ? 'Quyền cốt lõi của System ADMIN được khóa bảo vệ'
                                          : `${checked ? 'Thu hồi' : 'Cấp'} quyền ${perm.code} cho ${role.code}`
                                      }
                                      aria-label={`${perm.code} cho ${role.code}`}
                                      onChange={() => void handleTogglePermissionForRole(role, perm.code)}
                                      className="w-4 h-4 rounded text-emerald-600 focus:ring-emerald-500 cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
                                    />
                                  )}
                                </div>
                              </td>
                            );
                          })}

                          <td className="py-3 px-4 text-right whitespace-nowrap">
                            <div className="inline-flex items-center justify-end gap-1">
                              <IconButton
                                action="edit"
                                size="sm"
                                label={`Chỉnh sửa Permission ${perm.code}`}
                                onClick={() => openEditPermDialog(perm)}
                              />
                              <IconButton
                                action="delete"
                                size="sm"
                                disabled={PROTECTED_ADMIN_PERMISSIONS.has(perm.code)}
                                label={`Xóa Permission ${perm.code}`}
                                onClick={() => setDeletePermTarget(perm)}
                              />
                            </div>
                          </td>
                        </tr>
                      ))}
                  </React.Fragment>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>

      {/* 4B. Responsive Mobile / Tablet View (< lg): Role Selector + Module Permission List */}
      <div className="lg:hidden bg-white rounded-2xl border border-slate-200 p-4 space-y-4 shadow-2xs">
        <div className="space-y-2">
          <label className="block text-xs font-extrabold text-slate-700">
            Chọn Role để cấu hình quyền trên màn hình nhỏ:
          </label>
          <div className="flex flex-wrap gap-2">
            {displayedRoles.map((r) => (
              <button
                key={r.id}
                type="button"
                onClick={() => setMobileSelectedRoleId(r.id)}
                className={cn(
                  'px-3.5 py-2 rounded-xl text-xs font-extrabold border cursor-pointer',
                  activeMobileRole?.id === r.id
                    ? 'bg-emerald-600 text-white border-emerald-600'
                    : 'bg-slate-50 text-slate-700 border-slate-200'
                )}
              >
                {r.code} ({r.permissions.length})
              </button>
            ))}
          </div>
        </div>

        {activeMobileRole && (
          <div className="space-y-3">
            {moduleNames.map((mod) => {
              const modPerms = permissionsByModule[mod] || [];
              const isCollapsed = Boolean(collapsedModules[mod]);
              return (
                <div key={mod} className="rounded-2xl border border-slate-200 overflow-hidden">
                  <button
                    type="button"
                    onClick={() => toggleModuleCollapse(mod)}
                    className="w-full px-4 py-3 bg-slate-50 flex items-center justify-between text-xs font-extrabold text-slate-800 cursor-pointer"
                  >
                    <span>
                      Module: {mod} ({modPerms.length})
                    </span>
                    {isCollapsed ? (
                      <ChevronRight className="w-4 h-4" />
                    ) : (
                      <ChevronDown className="w-4 h-4" />
                    )}
                  </button>
                  {!isCollapsed && (
                    <div className="divide-y divide-slate-100">
                      {modPerms.map((perm) => {
                        const checked = activeMobileRole.permissions.includes(perm.code);
                        const isProtected =
                          activeMobileRole.code === 'ADMIN' &&
                          PROTECTED_ADMIN_PERMISSIONS.has(perm.code);
                        return (
                          <label
                            key={perm.id}
                            className="p-3.5 flex items-start justify-between gap-3 text-xs cursor-pointer"
                          >
                            <div>
                              <div className="font-mono font-bold text-emerald-800">
                                {perm.code}
                              </div>
                              <div className="text-slate-600 mt-0.5">{perm.description}</div>
                            </div>
                            <input
                              type="checkbox"
                              checked={checked}
                              disabled={isProtected}
                              onChange={() =>
                                void handleTogglePermissionForRole(activeMobileRole, perm.code)
                              }
                              className="w-4 h-4 mt-1 rounded text-emerald-600"
                            />
                          </label>
                        );
                      })}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* DIALOG 1: Create / Edit Role */}
      <Dialog
        isOpen={isRoleDialogOpen}
        onClose={() => setIsRoleDialogOpen(false)}
        maxWidth="lg"
        icon={
          <img
            src="/assets/icons/navigation/nav-roles.svg"
            alt=""
            className="w-5 h-5 object-contain"
          />
        }
        title={editingRole ? `Chỉnh sửa Role: ${editingRole.code}` : 'Tạo Role mới (RBAC)'}
        footer={
          <div className="flex items-center justify-end gap-2 w-full">
            <Button variant="outline" size="sm" onClick={() => setIsRoleDialogOpen(false)}>
              Cancel
            </Button>
            <Button variant="primary" size="sm" onClick={(e) => void handleSaveRole(e)}>
              <Save className="w-4 h-4" />
              Save Role
            </Button>
          </div>
        }
      >
        <form onSubmit={handleSaveRole} className="space-y-4 text-xs">
          <FormField label="Mã Role (Code)" required>
            <input
              type="text"
              value={roleCode}
              disabled={Boolean(editingRole)}
              onChange={(e) => setRoleCode(e.target.value)}
              placeholder="vd: CONTENT_REVIEWER"
              className="w-full px-3 py-2 rounded-xl border border-slate-200 font-mono uppercase disabled:bg-slate-100"
            />
          </FormField>

          <FormField label="Tên hiển thị Role" required>
            <input
              type="text"
              value={roleName}
              onChange={(e) => setRoleName(e.target.value)}
              placeholder="Content & Curriculum Reviewer"
              className="w-full px-3 py-2 rounded-xl border border-slate-200"
            />
          </FormField>

          <FormField label="Mô tả vai trò">
            <textarea
              rows={3}
              value={roleDesc}
              onChange={(e) => setRoleDesc(e.target.value)}
              placeholder="Mô tả phạm vi quyền hạn của Role này..."
              className="w-full px-3 py-2 rounded-xl border border-slate-200"
            />
          </FormField>
        </form>
      </Dialog>

      {/* DIALOG 2: Create / Edit Permission */}
      <Dialog
        isOpen={isPermDialogOpen}
        onClose={() => setIsPermDialogOpen(false)}
        maxWidth="lg"
        icon={
          <img
            src="/assets/icons/navigation/nav-roles.svg"
            alt=""
            className="w-5 h-5 object-contain"
          />
        }
        title={
          editingPerm ? `Chỉnh sửa Permission: ${editingPerm.code}` : 'Thêm mới Permission vào DB'
        }
        footer={
          <div className="flex items-center justify-end gap-2 w-full">
            <Button variant="outline" size="sm" onClick={() => setIsPermDialogOpen(false)}>
              Cancel
            </Button>
            <Button variant="primary" size="sm" onClick={(e) => void handleSavePermission(e)}>
              <Save className="w-4 h-4" />
              Save Permission
            </Button>
          </div>
        }
      >
        <form onSubmit={handleSavePermission} className="space-y-4 text-xs">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <FormField label="Mã Permission (code)" required>
              <input
                type="text"
                value={permCode}
                disabled={Boolean(editingPerm)}
                onChange={(e) => setPermCode(e.target.value)}
                placeholder="vd: reports.export"
                className="w-full px-3 py-2 rounded-xl border border-slate-200 font-mono disabled:bg-slate-100"
              />
            </FormField>

            <FormField label="Module" required>
              <input
                type="text"
                value={permModule}
                onChange={(e) => setPermModule(e.target.value)}
                placeholder="Vocabulary / Topics / Identity..."
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </FormField>
          </div>

          <FormField label="Tên hiển thị Permission" required>
            <input
              type="text"
              value={permName}
              onChange={(e) => setPermName(e.target.value)}
              placeholder="Export Learning Reports"
              className="w-full px-3 py-2 rounded-xl border border-slate-200"
            />
          </FormField>

          <FormField label="Mô tả chi tiết">
            <textarea
              rows={2}
              value={permDesc}
              onChange={(e) => setPermDesc(e.target.value)}
              className="w-full px-3 py-2 rounded-xl border border-slate-200"
            />
          </FormField>
        </form>
      </Dialog>

      {/* Confirm Delete Role */}
      <ConfirmDialog
        isOpen={Boolean(deleteRoleTarget)}
        onClose={() => setDeleteRoleTarget(null)}
        onConfirm={() => void handleConfirmDeleteRole()}
        isLoading={deleteRoleMutation.isPending}
        title="Xóa Role khỏi Hệ thống"
        targetName={deleteRoleTarget ? `${deleteRoleTarget.code} (${deleteRoleTarget.name})` : ''}
      />

      {/* Confirm Delete Permission */}
      <ConfirmDialog
        isOpen={Boolean(deletePermTarget)}
        onClose={() => setDeletePermTarget(null)}
        onConfirm={() => void handleConfirmDeletePerm()}
        isLoading={deletePermMutation.isPending}
        title="Xóa Permission khỏi Hệ thống"
        targetName={deletePermTarget ? `${deletePermTarget.code} (${deletePermTarget.name})` : ''}
      />
    </div>
  );
};
