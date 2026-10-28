import React from 'react';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import { LoadingState } from '@/shared/components/ui/LoadingState';

interface RequireAuthProps {
  children?: React.ReactNode;
}

/**
 * Guard Component 1: RequireAuth
 * Checks if user is authenticated; displays loading when uninitialized;
 * redirects unauthenticated visitors to /login with return state when initialization is complete.
 */
export const RequireAuth: React.FC<RequireAuthProps> = ({ children }) => {
  const location = useLocation();
  const store = useAuthStore();
  const isInitialized = typeof window === 'undefined' ? useAuthStore.getState().isInitialized : store.isInitialized;
  const isAuthenticated = typeof window === 'undefined' ? useAuthStore.getState().isAuthenticated : store.isAuthenticated;
  const currentUser = typeof window === 'undefined' ? useAuthStore.getState().currentUser : store.currentUser;

  if (!isInitialized) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-slate-50">
        <LoadingState message="Đang kiểm tra phiên đăng nhập..." />
      </div>
    );
  }

  if (!isAuthenticated || !currentUser) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return children ? <>{children}</> : <Outlet />;
};
