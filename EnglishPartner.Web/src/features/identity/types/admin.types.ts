export interface AdminUserResponse {
  id: string
  email: string
  displayName: string
  isActive: boolean
  createdAt: string
  roles: { id: string; name: string }[]
}

export interface PaginatedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface GetUsersParams {
  search?: string
  role?: string
  isActive?: boolean
  page?: number
  pageSize?: number
}

export interface ResetPasswordRequest {
  sendEmail: boolean
}

export interface ManageRoleRequest {
  roleId: string
  operation: 'Assign' | 'Revoke'
}

// ─── Role ────────────────────────────────────────────────────────────────────

export interface PermissionResponse {
  id: string
  code: string
  name: string
  module: string
  description?: string
}

export interface RoleResponse {
  id: string
  name: string
  description?: string
  permissions: PermissionResponse[]
}

export interface CreateRoleRequest {
  name: string
  description?: string
}

export interface UpdateRoleRequest {
  name: string
  description?: string
}

export interface AssignRolePermissionRequest {
  permissionId: string
}
