import { Fragment, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import Card from '../../../shared/components/ui/Card'
import PageHeader from '../../../shared/components/ui/PageHeader'
import StateMessage from '../../../shared/components/ui/StateMessage'
import StatusBadge from '../../../shared/components/ui/StatusBadge'
import Tabs from '../../../shared/components/ui/Tabs'
import ExportMenu from '../../../shared/components/ui/ExportMenu'
import { useAuth } from '../../../shared/hooks/useAuth'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import TaskFormModal from '../components/TaskFormModal'
import {
  useAssignableUsers, useAssignTask, useDeleteTask, useSendReminders, useSetTaskStatus,
  useToggleTaskItem, useWorkboard, useWorkTasks,
} from '../hooks/useTasks'
import { PRIORITY_LABEL, STATUS_OPTIONS, STATUS_TONE } from '../types/task.types'
import type { WorkItemDto, WorkTaskDto } from '../types/task.types'
import type { ExportSection } from '../../../shared/utils/exportTable'

type Tab = 'company' | 'board'
const TABS: { key: Tab; label: string }[] = [
  { key: 'company', label: 'งานของบริษัท' },
  { key: 'board', label: 'งานข้ามบริษัท (workboard)' },
]

function fmtDate(d?: string | null) {
  return d ? d.slice(0, 10) : '—'
}

export default function TasksPage() {
  const [tab, setTab] = useState<Tab>('company')
  return (
    <div>
      <PageHeader title="งาน / มอบหมายงาน" description="งานทั่วไป (ad-hoc) ต่อบริษัท + ภาพรวมงานข้ามทุกบริษัท" />
      <Tabs items={TABS} activeKey={tab} onChange={setTab} />
      {tab === 'company' ? <CompanyTasksTab /> : <WorkboardTab />}
    </div>
  )
}

// ── แท็บ 1: งานของบริษัท ───────────────────────────────────────────────────────
function CompanyTasksTab() {
  const { companyId } = useCurrentCompany()
  const [statusFilter, setStatusFilter] = useState<number | ''>('')
  const { data: tasks, isLoading, isError } = useWorkTasks(companyId, statusFilter === '' ? null : statusFilter)
  const { data: users } = useAssignableUsers(companyId)
  const setStatus = useSetTaskStatus()
  const assign = useAssignTask()
  const del = useDeleteTask()
  const toggleItem = useToggleTaskItem()
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<WorkTaskDto | null>(null)
  const [expanded, setExpanded] = useState<number | null>(null)

  if (!companyId) return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>

  async function handleDelete(t: WorkTaskDto) {
    if (!window.confirm(`ลบงาน "${t.title}"? (บันทึก audit trail)`)) return
    await del.mutateAsync(t.id)
  }

  return (
    <div>
      <Card className="mb-4 flex flex-wrap items-center justify-between gap-3 px-6 py-4">
        <div className="flex items-end gap-3">
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">สถานะ</label>
            <select
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value === '' ? '' : Number(e.target.value))}
              className="rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
            >
              <option value="">ทั้งหมด</option>
              {STATUS_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
            </select>
          </div>
          <p className="pb-2 text-xs text-gray-500">{tasks?.length ?? 0} งาน</p>
        </div>
        <div className="flex items-center gap-2">
          {tasks && tasks.length > 0 && (
            <ExportMenu
              meta={{ title: 'งานของบริษัท', fileName: `work-tasks-${companyId}` }}
              getSections={(): ExportSection[] => [{
                name: 'งาน',
                columns: [
                  { key: 'title', header: 'ชื่องาน' },
                  { key: 'category', header: 'หมวด', value: (t) => t.category ?? '' },
                  { key: 'priorityName', header: 'ความสำคัญ' },
                  { key: 'dueDate', header: 'กำหนดส่ง', value: (t) => fmtDate(t.dueDate) },
                  { key: 'statusName', header: 'สถานะ' },
                  { key: 'assignedUserName', header: 'ผู้รับผิดชอบ', value: (t) => t.assignedUserName ?? '' },
                ],
                rows: tasks,
              }]}
            />
          )}
          <Button type="button" onClick={() => { setEditing(null); setModalOpen(true) }}>+ สร้างงาน</Button>
        </div>
      </Card>

      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}
      {tasks && tasks.length === 0 && <Card><StateMessage centered>ยังไม่มีงาน — กด "สร้างงาน"</StateMessage></Card>}

      {tasks && tasks.length > 0 && (
        <Card className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="border-b bg-slate-50 text-xs text-gray-600">
              <tr>
                <th className="px-4 py-3 text-left font-medium">ชื่องาน</th>
                <th className="px-4 py-3 text-left font-medium w-24">หมวด</th>
                <th className="px-4 py-3 text-left font-medium w-20">ความสำคัญ</th>
                <th className="px-4 py-3 text-left font-medium w-28">กำหนดส่ง</th>
                <th className="px-4 py-3 text-left font-medium w-32">สถานะ</th>
                <th className="px-4 py-3 text-left font-medium w-40">ผู้รับผิดชอบ</th>
                <th className="px-4 py-3 text-right font-medium w-28">จัดการ</th>
              </tr>
            </thead>
            <tbody>
              {tasks.map((t) => (
                <Fragment key={t.id}>
                <tr className={`border-b border-gray-100 hover:bg-slate-50 ${t.isOverdue ? 'bg-red-50/50' : ''}`}>
                  <td className="px-4 py-2.5">
                    <div className="flex items-center gap-2">
                      <span className="font-medium text-slate-800">{t.title}</span>
                      {t.recurrenceType !== 0 && (
                        <span className="rounded-full bg-violet-100 px-2 py-0.5 text-[10px] font-medium text-violet-700">🔁 {t.recurrenceName}</span>
                      )}
                    </div>
                    {t.description && <div className="text-xs text-gray-400">{t.description}</div>}
                    {t.totalCount > 0 && (
                      <button type="button" onClick={() => setExpanded((v) => (v === t.id ? null : t.id))}
                        className="mt-0.5 text-xs text-sky-600 hover:underline">
                        {expanded === t.id ? '▼' : '▶'} checklist {t.doneCount}/{t.totalCount}
                      </button>
                    )}
                  </td>
                  <td className="px-4 py-2.5 text-gray-600">{t.category ?? '—'}</td>
                  <td className="px-4 py-2.5 text-gray-600">{PRIORITY_LABEL[t.priority]}</td>
                  <td className={`px-4 py-2.5 ${t.isOverdue ? 'font-medium text-red-600' : 'text-gray-600'}`}>
                    {fmtDate(t.dueDate)}{t.isOverdue ? ' (เกิน)' : ''}
                  </td>
                  <td className="px-4 py-2.5">
                    <select
                      value={t.status}
                      onChange={(e) => setStatus.mutate({ id: t.id, status: Number(e.target.value) })}
                      className="rounded border border-gray-200 px-2 py-1 text-xs focus:outline-none focus:ring-1 focus:ring-slate-400"
                    >
                      {STATUS_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
                    </select>
                  </td>
                  <td className="px-4 py-2.5">
                    <select
                      value={t.assignedUserId ?? ''}
                      onChange={(e) => assign.mutate({ id: t.id, userId: e.target.value === '' ? null : Number(e.target.value) })}
                      className="rounded border border-gray-200 px-2 py-1 text-xs focus:outline-none focus:ring-1 focus:ring-slate-400"
                    >
                      <option value="">— ไม่ระบุ —</option>
                      {(users ?? []).map((u) => <option key={u.userId} value={u.userId}>{u.displayName}</option>)}
                    </select>
                  </td>
                  <td className="px-4 py-2.5 text-right">
                    <Button type="button" variant="ghost" onClick={() => { setEditing(t); setModalOpen(true) }} className="px-2 py-1 text-xs">แก้ไข</Button>
                    <Button type="button" variant="ghost" onClick={() => handleDelete(t)} className="px-2 py-1 text-xs text-red-500 hover:text-red-600">ลบ</Button>
                  </td>
                </tr>
                {expanded === t.id && t.totalCount > 0 && (
                  <tr className="bg-slate-50/60">
                    <td colSpan={7} className="px-6 py-3">
                      <div className="space-y-1.5">
                        {t.items.map((it) => (
                          <label key={it.id} className="flex items-center gap-2 text-sm text-slate-700">
                            <input type="checkbox" checked={it.isDone}
                              onChange={(e) => toggleItem.mutate({ taskId: t.id, itemId: it.id, isDone: e.target.checked })}
                              className="rounded" />
                            <span className={it.isDone ? 'text-slate-400 line-through' : ''}>{it.text}</span>
                          </label>
                        ))}
                      </div>
                    </td>
                  </tr>
                )}
                </Fragment>
              ))}
            </tbody>
          </table>
        </Card>
      )}

      {modalOpen && <TaskFormModal companyId={companyId} editing={editing} onClose={() => setModalOpen(false)} />}
    </div>
  )
}

