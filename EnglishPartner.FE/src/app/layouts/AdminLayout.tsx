import React from 'react';
import { Outlet } from 'react-router-dom';
import { AdminSidebar } from '@/features/admin';

/**
 * Admin Portal App Shell:
 * - Outer container: `h-screen w-full overflow-hidden flex` (no body scroll)
 * - Left Menu (`AdminSidebar`): fixed `h-screen shrink-0 overflow-y-auto`
 * - Main Content (`main`): `flex-1 h-screen overflow-y-auto` scrolls independently
 */
export const AdminLayout: React.FC = () => {
  return (
    <div className="h-screen w-full overflow-hidden bg-slate-100 text-slate-900 flex">
      <AdminSidebar />
      <main className="flex-1 h-screen overflow-y-auto overflow-x-hidden min-w-0 p-4 sm:p-6">
        <div className="max-w-7xl mx-auto">
          <Outlet />
        </div>
      </main>
    </div>
  );
};
