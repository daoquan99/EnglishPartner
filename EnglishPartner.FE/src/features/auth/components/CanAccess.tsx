import React from 'react';
import { useCurrentUser } from '../hooks/useAuth';

interface CanAccessProps {
  permission?: string;
  anyPermissions?: string[];
  role?: string;
  fallback?: React.ReactNode;
  children: React.ReactNode;
}

/**
 * Guard Component 3: CanAccess
 * Controls visibility of buttons, actions, and menus based on user's actual RBAC permissions/roles.
 */
export const CanAccess: React.FC<CanAccessProps> = ({
  permission,
  anyPermissions,
  role,
  fallback = null,
  children,
}) => {
  const { user, hasPermission, hasAnyPermission, hasRole } = useCurrentUser();

  if (!user) return <>{fallback}</>;

  const allowedByPerm = permission ? hasPermission(permission) : true;
  const allowedByAny = anyPermissions ? hasAnyPermission(anyPermissions) : true;
  const allowedByRole = role ? hasRole(role) : true;

  if (!allowedByPerm || !allowedByAny || !allowedByRole) {
    return <>{fallback}</>;
  }

  return <>{children}</>;
};
