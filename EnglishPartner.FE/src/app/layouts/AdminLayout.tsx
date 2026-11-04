import React, { useEffect, useRef } from 'react';
import { Outlet } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Menu } from 'lucide-react';
import { AdminSidebar } from '@/features/admin';
import { useMobileDrawer, useIsDesktop } from '@/shared/hooks/useMobileDrawer';
import { cn } from '@/shared/lib/cn';

/**
 * Admin Portal App Shell (.adminTheme):
 * - Outer container: `h-screen w-full overflow-hidden flex adminTheme`
 * - Mobile (< lg): off-canvas drawer `fixed inset-y-0 left-0 z-50 w-72 max-w-[85vw]`
 * - Desktop (>= lg): fixed static sidebar `h-screen shrink-0 overflow-y-auto`
 * - Mobile sticky top bar in `main` with hamburger button, brand logo, and portal title
 * - Main Content (`main`): `flex-1 h-screen overflow-y-auto` scrolls independently
 */
export const AdminLayout: React.FC = () => {
  const { t } = useTranslation();
  const mobileDrawer = useMobileDrawer();
  const isDesktop = useIsDesktop();

  // Auto-close mobile drawer when switching to desktop
  const prevDesktopRef = useRef(isDesktop);
  useEffect(() => {
    if (!prevDesktopRef.current && isDesktop && mobileDrawer.isOpen) {
      mobileDrawer.close();
    }
    prevDesktopRef.current = isDesktop;
  }, [isDesktop, mobileDrawer.isOpen, mobileDrawer.close]);

  return (
    <div className="h-screen w-full overflow-hidden adminTheme flex">
      {/* Mobile Drawer Backdrop Overlay */}
      {mobileDrawer.isOpen && (
        <div
          role="presentation"
          aria-hidden="true"
          onClick={mobileDrawer.close}
          className="fixed inset-0 z-40 bg-slate-900/40 lg:hidden backdrop-blur-[1px] transition-opacity cursor-pointer"
        />
      )}

      {/* Admin Sidebar / Drawer */}
      <AdminSidebar
        id="admin-sidebar"
        isOpen={mobileDrawer.isOpen}
        onClose={mobileDrawer.close}
        drawerRef={mobileDrawer.drawerRef}
      />

      {/* Independent Scrollable Main Content */}
      <main
        inert={!isDesktop && mobileDrawer.isOpen ? true : undefined}
        className={cn(
          'flex-1 h-screen min-w-0 flex flex-col',
          mobileDrawer.isOpen && !isDesktop ? 'overflow-hidden' : 'overflow-y-auto overflow-x-hidden'
        )}
      >
        {/* Mobile Sticky Top Bar (< lg) */}
        <header className="lg:hidden sticky top-0 z-20 shrink-0 bg-slate-900/95 backdrop-blur-xs border-b border-slate-800 px-4 py-2.5 flex items-center justify-between">
          <div className="flex items-center gap-3 min-w-0">
            <button
              ref={mobileDrawer.triggerRef}
              type="button"
              onClick={mobileDrawer.toggle}
              aria-label={t('nav.openMenu')}
              aria-expanded={mobileDrawer.isOpen}
              aria-controls="admin-sidebar"
              className="min-h-[44px] min-w-[44px] flex items-center justify-center rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white transition-colors cursor-pointer shrink-0"
            >
              <Menu className="w-5 h-5" />
            </button>

            <div className="flex items-center gap-2.5 min-w-0">
              <img
                src="/assets/brand/logo-mark.svg"
                alt=""
                className="w-8 h-8 shrink-0 drop-shadow-md object-contain"
              />
              <div className="min-w-0">
                <div className="text-sm font-extrabold text-white truncate">
                  {t('appTitle')}
                </div>
                <div className="text-[11px] text-emerald-400 font-semibold truncate">
                  {t('adminPortal')}
                </div>
              </div>
            </div>
          </div>
        </header>

        {/* Content Container */}
        <div className="flex-1 p-4 sm:p-6">
          <div className="max-w-7xl mx-auto">
            <Outlet />
          </div>
        </div>
      </main>
    </div>
  );
};
