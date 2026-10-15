import React from 'react';
import { Link, Outlet } from 'react-router-dom';
import { ShieldAlert, ArrowLeft } from 'lucide-react';
import { useCurrentUser } from '../hooks/useAuth';

interface RequirePermissionProps {
  permission?: string;
  anyPermissions?: string[];
  role?: string;
  children?: React.ReactNode;
}

/**
 * Guard Component 2: RequirePermission
 * Protects routes based on RBAC permission(s) or role.
 * Note: Frontend guard is for UX navigation protection; .NET Backend enforces RequirePermission on every API.
 */
export const RequirePermission: React.FC<RequirePermissionProps> = ({
  permission,
  anyPermissions,
  role,
  children,
}) => {
  const { user, hasPermission, hasAnyPermission, hasRole } = useCurrentUser();

  const allowedByPerm = permission ? hasPermission(permission) : true;
  const allowedByAny = anyPermissions ? hasAnyPermission(anyPermissions) : true;
  const allowedByRole = role ? hasRole(role) : true;

  if (!user || !allowedByPerm || !allowedByAny || !allowedByRole) {
    return (
      <div className="min-h-[65vh] flex items-center justify-center p-6">
        <div className="max-w-md w-full bg-white rounded-3xl border border-rose-200 shadow-lg p-8 text-center space-y-4">
          <img
            src="/assets/illustrations/illustration-load-error.svg"
            alt=""
            className="w-36 h-36 object-contain mx-auto select-none pointer-events-none"
          />
          <h2 className="text-xl font-bold text-slate-900">Không đủ quyền truy cập (403)</h2>
          <p className="text-sm text-slate-600 leading-relaxed">
            Tài khoản <strong className="text-slate-900">{user?.username ?? 'Guest'}</strong> không có quyền{' '}
            <code className="px-2 py-0.5 rounded bg-rose-50 text-rose-700 font-mono text-xs">
              {permission || anyPermissions?.join(' | ') || role}
            </code>{' '}
            để truy cập khu vực này.
          </p>
          <div className="pt-2 flex items-center justify-center gap-3">
            <Link
              to="/student/tutor"
              className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl bg-emerald-600 text-white text-sm font-semibold hover:bg-emerald-700 transition-colors"
            >
              <ArrowLeft className="w-4 h-4" />
              Về Cổng Học Viên
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return children ? <>{children}</> : <Outlet />;
};
