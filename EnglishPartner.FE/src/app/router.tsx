import React, { Suspense, lazy } from 'react';
import { BrowserRouter, Navigate, Route, Routes, useParams } from 'react-router-dom';
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

const AdminLessonsPage = lazy(() =>
  import('@/features/lessons').then((m) => ({ default: m.AdminLessonsPage }))
);
const AdminLessonBuilderPage = lazy(() =>
  import('@/features/lessons').then((m) => ({ default: m.AdminLessonBuilderPage }))
);
const AdminLessonRubricsPage = lazy(() =>
  import('@/features/lessons').then((m) => ({ default: m.AdminLessonRubricsPage }))
);
const StudentLessonsPage = lazy(() =>
  import('@/features/lessons').then((m) => ({ default: m.StudentLessonsPage }))
);
const StudentLessonDetailPage = lazy(() =>
  import('@/features/lessons').then((m) => ({ default: m.StudentLessonDetailPage }))
);
const StudentLessonPlayerPage = lazy(() =>
  import('@/features/lessons').then((m) => ({ default: m.StudentLessonPlayerPage }))
);
const StudentLessonHistoryPage = lazy(() =>
  import('@/features/lessons').then((m) => ({ default: m.StudentLessonHistoryPage }))
);
const GrammarRoadmapPage = lazy(() =>
  import('@/features/grammar').then((m) => ({ default: m.GrammarRoadmapPage }))
);
const GrammarStepPage = lazy(() =>
  import('@/features/grammar').then((m) => ({ default: m.GrammarStepPage }))
);
const GrammarAttemptPage = lazy(() =>
  import('@/features/grammar').then((m) => ({ default: m.GrammarAttemptPage }))
);
const AdminGrammarStepsPage = lazy(() =>
  import('@/features/grammar').then((m) => ({ default: m.AdminGrammarStepsPage }))
);
const AdminGrammarStepEditorPage = lazy(() =>
  import('@/features/grammar').then((m) => ({ default: m.AdminGrammarStepEditorPage }))
);
const AdminGrammarModerationPage = lazy(() =>
  import('@/features/grammar').then((m) => ({ default: m.AdminGrammarModerationPage }))
);
const MyGrammarExercisesPage = lazy(() =>
  import('@/features/grammar').then((m) => ({ default: m.MyGrammarExercisesPage }))
);
const FallingWordsSetupPage = lazy(() =>
  import('@/features/vocabulary-games').then((m) => ({ default: m.FallingWordsSetupPage }))
);
const FallingWordsPlayPage = lazy(() =>
  import('@/features/vocabulary-games').then((m) => ({ default: m.FallingWordsPlayPage }))
);
const FallingWordsHistoryPage = lazy(() =>
  import('@/features/vocabulary-games').then((m) => ({ default: m.FallingWordsHistoryPage }))
);

export const GrammarStepRoute: React.FC = () => {
  const { code } = useParams<{ code: string }>();
  return <GrammarStepPage key={code} />;
};

export const GrammarAttemptRoute: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  return <GrammarAttemptPage key={id} />;
};

export const AdminGrammarStepRoute: React.FC = () => {
  const { code } = useParams<{ code: string }>();
  return <AdminGrammarStepEditorPage key={code} />;
};

const PageLoader: React.FC = () => (
  <div className="min-h-[45vh] flex items-center justify-center text-xs font-semibold text-slate-500">
    Đang tải trang...
  </div>
);

export const AppRoutes: React.FC = () => {
  return (
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
              <Route
                path="vocabulary-games/falling-words"
                element={
                  <RequirePermission permission="vocabularies.read">
                    <FallingWordsSetupPage />
                  </RequirePermission>
                }
              />
              <Route
                path="vocabulary-games/falling-words/sessions/:id"
                element={
                  <RequirePermission permission="vocabularies.read">
                    <FallingWordsPlayPage />
                  </RequirePermission>
                }
              />
              <Route
                path="vocabulary-games/falling-words/history"
                element={
                  <RequirePermission permission="vocabularies.read">
                    <FallingWordsHistoryPage />
                  </RequirePermission>
                }
              />
              <Route
                path="lessons"
                element={<StudentLessonsPage />}
              />
              <Route
                path="lessons/:id"
                element={<StudentLessonDetailPage />}
              />
              <Route
                path="lesson-attempts/:attemptId"
                element={<StudentLessonPlayerPage />}
              />
              <Route
                path="lesson-history"
                element={<StudentLessonHistoryPage />}
              />
              <Route
                path="grammar"
                element={
                  <RequirePermission permission="grammar.read">
                    <GrammarRoadmapPage />
                  </RequirePermission>
                }
              />
              <Route
                path="grammar/my-exercises"
                element={
                  <RequirePermission permission="grammar.generate">
                    <MyGrammarExercisesPage />
                  </RequirePermission>
                }
              />
              <Route
                path="grammar/attempts/:id"
                element={
                  <RequirePermission permission="grammar.attempt">
                    <GrammarAttemptRoute />
                  </RequirePermission>
                }
              />
              <Route
                path="grammar/:code"
                element={
                  <RequirePermission permission="grammar.read">
                    <GrammarStepRoute />
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
                    'lessons.manage',
                    'lessons.read',
                    'lessons.publish',
                    'grammar.manage',
                    'grammar.moderate',
                  ]}
                >
                  <AdminLayout />
                </RequirePermission>
              }
            >
              <Route index element={<AdminDashboardPage />} />
              <Route
                path="lessons"
                element={
                  <RequirePermission
                    anyPermissions={['lessons.manage', 'lessons.read', 'lessons.publish']}
                  >
                    <AdminLessonsPage />
                  </RequirePermission>
                }
              />
              <Route
                path="lessons/:id"
                element={
                  <RequirePermission permission="lessons.manage">
                    <AdminLessonBuilderPage />
                  </RequirePermission>
                }
              />
              <Route
                path="lesson-rubrics"
                element={
                  <RequirePermission anyPermissions={['rubrics.manage', 'lessons.manage']}>
                    <AdminLessonRubricsPage />
                  </RequirePermission>
                }
              />
              <Route
                path="grammar"
                element={
                  <RequirePermission permission="grammar.manage">
                    <AdminGrammarStepsPage />
                  </RequirePermission>
                }
              />
              <Route
                path="grammar/moderation"
                element={
                  <RequirePermission permission="grammar.moderate">
                    <AdminGrammarModerationPage />
                  </RequirePermission>
                }
              />
              <Route
                path="grammar/:code"
                element={
                  <RequirePermission permission="grammar.manage">
                    <AdminGrammarStepRoute />
                  </RequirePermission>
                }
              />
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
  );
};

export const AppRouter: React.FC = () => {
  return (
    <BrowserRouter>
      <AppRoutes />
    </BrowserRouter>
  );
};
