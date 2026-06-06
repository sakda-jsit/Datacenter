import { useState } from 'react'
import Button from '../../../../shared/components/ui/Button'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import { useCreatePayrollRun, useDeletePayrollRun, usePayrollRuns } from '../../hooks/usePayroll'
import { MONTH_TH, PAYROLL_RUN_STATUS_LABEL, type PayrollRunListItem } from '../../types/payroll.types'
import PayrollRunGrid from '../../components/PayrollRunGrid'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  companyId: number
}

export default function PayrollRunsTab({ companyId }: Props) {
  const [openRunId, setOpenRunId] = useState<number | null>(null)
  const { data, isLoading, isError } = usePayrollRuns(companyId)
  const create = useCreatePayrollRun(companyId)
  const del = useDeletePayrollRun(companyId)
  const now = new Date()
  const [year, setYear] = useState(now.getFullYear())
  const [month, setMonth] = useState(now.getMonth() + 1)
  const [error, setError] = useState('')

  if (openRunId) {
    return <PayrollRunGrid companyId={companyId} runId={openRunId} onBack={() => setOpenRunId(null)} />
  }

  async function createRun() {
    setError('')
    try {
      const res = await create.mutateAsync({ year, month })
      setOpenRunId(res.id)
    } catch (err) {
      const msg = (err as { response?: { data?: { detail?: string; title?: string } } })?.response?.data
      setError(msg?.detail ?? msg?.title ?? 'สร้างงวดไม่สำเร็จ')
    }
  }
  async function handleDelete(r: PayrollRunListItem) {
    if (!window.confirm(`ลบงวด ${MONTH_TH[r.month]} ${r.year}? รายการทั้งหมดในงวดจะถูกลบ`)) return
    await del.mutateAsync(r.id)
  }

  const rows = data ?? []

  return (
    <div>
      <Card className="mb-4 flex flex-wrap items-end justify-between gap-3 px-6 py-4">
        <div>
          <p className="text-sm font-semibold text-slate-800">งวดเงินเดือนรายเดือน</p>
          <p className="text-xs text-gray-500">สร้างงวด → ระบบดึงพนักงานที่ยังทำงาน แล้วกรอกตาม slip · คำนวณ ปกส./ภาษีเทียบให้</p>
        </div>
        <div className="flex items-end gap-2">
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ปี</label>
            <input type="number" value={year} onChange={(e) => setYear(Number(e.target.value))} className="w-24 rounded border border-gray-300 px-2 py-1.5 text-sm" />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">เดือน</label>
            <select value={month} onChange={(e) => setMonth(Number(e.target.value))} className="rounded border border-gray-300 px-2 py-1.5 text-sm">
              {Array.from({ length: 12 }, (_, i) => i + 1).map((m) => <option key={m} value={m}>{MONTH_TH[m]}</option>)}
            </select>
          </div>
          <Button type="button" onClick={createRun} disabled={create.isPending}>+ สร้างงวด</Button>
        </div>
      </Card>

      {error && <StateMessage tone="error">{error}</StateMessage>}
      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {data && rows.length === 0 && (
        <Card><StateMessage centered>ยังไม่มีงวดเงินเดือน — เลือกปี/เดือนแล้วกด “+ สร้างงวด”</StateMessage></Card>
      )}

      {rows.length > 0 && (
        <Card className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="border-b bg-slate-50 text-xs text-gray-600">
              <tr>
                <th className="px-4 py-2 text-left font-medium">งวด</th>
                <th className="px-4 py-2 text-center font-medium w-20">พนักงาน</th>
                <th className="px-4 py-2 text-right font-medium">รวมรายได้</th>
                <th className="px-4 py-2 text-right font-medium">หัก ปกส.</th>
                <th className="px-4 py-2 text-right font-medium">ภาษี</th>
                <th className="px-4 py-2 text-right font-medium">เงินสุทธิ</th>
                <th className="px-4 py-2 text-center font-medium w-24">สถานะ</th>
                <th className="px-4 py-2 text-right font-medium w-32">จัดการ</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => (
                <tr key={r.id} className="border-b border-gray-100 hover:bg-slate-50 cursor-pointer" onClick={() => setOpenRunId(r.id)}>
                  <td className="px-4 py-1.5 font-medium text-slate-800">{MONTH_TH[r.month]} {r.year}</td>
                  <td className="px-4 py-1.5 text-center text-gray-600">{r.employeeCount}</td>
                  <td className="px-4 py-1.5 text-right font-mono">{fmt(r.totalGross)}</td>
                  <td className="px-4 py-1.5 text-right font-mono">{fmt(r.totalSsoEmployee)}</td>
                  <td className="px-4 py-1.5 text-right font-mono">{fmt(r.totalTax)}</td>
                  <td className="px-4 py-1.5 text-right font-mono font-semibold">{fmt(r.totalNet)}</td>
                  <td className="px-4 py-1.5 text-center">
                    <span className={`rounded-full px-2 py-0.5 text-xs ${r.status === 0 ? 'bg-amber-50 text-amber-700' : r.status === 1 ? 'bg-green-50 text-green-700' : 'bg-slate-100 text-slate-600'}`}>
                      {PAYROLL_RUN_STATUS_LABEL[r.status]}
                    </span>
                  </td>
                  <td className="px-4 py-1.5 text-right" onClick={(e) => e.stopPropagation()}>
                    <div className="flex justify-end gap-1">
                      <Button type="button" variant="ghost" onClick={() => setOpenRunId(r.id)} className="px-2 py-1 text-xs">เปิด</Button>
                      <Button type="button" variant="ghost" onClick={() => handleDelete(r)} className="px-2 py-1 text-xs text-red-500 hover:text-red-600">ลบ</Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}
    </div>
  )
}
