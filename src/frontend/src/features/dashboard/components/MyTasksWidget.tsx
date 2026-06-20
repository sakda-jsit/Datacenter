import { Link } from 'react-router-dom'
import { useAuth } from '../../../shared/hooks/useAuth'
import { useWorkboard } from '../../tasks/hooks/useTasks'

function fmtDate(d?: string | null) {
  return d ? d.slice(0, 10) : '—'
}

/**
 * Widget "งานของฉัน" บน Dashboard — งานค้าง/เกินกำหนดที่มอบหมายให้ผู้ใช้ปัจจุบัน
 * รวมทุกบริษัทที่เข้าถึงได้ (WorkTask + ComplianceTask) reuse GetWorkboardQuery
 */
export default function MyTasksWidget() {
  const { user } = useAuth()
  const { data: items, isLoading } = useWorkboard(
    { assignedUserId: user?.userId ?? null, openOnly: true, includeCompliance: true },
    !!user?.userId,
  )

  if (!user?.userId) return null

  const open = items?.length ?? 0
  const overdue = items?.filter((i) => i.isOverdue).length ?? 0
  // เรียงตามเกินกำหนดก่อน (board เรียงให้แล้ว) — แสดง 6 อันแรก
  const top = (items ?? []).slice(0, 6)

  return (
    <div className="dc-card overflow-hidden">
      <div className="flex items-center justify-between border-b border-slate-100 px-5 py-3">
        <span className="text-sm font-extrabold text-slate-800">
          🗂️ งานของฉัน
          {open > 0 && (
            <span className="ml-2 text-xs font-bold text-slate-500">
              ค้าง {open}
              {overdue > 0 && <span className="ml-1 text-red-600">· เกินกำหนด {overdue}</span>}
            </span>
          )}
        </span>
        <Link to="/tasks" className="text-xs font-bold text-sky-600 hover:underline">
          ดูทั้งหมด →
        </Link>
      </div>

      {isLoading ? (
        <p className="p-5 text-sm text-slate-400">กำลังโหลด...</p>
      ) : open === 0 ? (
        <p className="p-5 text-sm text-slate-400">ไม่มีงานที่มอบหมายให้คุณค้างอยู่ 🎉</p>
      ) : (
        <ul className="divide-y divide-slate-100">
          {top.map((i) => (
            <li key={`${i.source}-${i.id}`} className="flex flex-wrap items-center gap-3 px-5 py-3">
              <span
                className={`dc-pill ${i.source === 'Task' ? 'bg-sky-50 text-sky-700' : 'bg-amber-50 text-amber-700'}`}
              >
                {i.source === 'Task' ? 'งานทั่วไป' : 'ภาษี'}
              </span>
              <span className="min-w-0 flex-1">
                <span className="block truncate text-sm font-medium text-slate-800">{i.title}</span>
                <span className="text-xs text-slate-500">{i.clientName}</span>
              </span>
              <span className={`whitespace-nowrap text-xs ${i.isOverdue ? 'font-bold text-red-600' : 'text-slate-500'}`}>
                {fmtDate(i.dueDate)}
                {i.isOverdue && typeof i.daysToDue === 'number' ? ` · เกิน ${Math.abs(i.daysToDue)} วัน` : ''}
              </span>
            </li>
          ))}
          {open > top.length && (
            <li className="px-5 py-2 text-center text-xs text-slate-400">
              และอีก {open - top.length} งาน —{' '}
              <Link to="/tasks" className="text-sky-600 hover:underline">ดูทั้งหมด</Link>
            </li>
          )}
        </ul>
      )}
    </div>
  )
}
