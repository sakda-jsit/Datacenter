import { useState } from 'react'
import type { AuthUser } from '../types/auth.types'

export function useAuth() {
  const [user] = useState<AuthUser | null>(() => {
    const raw = localStorage.getItem('user')
    return raw ? (JSON.parse(raw) as AuthUser) : null
  })

  const isAuthenticated = !!user

  function logout() {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    localStorage.removeItem('companyId')
    window.location.href = '/login'
  }

  return { user, isAuthenticated, logout }
}
