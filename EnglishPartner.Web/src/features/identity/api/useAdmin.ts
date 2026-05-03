'use client'

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { 
  activateUser, 
  addRolePermission,
  createRole,
  deactivateUser, 
  deleteRole,
  deleteUser, 
  getPermissions,
  getRoles,
  getUsers, 
  manageUserRole, 
  removeRolePermission,
  resetUserPassword,
  updateRole,
} from './admin.api'
import type { GetUsersParams, UpdateRoleRequest } from '../types/admin.types'

// ── User Management ──────────────────────────────────────────────────────────

export const useAdminUsers = (params: GetUsersParams) => {
  return useQuery({
    queryKey: ['admin', 'users', params],
    queryFn: () => getUsers(params),
    staleTime: 60 * 1000,
  })
}

export const useActivateUser = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: activateUser,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'users'] })
      toast.success('User activated successfully')
    },
    onError: () => toast.error('Failed to activate user')
  })
}

export const useDeactivateUser = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deactivateUser,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'users'] })
      toast.success('User deactivated successfully')
    },
    onError: () => toast.error('Failed to deactivate user')
  })
}

export const useDeleteUser = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteUser,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'users'] })
      toast.success('User deleted successfully')
    },
    onError: () => toast.error('Failed to delete user')
  })
}

export const useResetUserPassword = () => {
  return useMutation({
    mutationFn: ({ id, sendEmail }: { id: string, sendEmail: boolean }) => resetUserPassword(id, { sendEmail }),
    onSuccess: (newPassword, vars) => {
      if (!vars.sendEmail && newPassword) {
        toast.success(`Password reset. New password: ${newPassword}`, { duration: 10000 })
      } else {
        toast.success('Password reset email sent')
      }
    },
    onError: () => toast.error('Failed to reset password')
  })
}

export const useManageUserRole = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, roleId, operation }: { id: string, roleId: string, operation: 'Assign' | 'Revoke' }) => 
      manageUserRole(id, { roleId, operation }),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'users'] })
      toast.success(`Role ${vars.operation === 'Assign' ? 'assigned' : 'revoked'} successfully`)
    },
    onError: () => toast.error('Failed to manage role')
  })
}

// ── Role Management ──────────────────────────────────────────────────────────

export const useAdminRoles = () => {
  return useQuery({
    queryKey: ['admin', 'roles'],
    queryFn: getRoles,
  })
}

export const useCreateRole = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createRole,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'roles'] })
      toast.success('Role created successfully')
    },
    onError: () => toast.error('Failed to create role')
  })
}

export const useUpdateRole = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ...body }: { id: string } & UpdateRoleRequest) => updateRole(id, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'roles'] })
      toast.success('Role updated successfully')
    },
    onError: () => toast.error('Failed to update role')
  })
}

export const useDeleteRole = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteRole,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'roles'] })
      toast.success('Role deleted successfully')
    },
    onError: () => toast.error('Failed to delete role')
  })
}

export const useAddRolePermission = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ roleId, permissionId }: { roleId: string, permissionId: string }) => 
      addRolePermission(roleId, { permissionId }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'roles'] })
      toast.success('Permission added successfully')
    },
    onError: () => toast.error('Failed to add permission')
  })
}

export const useRemoveRolePermission = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ roleId, permissionId }: { roleId: string, permissionId: string }) => 
      removeRolePermission(roleId, permissionId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'roles'] })
      toast.success('Permission removed successfully')
    },
    onError: () => toast.error('Failed to remove permission')
  })
}

// ── Permissions (read-only) ──────────────────────────────────────────────────

export const usePermissions = () => {
  return useQuery({
    queryKey: ['admin', 'permissions'],
    queryFn: getPermissions,
    staleTime: 10 * 60 * 1000, // 10 minutes (rarely changes)
  })
}
