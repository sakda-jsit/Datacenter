import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../shared/hooks/useAuth'

interface Props {
  /** 'central' = Admin + หัวหน้างาน (ค่าเริ่มต้น) · 'admin' = Admin เท่านั้น */
  level?: 'central' | 'admin'
}

/**
 * กันไม่ให้เข้าหน้าตั้งค่ากลางด้วยการพิมพ์ URL ตรง ๆ ทั้งที่เมนูถูกซ่อนไว้.
 * เป็นแค่ชั้นความสะดวก — ตัวจริงบังคับที่ API (Authorize Roles) อีกชั้น
 */
export default function AdminRoute({ level = 'central' }: Props) {
  const { isAdmin, canCentralSettings } = useAuth()
  const allowed = level === 'admin' ? isAdmin : canCentralSettings
  return allowed ? <Outlet /> : <Navigate to="/dashboard" replace />
}
