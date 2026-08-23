import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../shared/hooks/useAuth'

export default function ProtectedRoute() {
  const { isAuthenticated, user } = useAuth()
  const location = useLocation()

  if (!isAuthenticated) return <Navigate to="/login" replace />

  // ยังไม่เปลี่ยนรหัสผ่านชั่วคราว → ใช้ได้แค่หน้าเปลี่ยนรหัสผ่าน
  if (user?.mustChangePassword && location.pathname !== '/change-password')
    return <Navigate to="/change-password" replace />

  return <Outlet />
}
