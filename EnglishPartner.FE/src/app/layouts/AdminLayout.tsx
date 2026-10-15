import React from 'react';
import { Outlet, Link, useNavigate } from 'react-router-dom';
import { ArrowLeft, LogOut, ShieldCheck } from 'lucide-react';
import { AdminSidebar } from '@/features/admin';
import { useCurrentUser, useLogoutMutation } from '@/features/auth';
import { Badge } from '@/shared/components/ui/Badge';

export const AdminLayout: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useCurrentUser();
  const logoutMutation = useLogoutMutation();

  const handleLogout = async () => {
    await logoutMutation.mutateAsync();
    navigate('/login', { replace: true });
  };

  return (
    <div className="min-h-screen bg-slate-100 text-slate-900 flex flex-col">
      {/* Top Admin Portal Header */}
      <header className="bg-slate-900 text-white border-b border-slate-800">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 h-16 flex items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-2xl bg-emerald-500 text-slate-950 flex items-center justify-center font-extrabold">
              <ShieldCheck className="w-5 h-5" />
            </div>
            <div>
              <span className="text-base font-extrabold tracking-tight block leading-none">
                LingoMint Admin Portal
              </span>
              <span className="text-[11px] text-slate-400">
                Role & Permission-Based Administration
              </span>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <Link
              to="/student/tutor"
              className="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-xs font-bold text-emerald-300 transition-colors"
            >
              <ArrowLeft className="w-3.5 h-3.5" />
              Chuyển sang Student Portal
            </Link>

            {user && <Badge variant="amber">{user.username}</Badge>}

            <button
              type="button"
              onClick={handleLogout}
              className="inline-flex items-center gap-1 px-3 py-2 rounded-xl bg-rose-600/20 hover:bg-rose-600/30 text-rose-300 text-xs font-bold cursor-pointer"
            >
              <LogOut className="w-3.5 h-3.5" />
              Đăng xuất
            </button>
          </div>
        </div>
      </header>

      {/* Admin Sidebar + Main Outlet */}
      <div className="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 py-6 flex flex-col lg:flex-row gap-6 items-start">
        <AdminSidebar />
        <main className="flex-1 min-w-0 w-full">
          <Outlet />
        </main>
      </div>
    </div>
  );
};
