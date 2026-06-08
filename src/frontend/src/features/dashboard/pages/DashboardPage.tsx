import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import { useWorkTracker } from '../hooks/useWorkTracker'
import {
  useComplianceDashboard,
  useComplianceTasks,
  useGenerateTasks,
  useUpdateTaskStatus,
} from '../../compliance-calendar/hooks/useCompliance'
import type { ComplianceTaskStatus } from '../../compliance-calendar/types/compliance.types'
import type { WorkTrackerCell } from '../types/workTracker.types'

const MONTH_TH = ['', 'ม.ค.', 'ก.พ.', 'มี.ค.', 'เม.ย.', 'พ.ค.', 'มิ.ย.', 'ก.ค.', 'ส.ค.', 'ก.ย.', 'ต.ค.', 'พ.ย.', 'ธ.ค.']
// คอลัมน์ประเภทงานประจำ (คงที่) + ลิงก์ไปโมดูลที่เกี่ยวข้อง
const TASK_TYPES = [
  { type: 1, short: 'ภ.พ.30', link: '/vat' },
  { type: 2, short: 'ภ.ง.ด.1', link: '/payroll?section=pnd1' },
  { type: 3, short: 'ภ.ง.ด.3', link: '/wht' },
  { type: 4, short: 'ภ.ง.ด.53', link: '/wht' },
  { type: 5, short: 'ปกส.', link: '/payroll?section=sso' },
  { type: 6, short: 'ปิดเดือน', link: '/closing-period' },
]
const TASK_LINK: Record<number, string> = Object.fromEntries(TASK_TYPES.map((t) => [t.type, t.link]))

function cellStyle(status: number, isOverdue: boolean) {
  if (status === 2) return { sym: '✓', cls: 'bg-green-100 text-green-700', title: 'เสร็จสิ้น' }
  if (isOverdue || status === 3) return { sym: '⚠', cls: 'bg-red-100 text-red-600', title: 'เกินกำหนด' }
  if (status === 1) return { sym: '●', cls: 'bg-sky-100 text-sky-700', title: 'กำลังดำเนินการ' }
  return { sym: '○', cls: 'bg-slate-100 text-slate-400', title: 'รอดำเนินการ' }
}

const nowDate = new Date()

export default function DashboardPage() {
  const { companyId, selectCompany } = useCurrentCompany()
  const [year, setYear] = useState(nowDate.getFullYear())
  const [month, setMonth] = useState(nowDate.getMonth() + 1)

  return (
    <div className="space-y-6">
      <section className="flex flex-col justify-between gap-4 rounded-[26px] border border-sky-100 bg-gradient-to-br from-white via-sky-50 to-cyan-50 p-6 shadow-[0_8px_24px_rgba(15,23,42,0.06)] lg:flex-row lg:items-center">
        <div>
          <span className="mb-2 inline-flex items-center gap-2 text-xs font-extrabold uppercase tracking-widest text-sky-700">
            <span className="h-0.5 w-6 rounded-full bg-sky-400" /> ติดตามงานประจำ
          </span>
          <h1 className="text-2xl font-extrabold leading-tight text-slate-900">
            {companyId ? 'งานประจำของบริษัทที่เลือก' : 'ภาพรวมงานประจำทุกบริษัท'}
          </h1>
          <p className="mt-1 text-sm text-slate-500">
            ภ.พ.30 · ภ.ง.ด.1/3/53 · ประกันสังคม · ปิดบัญชีประจำเดือน
            {!companyId && ' — เลือกบริษัทที่แถบด้านบนเพื่อดูรายบริษัท'}
          </p>
        </div>
        <div className="flex items-end gap-2">
          <label className="text-xs font-medium text-slate-500">
            ปี
            <select value={year} onChange={(e) => setYear(Number(e.target.value))}
              className="ml-1 block rounded-lg border border-slate-300 px-2 py-1.5 text-sm">
              {[year + 1, year, year - 1, year - 2].filter((v, i, a) => a.indexOf(v) === i).map((y) => (
                <option key={y} value={y}>{y}</option>
              ))}
            </select>
          </label>
          {!companyId && (
            <label className="text-xs font-medium text-slate-500">
              เดือน
              <select value={month} onChange={(e) => setMonth(Number(e.target.value))}
                className="ml-1 block rounded-lg border border-slate-300 px-2 py-1.5 text-sm">
                {MONTH_TH.slice(1).map((m, i) => <option key={i + 1} value={i + 1}>{m}</option>)}
              </select>
            </label>
          )}
        </div>
      </section>

      {companyId
        ? <CompanyView companyId={companyId} year={year} />
        : <AllCompaniesView year={year} month={month} onSelectCompany={selectCompany} />}
    </div>
  )
}

