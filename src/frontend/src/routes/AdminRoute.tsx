import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../shared/hooks/useAuth'

/**
 * กันไม่ให้เข้าหน้าตั้งค่ากลางด้วยการพิมพ์ URL ตรง ๆ ทั้งที่เมนูถูกซ่อนไว้.
 * เป็นแค่ชั้นความสะดวก — ตัวจริงบังคับที่ API (Authorize Roles=Admin) อีกชั้น
 */
export default function AdminRoute() {
  const { isAdmin } = useAuth()
  return isAdmin ? <Outlet /> : <Navigate to="/dashboard" replace />
}
