import React from 'react';
import { describe, it, expect, beforeEach } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { RequireAuth } from '../components/RequireAuth';
import { useAuthStore } from '../store/authStore';
import type { UserProfileDto } from '@/shared/api/openapi.types';

const testUser: UserProfileDto = {
  id: 'u-1',
  username: 'student1',
  email: 'student1@lingomint.local',
  fullName: 'Student One',
  preferredCefrLevel: 'B1',
  preferredVoiceName: 'Aoede',
  timeZoneId: 'Asia/Ho_Chi_Minh',
  isActive: true,
  roles: ['LEARNER'],
  permissions: ['conversations.access'],
};

describe('RequireAuth Guard component', () => {
  beforeEach(() => {
    (useAuthStore as any).getInitialState = () => useAuthStore.getState();
    (useAuthStore as any).getServerSnapshot = () => useAuthStore.getState();
    useAuthStore.setState({
      isAuthenticated: false,
      isInitialized: false,
      accessToken: null,
      currentUser: null,
    });
  });

  it('renders loading state without redirecting when not initialized (isInitialized === false)', () => {
    const html = renderToStaticMarkup(
      <MemoryRouter initialEntries={['/protected']}>
        <Routes>
          <Route
            path="/protected"
            element={
              <RequireAuth>
                <div>Protected Content</div>
              </RequireAuth>
            }
          />
          <Route path="/login" element={<div>Login Page</div>} />
        </Routes>
      </MemoryRouter>
    );

    expect(html).toContain('Đang kiểm tra phiên đăng nhập...');
    expect(html).not.toContain('Login Page');
    expect(html).not.toContain('Protected Content');
  });

  it('redirects to /login when initialized and unauthenticated (isInitialized === true, isAuthenticated === false)', () => {
    useAuthStore.setState({
      isInitialized: true,
      isAuthenticated: false,
      currentUser: null,
    });

    const html = renderToStaticMarkup(
      <MemoryRouter initialEntries={['/protected']}>
        <Routes>
          <Route
            path="/protected"
            element={
              <RequireAuth>
                <div>Protected Content</div>
              </RequireAuth>
            }
          />
          <Route path="/login" element={<div>Login Page</div>} />
        </Routes>
      </MemoryRouter>
    );

    // In static rendering, <Navigate to="/login" /> renders empty null markup and does not reveal protected content
    expect(html).toBe('');
    expect(html).not.toContain('Protected Content');
    expect(html).not.toContain('Đang kiểm tra phiên đăng nhập...');
  });

  it('renders protected content when initialized and authenticated', () => {
    useAuthStore.setState({
      isInitialized: true,
      isAuthenticated: true,
      currentUser: testUser,
      accessToken: 'valid-token',
    });

    const html = renderToStaticMarkup(
      <MemoryRouter initialEntries={['/protected']}>
        <Routes>
          <Route
            path="/protected"
            element={
              <RequireAuth>
                <div>Protected Content</div>
              </RequireAuth>
            }
          />
          <Route path="/login" element={<div>Login Page</div>} />
        </Routes>
      </MemoryRouter>
    );

    expect(html).toContain('Protected Content');
    expect(html).not.toContain('Login Page');
  });
});
