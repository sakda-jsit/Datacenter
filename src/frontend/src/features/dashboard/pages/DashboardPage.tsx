import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import apiClient from '../../../shared/services/apiClient'

interface DashboardSummary {
  totalClients: number
  activeClients: number
  pendingComplianceTasks: number
  overdueComplianceTasks: number
  importBatchesThisMonth: number
  recentClients: { id: number; code: string; name: string; isActive: boolean }[]
}

const modules = [
  { label: 'Client Management', desc: 'ข้อมูลบริษัทลูกค้าและสิทธิ์ผู้ใช้', to: '/clients', icon: 'CL' },
  { label: 'Import Data', desc: 'นำเข้า Express, Excel และ CSV', to: '/import', icon: 'IM' },
  { label: 'Trial Balance', desc: 'ตรวจยอดยกมาและ movement', to: '/trial-balance', icon: 'TB' },
  { label: 'Financial Statement', desc: 'งบการเงินและ mapping', to: '/financial-statement', icon: 'FS' },
]

export default function DashboardPage() {
  const [data, setData] = useState<DashboardSummary | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    apiClient.get<DashboardSummary>('/dashboard')
      .then(res => setData(res.data))
      .finally(() => setLoading(false))
  }, [])

  if (loading) {
    return <div className="text-sm text-slate-500">กำลังโหลด...</div>
  }

  const cards = [
    { label: 'ลูกค้าทั้งหมด', value: data?.totalClients ?? 0, tone: 'border-sky-100 bg-sky-50 text-sky-700' },
    { label: 'ลูกค้าที่ใช้งาน', value: data?.activeClients ?? 0, tone: 'border-green-100 bg-green-50 text-green-700' },
    { label: 'งาน Compliance ที่รอ', value: data?.pendingComplianceTasks ?? 0, tone: 'border-amber-100 bg-amber-50 text-amber-700' },
    { label: 'งาน Compliance เกินกำหนด', value: data?.overdueComplianceTasks ?? 0, tone: 'border-red-100 bg-red-50 text-red-700' },
    { label: 'Import เดือนนี้', value: data?.importBatchesThisMonth ?? 0, tone: 'border-indigo-100 bg-indigo-50 text-indigo-700' },
  ]

  return (
    <div className="space-y-6">
      <section className="flex flex-col justify-between gap-6 rounded-[26px] border border-sky-100 bg-gradient-to-br from-white via-sky-50 to-cyan-50 p-6 shadow-[0_8px_24px_rgba(15,23,42,0.06)] lg:flex-row lg:items-stretch">
        <div>
          <span className="mb-2 inline-flex items-center gap-2 text-xs font-extrabold uppercase tracking-widest text-sky-700">
            <span className="h-0.5 w-6 rounded-full bg-sky-400" />
            Dashboard
          </span>
          <h1 className="text-3xl font-extrabold leading-tight text-slate-900">ระบบสำนักงานบัญชี</h1>
          <p className="mt-2 max-w-2xl text-sm text-slate-500">
            ภาพรวมลูกค้า งานนำเข้า ภาษี และกำหนดส่งงานรายเดือนสำหรับหลายบริษัท
          </p>
        </div>
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:min-w-[430px]">
          {cards.slice(0, 3).map((card) => (
            <div key={card.label} className={`rounded-[18px] border bg-white/80 p-4 text-center ${card.tone}`}>
              <span className="block text-xs font-bold text-slate-500">{card.label}</span>
              <strong className="mt-1 block text-3xl font-extrabold text-slate-900">{card.value}</strong>
            </div>
          ))}
        </div>
      </section>

      <div className="grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-5">
        {cards.map(card => (
          <div key={card.label} className={`rounded-[18px] border p-4 shadow-sm ${card.tone}`}>
            <div className="text-3xl font-extrabold">{card.value}</div>
            <div className="mt-1 text-sm font-medium">{card.label}</div>
          </div>
        ))}
      </div>

      <section>
        <div className="mb-4 flex items-center justify-between gap-4">
          <h2 className="text-lg font-extrabold text-slate-900">โมดูลใช้งานบ่อย</h2>
        </div>
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {modules.map((module) => (
            <Link
              key={module.to}
              to={module.to}
              className="dc-card group flex min-h-[136px] gap-4 p-5 text-slate-900 no-underline transition hover:-translate-y-1 hover:border-sky-100 hover:shadow-[0_18px_45px_rgba(15,23,42,0.08)]"
            >
              <span className="grid h-12 w-12 min-w-12 place-items-center rounded-2xl border border-sky-100 bg-gradient-to-br from-sky-50 to-white text-sm font-extrabold text-sky-700">
                {module.icon}
              </span>
              <span className="min-w-0">
                <strong className="block text-base font-extrabold">{module.label}</strong>
                <span className="mt-1 block text-sm leading-6 text-slate-500">{module.desc}</span>
                <span className="mt-3 block text-xs font-extrabold text-sky-600 transition group-hover:translate-x-1">เปิดโมดูล →</span>
              </span>
            </Link>
          ))}
        </div>
      </section>

      <div className="dc-card overflow-hidden">
        <div className="border-b border-slate-100 px-5 py-4">
          <h2 className="font-extrabold text-slate-800">ลูกค้าล่าสุด</h2>
        </div>
        {data?.recentClients.length === 0 ? (
          <p className="p-5 text-sm text-slate-400">ยังไม่มีข้อมูลลูกค้า</p>
        ) : (
          <table className="dc-table">
            <thead>
              <tr>
                <th>รหัส</th>
                <th>ชื่อบริษัท</th>
                <th>สถานะ</th>
              </tr>
            </thead>
            <tbody>
              {data?.recentClients.map(c => (
                <tr key={c.id}>
                  <td className="font-mono font-semibold text-slate-600">{c.code}</td>
                  <td className="font-medium text-slate-800">{c.name}</td>
                  <td>
                    <span className={`dc-pill ${c.isActive ? 'bg-green-50 text-green-700' : 'bg-slate-100 text-slate-500'}`}>
                      {c.isActive ? 'ใช้งาน' : 'ปิดใช้งาน'}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}
