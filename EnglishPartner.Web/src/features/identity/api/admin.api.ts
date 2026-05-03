import { apiClient } from '@/lib/axios'
import type { 
  AdminUserResponse, 
  AssignRolePermissionRequest,
  CreateRoleRequest,
  GetUsersParams, 
  ManageRoleRequest, 
  PaginatedResult, 
  PermissionResponse,
  ResetPasswordRequest,
  RoleResponse,
  UpdateRoleRequest,
} from '../types/admin.types'

// ── User Management ──────────────────────────────────────────────────────────

export const getUsers = async (params: GetUsersParams): Promise<PaginatedResult<AdminUserResponse>> => {
  const { data } = await apiClient.get<PaginatedResult<AdminUserResponse>>('/api/v1/admin/identity/users', { params })
  return data
}

export const activateUser = async (id: string): Promise<void> => {
  await apiClient.post(`/api/v1/admin/identity/users/${id}/activate`)
}

export const deactivateUser = async (id: string): Promise<void> => {
  await apiClient.post(`/api/v1/admin/identity/users/${id}/deactivate`)
}

export const deleteUser = async (id: string): Promise<void> => {
  await apiClient.delete(`/api/v1/admin/identity/users/${id}`)
}

export const resetUserPassword = async (id: string, body: ResetPasswordRequest): Promise<string> => {
  const { data } = await apiClient.post<{ newPassword: string }>(`/api/v1/admin/identity/users/${id}/reset-password`, body)
  return data?.newPassword
}

export const manageUserRole = async (id: string, body: ManageRoleRequest): Promise<void> => {
  await apiClient.post(`/api/v1/admin/identity/users/${id}/roles`, body)
}

// ── Role Management ──────────────────────────────────────────────────────────

export const getRoles = async (): Promise<RoleResponse[]> => {
  const { data } = await apiClient.get<RoleResponse[]>('/api/v1/admin/identity/roles')
  return data
}

export const createRole = async (body: CreateRoleRequest): Promise<RoleResponse> => {
  const { data } = await apiClient.post<RoleResponse>('/api/v1/admin/identity/roles', body)
  return data
}

export const updateRole = async (id: string, body: UpdateRoleRequest): Promise<void> => {
  await apiClient.put(`/api/v1/admin/identity/roles/${id}`, body)
}

export const deleteRole = async (id: string): Promise<void> => {
  await apiClient.delete(`/api/v1/admin/identity/roles/${id}`)
}

export const addRolePermission = async (roleId: string, body: AssignRolePermissionRequest): Promise<void> => {
  await apiClient.post(`/api/v1/admin/identity/roles/${roleId}/permissions`, body)
}

export const removeRolePermission = async (roleId: string, permissionId: string): Promise<void> => {
  await apiClient.delete(`/api/v1/admin/identity/roles/${roleId}/permissions/${permissionId}`)
}

// ── Permissions (read-only) ──────────────────────────────────────────────────

export const getPermissions = async (): Promise<PermissionResponse[]> => {
  const { data } = await apiClient.get<PermissionResponse[]>('/api/v1/admin/identity/permissions')
  return data
}

