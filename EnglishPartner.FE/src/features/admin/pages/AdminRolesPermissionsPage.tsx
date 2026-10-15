import React from 'react';
import { ShieldCheck } from 'lucide-react';
import { useAdminPermissionsQuery, useAdminRolesQuery } from '../hooks/useAdminQueries';
import { Badge } from '@/shared/components/ui/Badge';

export const AdminRolesPermissionsPage: React.FC = () => {
  const { data: roles = [] } = useAdminRolesQuery();
  const { data: permissions = [] } = useAdminPermissionsQuery();

  return (
    <div className="space-y-6">
      <div className="bg-white rounded-3xl border border-slate-200 p-6 flex items-center justify-between">
        <div>
          <h1 className="text-lg font-extrabold text-slate-900 flex items-center gap-2">
            <ShieldCheck className="w-5 h-5 text-emerald-600" />
            Ma trận Roles & Permissions (`RBAC`)
          </h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Đồng bộ giữa Frontend Guards (`RequirePermission`, `CanAccess`, `AdminSidebar`) và Backend `.RequirePermission(...)`.
          </p>
        </div>
        <Badge variant="emerald">Permission: roles.manage</Badge>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {roles.map((role) => (
          <div
            key={role.id}
            className="bg-white rounded-3xl border border-slate-200 p-5 space-y-3"
          >
            <div className="flex items-center justify-between">
              <span className="text-base font-extrabold text-slate-900">{role.code}</span>
              <Badge variant="emerald">{role.permissions.length} quyền</Badge>
            </div>
            <p className="text-xs text-slate-500">{role.description}</p>
            <div className="flex flex-wrap gap-1.5 pt-2">
              {role.permissions.map((perm) => (
                <span
                  key={perm}
                  className="px-2 py-0.5 rounded-lg bg-slate-100 text-slate-700 font-mono text-[11px]"
                >
                  {perm}
                </span>
              ))}
            </div>
          </div>
        ))}
      </div>

      <div className="bg-white rounded-3xl border border-slate-200 p-6 space-y-3">
        <h2 className="text-sm font-extrabold text-slate-900">
          Danh mục {permissions.length} Permissions hệ thống
        </h2>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-2.5">
          {permissions.map((p) => (
            <div
              key={p.id}
              className="p-3 rounded-2xl bg-slate-50 border border-slate-200 flex items-start justify-between gap-2 text-xs"
            >
              <div>
                <div className="font-mono font-bold text-emerald-800">{p.code}</div>
                <div className="text-slate-600 mt-0.5">{p.description}</div>
              </div>
              <Badge variant="default">{p.module}</Badge>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
