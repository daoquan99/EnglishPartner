import React, { Suspense, lazy } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { RequireAuth, RequirePermission } from '@/features/auth';
import { StudentLayout } from './layouts/StudentLayout';
import { AdminLayout } from './layouts/AdminLayout';

// Rule: Lazy-load all pages to minimize initial bundle size
const LoginPage = lazy(() =>
  import('@/features/auth').then((m) => ({ default: m.LoginPage }))
);
const RegisterPage = lazy(() =>
  import('@/features/auth').then((m) => ({ default: m.RegisterPage }))
);
const LiveTutorPage = lazy(() =>
  import('@/features/tutor').then((m) => ({ default: m.LiveTutorPage }))
);
const DailyVocabularyPage = lazy(() =>
  import('@/features/vocabulary').then((m) => ({ default: m.DailyVocabularyPage }))
);

const AdminDashboardPage = lazy(() =>
  import('@/features/admin').then((m) => ({ default: m.AdminDashboardPage }))
);
const AdminVocabulariesPage = lazy(() =>
  import('@/features/admin').then((m) => ({ default: m.AdminVocabulariesPage }))
);
const AdminTopicsPage = lazy(() =>
  import('@/features/admin').then((m) => ({ default: m.AdminTopicsPage }))
);
const AdminPromptsPage = lazy(() =>
  import('@/features/admin').then((m) => ({ default: m.AdminPromptsPage }))
);
const AdminVoicesPage = lazy(() =>
  import('@/features/admin').then((m) => ({ default: m.AdminVoicesPage }))
);
const AdminRubricsPage = lazy(() =>
  import('@/features/admin').then((m) => ({ default: m.AdminRubricsPage }))
);
const AdminUsersPage = lazy(() =>
  import('@/features/admin').then((m) => ({ default: m.AdminUsersPage }))
);
const AdminRolesPermissionsPage = lazy(() =>
  import('@/features/admin').then((m) => ({ default: m.AdminRolesPermissionsPage }))
);

const PageLoader: React.FC = () => (
  <div className="min-h-[45vh] flex items-center justify-center text-xs font-semibold text-slate-500">
    Đang tải trang...
  </div>
);

export const AppRouter: React.FC = () => {
  return (
    <BrowserRouter>
      <Suspense fallback={<PageLoader />}>
        <Routes>
          {/* Public Auth Routes */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />

          {/* Protected Student Portal */}
          <Route element={<RequireAuth />}>
            <Route path="/student" element={<StudentLayout />}>
              <Route index element={<Navigate to="tutor" replace />} />
              <Route
                path="tutor"
                element={
                  <RequirePermission permission="conversations.access">
                    <LiveTutorPage />
                  </RequirePermission>
                }
              />
              <Route
                path="vocabulary"
                element={
                  <RequirePermission permission="vocabularies.read">
                    <DailyVocabularyPage />
                  </RequirePermission>
                }
              />
            </Route>

            {/* Protected Admin Portal */}
            <Route
              path="/admin"
              element={
                <RequirePermission
                  anyPermissions={[
                    'users.read',
                    'users.manage',
                    'roles.manage',
                    'prompts.manage',
                    'voices.manage',
                    'topics.manage',
                    'rubrics.manage',
                  ]}
                >
                  <AdminLayout />
                </RequirePermission>
              }
            >
              <Route index element={<AdminDashboardPage />} />
              <Route
                path="vocabularies"
                element={
                  <RequirePermission
                    anyPermissions={['vocabularies.manage', 'vocabularies.generate', 'topics.manage']}
                  >
                    <AdminVocabulariesPage />
                  </RequirePermission>
                }
              />
              <Route
                path="topics"
                element={
                  <RequirePermission permission="topics.manage">
                    <AdminTopicsPage />
                  </RequirePermission>
                }
              />
              <Route
                path="prompts"
                element={
                  <RequirePermission permission="prompts.manage">
                    <AdminPromptsPage />
                  </RequirePermission>
                }
              />
              <Route
                path="voices"
                element={
                  <RequirePermission permission="voices.manage">
                    <AdminVoicesPage />
                  </RequirePermission>
                }
              />
              <Route
                path="rubrics"
                element={
                  <RequirePermission permission="rubrics.manage">
                    <AdminRubricsPage />
                  </RequirePermission>
                }
              />
              <Route
                path="users"
                element={
                  <RequirePermission anyPermissions={['users.read', 'users.manage']}>
                    <AdminUsersPage />
                  </RequirePermission>
                }
              />
              <Route
                path="roles"
                element={
                  <RequirePermission permission="roles.manage">
                    <AdminRolesPermissionsPage />
                  </RequirePermission>
                }
              />
            </Route>
          </Route>

          {/* Default Redirect */}
          <Route path="*" element={<Navigate to="/student/tutor" replace />} />
        </Routes>
      </Suspense>
    </BrowserRouter>
  );
};
