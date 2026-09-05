import { useEffect, useState } from 'react'
import { Outlet } from 'react-router-dom'
import Sidebar from './Sidebar'
import TopBar from './TopBar'
import { useAuth } from '../../hooks/useAuth'
import { useCurrentCompany } from '../../hooks/useCurrentCompany'

export default function AppLayout() {
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [collapsed, setCollapsed] = useState(() => {
    try {
      return localStorage.getItem('sidebarCollapsed') === '1'
    } catch {
      return false
    }
  })

  useEffect(() => {
    try {
      localStorage.setItem('sidebarCollapsed', collapsed ? '1' : '0')
    } catch {
      // Ignore storage failures in restricted browser contexts.
    }
  }, [collapsed])

  return (
    <div className="flex min-h-screen bg-[linear-gradient(180deg,#f8fbff_0%,#f5f7fb_42%,#f7f8fb_100%)]">
      {sidebarOpen && (
        <button
          type="button"
          aria-label="ปิดเมนู"
          onClick={() => setSidebarOpen(false)}
          className="fixed inset-0 z-30 bg-slate-900/40 md:hidden"
        />
      )}

      <Sidebar
        collapsed={collapsed}
        open={sidebarOpen}
        onToggleCollapsed={() => setCollapsed((value) => !value)}
        onCloseMobile={() => setSidebarOpen(false)}
      />

      <div className="flex min-w-0 flex-1 flex-col">
        <TopBar onOpenMenu={() => setSidebarOpen(true)} />
        <ReadOnlyBanner />
        <main className="flex-1 px-4 py-5 sm:px-6 lg:px-8">
          <Outlet />
        </main>
        <footer className="px-6 pb-7 pt-3 text-center text-xs text-slate-500">
          JSP Datacenter · Accounting Office Platform
        </footer>
      </div>
    </div>
  )
}

/**
 * แถบเตือนเมื่อผู้ใช้ "ดูอย่างเดียว" สำหรับบริษัทที่เลือกอยู่ —
 * ทุกคนดูข้อมูลได้ทุกบริษัท แต่บันทึก/แก้/ลบ และดูข้อมูลเงินเดือน ทำได้เฉพาะบริษัทที่ตัวเองดูแล
 */
function ReadOnlyBanner() {
  const { companyId } = useCurrentCompany()
  const { user, canManage } = useAuth()

  if (!user || !companyId || canManage(companyId)) return null

  return (
    <div className="mx-4 mt-3 rounded-lg border border-amber-200 bg-amber-50 px-4 py-2 text-sm text-amber-800 sm:mx-6 lg:mx-8">
      👁 <b>ดูอย่างเดียว</b> — คุณไม่ได้เป็นผู้ดูแลบริษัทนี้ จึงบันทึก/แก้ไขข้อมูล
      และดูข้อมูลเงินเดือนไม่ได้ · ติดต่อผู้ดูแลระบบถ้าต้องการสิทธิ์
    </div>
  )
}