// ── แท็บ 2: workboard ข้ามบริษัท ─────────────────────────────────────────────
function WorkboardTab() {
  const { user } = useAuth()
  const sendReminders = useSendReminders()
  const [mineOnly, setMineOnly] = useState(false)

  async function handleSendReminders() {
    if (!window.confirm('ส่งอีเมลเตือนงานค้าง/ใกล้ครบกำหนดให้ผู้รับผิดชอบ (ทุกบริษัท)?')) return
    try {
      const r = await sendReminders.mutateAsync(3)
      alert(`ส่งสำเร็จ ${r.sent} · ข้าม ${r.skipped} · ล้มเหลว ${r.failed}\n${r.messages.join('\n')}`)
    } catch (e) {
      const msg = (e as { response?: { data?: { detail?: string; title?: string } } })?.response?.data
      alert(msg?.detail ?? msg?.title ?? 'ส่งไม่สำเร็จ')
    }
  }
  const [openOnly, setOpenOnly] = useState(true)
  const [includeCompliance, setIncludeCompliance] = useState(true)
  const [dueBefore, setDueBefore] = useState('')

  const { data: items, isLoading, isError } = useWorkboard({
    assignedUserId: mineOnly ? user?.userId ?? null : null,
    openOnly,
    includeCompliance,
    dueBefore: dueBefore ? `${dueBefore}T00:00:00` : null,
  })

  return (
    <div>
      <Card className="mb-4 flex flex-wrap items-center justify-between gap-3 px-6 py-4">
        <div className="flex flex-wrap items-center gap-4">
          <label className="flex items-center gap-2 text-sm text-gray-600">
            <input type="checkbox" checked={mineOnly} onChange={(e) => setMineOnly(e.target.checked)} className="rounded" />
            เฉพาะของฉัน
          </label>
          <label className="flex items-center gap-2 text-sm text-gray-600">
            <input type="checkbox" checked={openOnly} onChange={(e) => setOpenOnly(e.target.checked)} className="rounded" />
            เฉพาะที่ยังไม่เสร็จ
          </label>
          <label className="flex items-center gap-2 text-sm text-gray-600">
            <input type="checkbox" checked={includeCompliance} onChange={(e) => setIncludeCompliance(e.target.checked)} className="rounded" />
            รวมงานภาษี (compliance)
          </label>
          <div className="flex items-center gap-2">
            <span className="text-xs text-gray-500">ครบกำหนดก่อน</span>
            <input
              type="date" value={dueBefore} onChange={(e) => setDueBefore(e.target.value)}
              className="rounded border border-gray-300 px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
            />
          </div>
        </div>
        <div className="flex items-center gap-2">
          <p className="text-xs text-gray-500">{items?.length ?? 0} รายการ</p>
          {user?.role === 'Admin' && (
            <Button type="button" variant="ghost" onClick={handleSendReminders} disabled={sendReminders.isPending} className="text-xs">
              {sendReminders.isPending ? 'กำลังส่ง...' : '✉ ส่งอีเมลเตือน'}
            </Button>
          )}
          {items && items.length > 0 && (
            <ExportMenu
              meta={{ title: 'งานข้ามบริษัท', fileName: 'workboard' }}
              getSections={(): ExportSection[] => [{
                name: 'งาน',
                columns: [
                  { key: 'source', header: 'ประเภท', value: (i: WorkItemDto) => (i.source === 'Task' ? 'งานทั่วไป' : 'งานภาษี') },
                  { key: 'clientName', header: 'บริษัท' },
                  { key: 'title', header: 'งาน' },
                  { key: 'dueDate', header: 'กำหนดส่ง', value: (i: WorkItemDto) => fmtDate(i.dueDate) },
                  { key: 'statusName', header: 'สถานะ' },
                  { key: 'assignedUserName', header: 'ผู้รับผิดชอบ', value: (i: WorkItemDto) => i.assignedUserName ?? '' },
                ],
                rows: items,
              }]}
            />
          )}
        </div>
      </Card>

      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}
      {items && items.length === 0 && <Card><StateMessage centered>ไม่มีงานตามเงื่อนไข</StateMessage></Card>}

      {items && items.length > 0 && (
        <Card className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="border-b bg-slate-50 text-xs text-gray-600">
              <tr>
                <th className="px-4 py-3 text-left font-medium w-24">ประเภท</th>
                <th className="px-4 py-3 text-left font-medium">บริษัท</th>
                <th className="px-4 py-3 text-left font-medium">งาน</th>
                <th className="px-4 py-3 text-left font-medium w-28">กำหนดส่ง</th>
                <th className="px-4 py-3 text-left font-medium w-28">สถานะ</th>
                <th className="px-4 py-3 text-left font-medium w-36">ผู้รับผิดชอบ</th>
              </tr>
            </thead>
            <tbody>
              {items.map((i) => (
                <tr key={`${i.source}-${i.id}`} className={`border-b border-gray-100 hover:bg-slate-50 ${i.isOverdue ? 'bg-red-50/50' : ''}`}>
                  <td className="px-4 py-2.5">
                    <StatusBadge tone={i.source === 'Task' ? 'blue' : 'yellow'}>
                      {i.source === 'Task' ? 'งานทั่วไป' : 'ภาษี'}
                    </StatusBadge>
                  </td>
                  <td className="px-4 py-2.5 text-gray-700">{i.clientName}</td>
                  <td className="px-4 py-2.5 text-slate-800">{i.title}</td>
                  <td className={`px-4 py-2.5 ${i.isOverdue ? 'font-medium text-red-600' : 'text-gray-600'}`}>
                    {fmtDate(i.dueDate)}{i.isOverdue ? ' (เกิน)' : ''}
                  </td>
                  <td className="px-4 py-2.5">
                    <StatusBadge tone={i.source === 'Task' ? STATUS_TONE[i.status] ?? 'gray' : (i.isOverdue ? 'red' : 'gray')}>
                      {i.statusName}
                    </StatusBadge>
                  </td>
                  <td className="px-4 py-2.5 text-gray-600">{i.assignedUserName ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}
    </div>
  )
}
