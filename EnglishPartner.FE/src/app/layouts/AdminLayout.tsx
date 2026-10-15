import React from 'react';
import { Outlet } from 'react-router-dom';
import { AdminSidebar } from '@/features/admin';

/**
 * Admin Portal App Shell (.adminTheme):
 * - Outer container: `h-screen w-full overflow-hidden flex adminTheme`
 * - Left Menu (`AdminSidebar`): fixed `h-screen shrink-0 overflow-y-auto`
 * - Main Content (`main`): `flex-1 h-screen overflow-y-auto` scrolls independently
 */
export const AdminLayout: React.FC = () => {
  return (
    <div className="h-screen w-full overflow-hidden adminTheme flex">
      <AdminSidebar />
      <main className="flex-1 h-screen overflow-y-auto overflow-x-hidden min-w-0 p-4 sm:p-6">
        <div className="max-w-7xl mx-auto">
          <Outlet />
        </div>
      </main>
    </div>
  );
};
