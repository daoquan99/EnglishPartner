import React from 'react';
import { Users } from 'lucide-react';
import { useAdminUsersQuery, useAssignUserRolesMutation } from '../hooks/useAdminQueries';
import { CanAccess } from '@/features/auth';
import { Badge } from '@/shared/components/ui/Badge';

export const AdminUsersPage: React.FC = () => {
  const { data: users = [] } = useAdminUsersQuery();
  const assignRolesMutation = useAssignUserRolesMutation();

  const toggleTeacherRole = (userId: string, currentRoles: string[]) => {
    const hasTeacher = currentRoles.includes('TEACHER');
    const nextRoles = hasTeacher
      ? currentRoles.filter((r) => r !== 'TEACHER')
      : [...currentRoles, 'TEACHER'];
    assignRolesMutation.mutate({
      userId,
      roleCodes: nextRoles.length > 0 ? nextRoles : ['LEARNER'],
    });
  };

  return (
    <div className="space-y-6">
      <div className="bg-white rounded-3xl border border-slate-200 p-6 flex items-center justify-between">
        <div>
          <h1 className="text-lg font-extrabold text-slate-900 flex items-center gap-2">
            <Users className="w-5 h-5 text-emerald-600" />
            Quản lý Người dùng & Phân quyền (`Users`)
          </h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Kiểm tra danh sách tài khoản, trình độ CEFR ưu tiên và gán Role (bảo vệ bằng `CanAccess permission=&quot;users.manage&quot;`).
          </p>
        </div>
        <Badge variant="emerald">Permission: users.read / users.manage</Badge>
      </div>

      <div className="bg-white rounded-3xl border border-slate-200 overflow-hidden">
        <table className="w-full text-left border-collapse text-xs">
          <thead>
            <tr className="bg-slate-50 border-b border-slate-200 text-slate-600 uppercase">
              <th className="py-3 px-4">Tài khoản</th>
              <th className="py-3 px-3">CEFR / Giọng Teacher</th>
              <th className="py-3 px-3">Roles hiện tại</th>
              <th className="py-3 px-3">Số Permissions</th>
              <th className="py-3 px-4 text-right">Hành động (`CanAccess`)</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {users.map((u) => (
              <tr key={u.id} className="hover:bg-slate-50">
                <td className="py-3 px-4">
                  <div className="font-bold text-slate-900">{u.fullName}</div>
                  <div className="text-slate-500">
                    @{u.username} · {u.email}
                  </div>
                </td>
                <td className="py-3 px-3">
                  <Badge variant="emerald">{u.preferredCefrLevel}</Badge>{' '}
                  <Badge variant="violet">{u.preferredVoiceName}</Badge>
                </td>
                <td className="py-3 px-3 space-x-1">
                  {u.roles.map((r) => (
                    <Badge key={r} variant={r === 'ADMIN' ? 'amber' : 'sky'}>
                      {r}
                    </Badge>
                  ))}
                </td>
                <td className="py-3 px-3 font-semibold text-slate-700">
                  {u.permissions.length} quyền
                </td>
                <td className="py-3 px-4 text-right">
                  <CanAccess
                    permission="users.manage"
                    fallback={<span className="text-slate-400">Chỉ xem</span>}
                  >
                    <button
                      type="button"
                      onClick={() => toggleTeacherRole(u.id, u.roles)}
                      className="px-3 py-1.5 rounded-xl bg-slate-100 hover:bg-emerald-50 text-slate-700 hover:text-emerald-700 font-semibold cursor-pointer"
                    >
                      {u.roles.includes('TEACHER') ? 'Gỡ Role TEACHER' : '+ Gán Role TEACHER'}
                    </button>
                  </CanAccess>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};
