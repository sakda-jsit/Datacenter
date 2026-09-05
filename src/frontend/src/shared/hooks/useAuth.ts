import { useState } from 'react'
import axios from 'axios'
import type { AuthUser } from '../types/auth.types'
import { apiBaseUrl, clearSession } from '../services/apiClient'

export function readAuthUser(): AuthUser | null {
  const raw = localStorage.getItem('user')
  if (!raw) return null
  try {
    return JSON.parse(raw) as AuthUser
  } catch {
    return null
  }
}

export function useAuth() {
  const [user] = useState<AuthUser | null>(() => readAuthUser())

  const isAuthenticated = !!user
  const isAdmin = user?.role === 'Admin'

  /**
   * ผู้ใช้ปัจจุบัน "ดูแล" บริษัทนี้หรือไม่ = ทำรายการได้ไหม
   * (ดูข้อมูลได้ทุกบริษัทอยู่แล้ว — ที่จำกัดคือการบันทึก/แก้/ลบ และการดูข้อมูลเงินเดือน)
   * Admin (ownedCompanyIds = null) ทำได้ทุกบริษัท
   */
  function canManage(companyId: number | null | undefined): boolean {
    if (!user || !companyId) return false
    if (user.ownedCompanyIds == null) return true   // Admin
    return user.ownedCompanyIds.includes(companyId)
  }

  function logout() {
    // แจ้ง server ให้ยกเลิก refresh token ใบนี้ (ไม่รอผล — ออกจากระบบต้องไม่ค้างเพราะเน็ตช้า)
    const refreshToken = localStorage.getItem('refreshToken')
    if (refreshToken) void axios.post(`${apiBaseUrl}/auth/logout`, { refreshToken }).catch(() => {})

    clearSession()
    localStorage.removeItem('companyId')
    window.location.href = '/login'
  }

  return { user, isAuthenticated, isAdmin, canManage, logout }
}
