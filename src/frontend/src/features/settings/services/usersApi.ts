import apiClient from '../../../shared/services/apiClient'

// ── ผู้ใช้ระบบ (เฉพาะ Admin) ──
// role serialize เป็นตัวเลขตามแบบของ API นี้ (ไม่มี string enum converter)

export const USER_ROLE = { Admin: 1, Maker: 2, Checker: 3, Supervisor: 4 } as const

export const USER_ROLE_LABEL: Record<number, string> = {
  1: 'ผู้ดูแลระบบ (Admin)',
  2: 'ผู้บันทึก (Maker)',
  3: 'ผู้ตรวจ (Checker)',
  4: 'หัวหน้างาน (Supervisor)',
}

export const USER_ROLE_DESC: Record<number, string> = {
  1: 'ทำรายการได้ทุกบริษัท + ตั้งค่ากลางทั้งหมด + ประวัติการใช้งาน',
  2: 'ดูได้ทุกบริษัท · บันทึก/นำเข้าข้อมูล + ดูเงินเดือน เฉพาะบริษัทที่ดูแล',
  3: 'ดูได้ทุกบริษัท · บันทึก/แก้ไข + ดูเงินเดือน เฉพาะบริษัทที่ดูแล',
  4: 'เหมือน Maker ในงานรายบริษัท + ตั้งค่ากลางได้ (ยกเว้นประวัติการใช้งาน)',
}

/** บทบาทที่เข้าเมนูตั้งค่ากลางได้ (ทุกอย่างในเมนู "ระบบ" ยกเว้นประวัติการใช้งาน) */
export const CENTRAL_SETTINGS_ROLES: number[] = [USER_ROLE.Admin, USER_ROLE.Supervisor]

export interface SystemUser {
  id: number
  username: string
  displayName: string
  email?: string | null
  role: number
  isActive: boolean
  mustChangePassword: boolean
  lastLoginAt?: string | null
  lockedUntil?: string | null
  isLocked: boolean
  companyIds: number[]
}

export interface UserCreateInput {
  username: string
  displayName: string
  email?: string | null
  role: number
  password: string
  companyIds: number[]
}

export interface UserUpdateInput {
  displayName: string
  email?: string | null
  role: number
  isActive: boolean
  companyIds: number[]
}

export const usersApi = {
  list: () => apiClient.get<SystemUser[]>('/users').then((r) => r.data),
  create: (d: UserCreateInput) => apiClient.post<{ id: number }>('/users', d).then((r) => r.data),
  update: (id: number, d: UserUpdateInput) => apiClient.put(`/users/${id}`, d).then((r) => r.data),
  resetPassword: (id: number, newPassword: string) =>
    apiClient.post(`/users/${id}/reset-password`, { newPassword }).then((r) => r.data),
  unlock: (id: number) => apiClient.post(`/users/${id}/unlock`).then((r) => r.data),
}