// ─────────────────────── โหมด A: ภาพรวมทุกบริษัท ───────────────────────
function AllCompaniesView({ year, month, onSelectCompany }: { year: number; month: number; onSelectCompany: (id: number) => void }) {
  const { data, isLoading } = useWorkTracker(year, month)

  if (isLoading) return <p className="text-sm text-slate-500">กำลังโหลด...</p>
  if (!data) return null

  const pct = data.totalTasks > 0 ? Math.round((data.completed / data.totalTasks) * 100) : 0

  return (
    <div className="space-y-5">
      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        <Kpi label={`งานเดือน ${MONTH_TH[month]} ${year}`} value={`${data.completed}/${data.totalTasks}`} sub={`เสร็จ ${pct}%`} tone="sky" />
        <Kpi label="เกินกำหนด" value={data.overdue} tone="red" />
        <Kpi label="ใกล้ครบกำหนด ≤7 วัน" value={data.dueSoon} tone="amber" />
        <Kpi label="บริษัทที่ยังมีงานค้าง" value={`${data.companiesWithOpenWork}/${data.companiesWithTasks}`} tone="indigo" />
      </div>

      {data.companiesNoTasks > 0 && (
        <div className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-2 text-sm text-amber-700">
          ⚠ มี {data.companiesNoTasks} บริษัทที่ยังไม่ได้สร้างงานเดือนนี้ — เลือกบริษัทแล้วกด “สร้างงานเดือนนี้” หรือไปที่เมนูปฏิทินงาน
        </div>
      )}

      {/* ต้องจัดการด่วน */}
      <div className="dc-card overflow-hidden">
        <div className="border-b border-slate-100 px-5 py-3 text-sm font-extrabold text-slate-800">
          🔴 ต้องจัดการด่วน ({data.needsAttention.length})
        </div>
        {data.needsAttention.length === 0 ? (
          <p className="p-5 text-sm text-slate-400">ไม่มีงานเกินกำหนดหรือใกล้ครบกำหนด 🎉</p>
        ) : (
          <div className="max-h-72 overflow-auto">
            <table className="dc-table text-sm">
              <thead><tr><th>บริษัท</th><th>งาน</th><th>กำหนด</th><th>สถานะ</th><th></th></tr></thead>
              <tbody>
                {data.needsAttention.map((a) => (
                  <tr key={a.taskId}>
                    <td className="font-medium text-slate-800">{a.clientName}</td>
                    <td className="text-slate-600">{a.taskTypeName}</td>
                    <td className="whitespace-nowrap">{a.dueDate.slice(0, 10)}</td>
                    <td>
                      <span className={`dc-pill ${a.isOverdue ? 'bg-red-50 text-red-600' : 'bg-amber-50 text-amber-700'}`}>
                        {a.isOverdue ? `เกินกำหนด ${Math.abs(a.daysToDue)} วัน` : `อีก ${a.daysToDue} วัน`}
                      </span>
                    </td>
                    <td className="text-right">
                      <button type="button" onClick={() => onSelectCompany(a.clientCompanyId)}
                        className="text-sky-600 hover:underline">เปิด →</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* ตารางงานตามบริษัท */}
      <div className="dc-card overflow-hidden">
        <div className="border-b border-slate-100 px-5 py-3 text-sm font-extrabold text-slate-800">
          📋 งานตามบริษัท — {MONTH_TH[month]} {year} <span className="font-normal text-slate-400">(คลิกบริษัทเพื่อดูรายละเอียด)</span>
        </div>
        {data.companies.length === 0 ? (
          <p className="p-5 text-sm text-slate-400">ยังไม่มีงานในเดือนนี้</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-xs">
              <thead className="bg-slate-50 text-gray-600">
                <tr>
                  <th className="px-3 py-2 text-left">บริษัท</th>
                  {TASK_TYPES.map((t) => <th key={t.type} className="px-2 py-2 text-center">{t.short}</th>)}
                  <th className="px-3 py-2 text-center">ค้าง</th>
                </tr>
              </thead>
              <tbody>
                {data.companies.map((row) => {
                  const byType = new Map<number, WorkTrackerCell>(row.cells.map((c) => [c.taskType, c]))
                  return (
                    <tr key={row.clientCompanyId} className="cursor-pointer border-t border-gray-100 hover:bg-sky-50"
                      onClick={() => onSelectCompany(row.clientCompanyId)}>
                      <td className="px-3 py-1.5 font-medium text-slate-800">{row.clientName}</td>
                      {TASK_TYPES.map((t) => {
                        const cell = byType.get(t.type)
                        if (!cell) return <td key={t.type} className="px-2 py-1.5 text-center text-slate-300">–</td>
                        const s = cellStyle(cell.status, cell.isOverdue)
                        return (
                          <td key={t.type} className="px-2 py-1.5 text-center">
                            <span className={`inline-flex h-6 w-6 items-center justify-center rounded-full ${s.cls}`} title={s.title}>{s.sym}</span>
                          </td>
                        )
                      })}
                      <td className="px-3 py-1.5 text-center font-mono">
                        {row.overdue > 0 ? <span className="text-red-600">{row.open} ({row.overdue}⚠)</span> : (row.open || '✓')}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}

// ─────────────────────── โหมด B: รายบริษัท ───────────────────────
const STATUS_NEXT: Record<number, ComplianceTaskStatus> = { 0: 1, 1: 2, 3: 1 }
const STATUS_NEXT_LABEL: Record<number, string> = { 0: 'เริ่มทำ', 1: 'ทำเสร็จ', 3: 'เริ่มทำ' }

function CompanyView({ companyId, year }: { companyId: number; year: number }) {
  const month = nowDate.getMonth() + 1
  const { data: dash, isLoading } = useComplianceDashboard(companyId, year)
  const { data: tasks } = useComplianceTasks(companyId, year, month)
  const gen = useGenerateTasks()
  const upd = useUpdateTaskStatus()

  if (isLoading) return <p className="text-sm text-slate-500">กำลังโหลด...</p>
  if (!dash) return null

  const total = dash.months.reduce((a, m) => a + m.total, 0)
  const completed = dash.months.reduce((a, m) => a + m.completed, 0)
  const pct = total > 0 ? Math.round((completed / total) * 100) : 0
  const dueSoon = dash.upcomingDueSoon.length

  return (
    <div className="space-y-5">
      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        <Kpi label={`งานปี ${year}`} value={`${completed}/${total}`} sub={`เสร็จ ${pct}%`} tone="sky" />
        <Kpi label="เกินกำหนด" value={dash.totalOverdue} tone="red" />
        <Kpi label="ใกล้ครบกำหนด ≤7 วัน" value={dueSoon} tone="amber" />
        <Kpi label="บริษัท" value={dash.clientName} tone="slate" small />
      </div>

      {/* ปฏิทินงาน 12 เดือน */}
      <div className="dc-card overflow-hidden">
        <div className="border-b border-slate-100 px-5 py-3 text-sm font-extrabold text-slate-800">ปฏิทินงานรายเดือน · ปี {year}</div>
        <div className="grid grid-cols-3 gap-2 p-4 sm:grid-cols-4 lg:grid-cols-6">
          {dash.months.map((m) => {
            const done = m.total > 0 && m.completed === m.total
            const tone = m.overdue > 0 ? 'border-red-200 bg-red-50' : done ? 'border-green-200 bg-green-50' : m.total > 0 ? 'border-sky-100 bg-white' : 'border-slate-100 bg-slate-50'
            return (
              <div key={m.month} className={`rounded-xl border px-3 py-2 ${tone} ${m.month === month ? 'ring-2 ring-sky-300' : ''}`}>
                <div className="flex items-center justify-between">
                  <span className="text-xs font-bold text-slate-700">{MONTH_TH[m.month]}</span>
                  {done && <span className="text-green-600">✓</span>}
                  {m.overdue > 0 && <span className="text-xs font-bold text-red-600">{m.overdue}⚠</span>}
                </div>
                <div className="mt-1 text-[11px] text-slate-500">{m.total > 0 ? `${m.completed}/${m.total}` : 'ไม่มีงาน'}</div>
              </div>
            )
          })}
        </div>
      </div>

      {/* งานเดือนปัจจุบัน */}
      <div className="dc-card overflow-hidden">
        <div className="flex items-center justify-between border-b border-slate-100 px-5 py-3">
          <span className="text-sm font-extrabold text-slate-800">งานเดือน {MONTH_TH[month]} {nowDate.getFullYear()}</span>
          {(!tasks || tasks.length === 0) && (
            <button type="button" onClick={() => gen.mutate({ clientCompanyId: companyId, year: nowDate.getFullYear(), month })}
              disabled={gen.isPending}
              className="rounded-lg border border-sky-200 px-3 py-1.5 text-xs font-bold text-sky-700 hover:bg-sky-50">
              {gen.isPending ? 'กำลังสร้าง...' : '+ สร้างงานเดือนนี้'}
            </button>
          )}
        </div>
        {!tasks || tasks.length === 0 ? (
          <p className="p-5 text-sm text-slate-400">ยังไม่มีงานเดือนนี้ — กด “สร้างงานเดือนนี้”</p>
        ) : (
          <ul className="divide-y divide-slate-100">
            {tasks.map((t) => {
              const s = cellStyle(t.status, t.isOverdue)
              const next = STATUS_NEXT[t.status]
              return (
                <li key={t.id} className="flex flex-wrap items-center gap-3 px-5 py-3">
                  <span className={`inline-flex h-7 w-7 items-center justify-center rounded-full ${s.cls}`} title={s.title}>{s.sym}</span>
                  <span className="min-w-0 flex-1">
                    <span className="block text-sm font-medium text-slate-800">{t.taskTypeName}</span>
                    <span className="text-xs text-slate-500">
                      ครบกำหนด {t.dueDate.slice(0, 10)}
                      {t.isOverdue && <span className="ml-1 font-bold text-red-600">· เกินกำหนด</span>}
                    </span>
                  </span>
                  <Link to={TASK_LINK[t.taskType] ?? '#'} className="text-xs font-bold text-sky-600 hover:underline">ไปทำงาน →</Link>
                  {next !== undefined && (
                    <button type="button" disabled={upd.isPending}
                      onClick={() => upd.mutate({ taskId: t.id, status: next })}
                      className="rounded-lg border border-slate-200 px-3 py-1 text-xs font-bold text-slate-600 hover:bg-slate-50">
                      {STATUS_NEXT_LABEL[t.status]}
                    </button>
                  )}
                </li>
              )
            })}
          </ul>
        )}
      </div>
    </div>
  )
}

// ─────────────────────── shared ───────────────────────
const TONE: Record<string, string> = {
  sky: 'border-sky-100 bg-sky-50 text-sky-700',
  red: 'border-red-100 bg-red-50 text-red-700',
  amber: 'border-amber-100 bg-amber-50 text-amber-700',
  indigo: 'border-indigo-100 bg-indigo-50 text-indigo-700',
  slate: 'border-slate-100 bg-slate-50 text-slate-700',
}
function Kpi({ label, value, sub, tone, small }: { label: string; value: string | number; sub?: string; tone: string; small?: boolean }) {
  return (
    <div className={`rounded-[18px] border p-4 shadow-sm ${TONE[tone]}`}>
      <div className="text-xs font-bold text-slate-500">{label}</div>
      <div className={`mt-1 font-extrabold text-slate-900 ${small ? 'truncate text-base' : 'text-3xl'}`} title={small ? String(value) : undefined}>{value}</div>
      {sub && <div className="mt-0.5 text-xs font-medium">{sub}</div>}
    </div>
  )
}
