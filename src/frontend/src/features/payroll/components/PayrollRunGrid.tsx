import { useRef, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import Card from '../../../shared/components/ui/Card'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { payrollApi } from '../services/payrollApi'
import { useImportPayrollRun, usePayrollRun, useSetRunStatus } from '../hooks/usePayroll'
import { MONTH_TH, PAYROLL_RUN_STATUS_LABEL } from '../types/payroll.types'

interface Props {
  companyId: number
  runId: number
  onBack: () => void
}

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

export default function PayrollRunGrid({ companyId, runId, onBack }: Props) {
  const { data: d, isLoading } = usePayrollRun(companyId, runId)
  const importRun = useImportPayrollRun(companyId, runId)
  const setStatus = useSetRunStatus(companyId)
  const fileRef = useRef<HTMLInputElement>(null)
  const [msg, setMsg] = useState('')
  const [error, setError] = useState('')

  async function download() {
    setMsg(''); setError('')
    const blob = await payrollApi.downloadTemplate(runId, companyId)
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `payroll-run-${runId}.xlsx`
    a.click()
    setTimeout(() => URL.revokeObjectURL(url), 30000)
  }

  async function onFile(e: React.ChangeEvent<HTMLInputElement>) {
    setMsg(''); setError('')
    const file = e.target.files?.[0]
    e.target.value = ''
    if (!file) return
    try {
      const res = await importRun.mutateAsync(file)
      setMsg(`อัปโหลดสำเร็จ — อัปเดต ${res.updated} รายการ`)
    } catch (err) {
      const m = (err as { response?: { data?: { detail?: string; title?: string } } })?.response?.data
      setError(m?.detail ?? m?.title ?? 'อัปโหลดไม่สำเร็จ')
    }
  }

  if (isLoading || !d) return <StateMessage>กำลังโหลด...</StateMessage>

  const empty = d.items.every((i) => i.grossIncome === 0)

  return (
    <div>
      <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-3">
          <Button type="button" variant="secondary" onClick={onBack} className="px-3 py-1">← กลับ</Button>
          <div>
            <p className="text-sm font-semibold text-slate-800">งวด {MONTH_TH[d.month]} {d.year}</p>
            <p className="text-xs text-gray-500">
              {d.items.length} คน · อัตรา ปกส. {d.rateSsoEmployeePct ?? '—'}% (เพดาน {(d.rateWageCap ?? 0).toLocaleString()}) · สถานะ {PAYROLL_RUN_STATUS_LABEL[d.status]}
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <input ref={fileRef} type="file" accept=".xlsx" onChange={onFile} className="hidden" />
          <Button type="button" variant="secondary" onClick={download}>⬇ ดาวน์โหลด Template</Button>
          <Button type="button" onClick={() => fileRef.current?.click()} disabled={importRun.isPending}>
            {importRun.isPending ? 'กำลังอัปโหลด...' : '⬆ อัปโหลดไฟล์'}
          </Button>
          {d.status === 0 && <Button type="button" variant="secondary" onClick={() => setStatus.mutate({ runId, status: 1 })}>บันทึกแล้ว</Button>}
          {d.status === 1 && <Button type="button" variant="secondary" onClick={() => setStatus.mutate({ runId, status: 0 })}>กลับเป็นร่าง</Button>}
        </div>
      </div>

      <div className="mb-3 rounded-lg border border-sky-200 bg-sky-50 px-4 py-2 text-xs text-sky-800">
        📋 กรอกข้อมูลรายได้/รายการหักใน <b>Template Excel</b> (ดาวน์โหลด → กรอก → อัปโหลด) — แก้ไขข้อมูลทำได้โดย<b>อัปโหลดไฟล์ใหม่ทับ</b> (คอลัมน์รหัสพนักงานใช้จับคู่ ห้ามแก้)
      </div>
      {msg && <StateMessage tone="success">{msg}</StateMessage>}
      {error && <StateMessage tone="error">{error}</StateMessage>}
      {empty && !msg && (
        <Card className="mb-3"><StateMessage centered>ยังไม่มีข้อมูล — ดาวน์โหลด Template กรอกแล้วอัปโหลด</StateMessage></Card>
      )}

      <Card className="overflow-x-auto">
        <table className="w-full text-xs">
          <thead className="border-b bg-slate-50 text-gray-600">
            <tr>
              <th className="px-2 py-2 text-left font-medium">พนักงาน</th>
              <th className="px-2 py-2 text-right font-medium">เงินเดือน</th>
              <th className="px-2 py-2 text-right font-medium">ค่าจ้างวัน</th>
              <th className="px-2 py-2 text-right font-medium">OT</th>
              <th className="px-2 py-2 text-right font-medium">เบี้ยขยัน</th>
              <th className="px-2 py-2 text-right font-medium">โบนัส</th>
              <th className="px-2 py-2 text-right font-medium bg-slate-100">รวมรายได้</th>
              <th className="px-2 py-2 text-right font-medium">ฐาน ปกส.</th>
              <th className="px-2 py-2 text-right font-medium">หัก ปกส.</th>
              <th className="px-2 py-2 text-right font-medium text-sky-700">ปกส.คำนวณ</th>
              <th className="px-2 py-2 text-right font-medium">ภาษี</th>
              <th className="px-2 py-2 text-right font-medium text-sky-700">ภาษีคำนวณ</th>
              <th className="px-2 py-2 text-right font-medium">ขาดงาน</th>
              <th className="px-2 py-2 text-right font-medium bg-slate-100">สุทธิ</th>
            </tr>
          </thead>
          <tbody>
            {d.items.map((it) => {
              const ssoMismatch = Math.abs(it.ssoDiff) > 0.01
              const taxMismatch = Math.abs(it.taxDiff) > 0.01
              return (
                <tr key={it.id} className="border-b border-gray-100">
                  <td className="px-2 py-1 whitespace-nowrap">
                    <span className="text-gray-800">{it.employeeName}</span>
                    <span className="block font-mono text-[10px] text-gray-400">{it.employeeCode}</span>
                  </td>
                  <td className="px-2 py-1 text-right font-mono">{fmt(it.salary)}</td>
                  <td className="px-2 py-1 text-right font-mono">{fmt(it.dailyWageDays * it.dailyWageRate)}</td>
                  <td className="px-2 py-1 text-right font-mono">{fmt(it.overtime)}</td>
                  <td className="px-2 py-1 text-right font-mono">{fmt(it.diligence)}</td>
                  <td className="px-2 py-1 text-right font-mono">{fmt(it.bonus)}</td>
                  <td className="px-2 py-1 text-right font-mono font-semibold bg-slate-50">{fmt(it.grossIncome)}</td>
                  <td className="px-2 py-1 text-right font-mono">{fmt(it.ssoWageBase)}</td>
                  <td className="px-2 py-1 text-right font-mono">{fmt(it.ssoEmployee)}</td>
                  <td className={`px-2 py-1 text-right font-mono ${ssoMismatch ? 'bg-red-50 text-red-600' : 'text-gray-400'}`} title={ssoMismatch ? 'ต่างจากที่กรอก' : 'ตรง'}>{fmt(it.ssoEmployeeCalc)}</td>
                  <td className="px-2 py-1 text-right font-mono">{fmt(it.withholdingTax)}</td>
                  <td className={`px-2 py-1 text-right font-mono ${taxMismatch ? 'bg-amber-50 text-amber-700' : 'text-gray-400'}`} title="ประมาณการ">{fmt(it.taxCalc)}</td>
                  <td className="px-2 py-1 text-right font-mono">{fmt(it.absence)}</td>
                  <td className="px-2 py-1 text-right font-mono font-semibold bg-slate-50">{fmt(it.netPay)}</td>
                </tr>
              )
            })}
          </tbody>
          <tfoot>
            <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
              <td className="px-2 py-2">รวม</td>
              <td colSpan={5} />
              <td className="px-2 py-2 text-right font-mono">{fmt(d.totalGross)}</td>
              <td colSpan={2} className="px-2 py-2 text-right font-mono">{fmt(d.totalSsoEmployee)}</td>
              <td />
              <td className="px-2 py-2 text-right font-mono">{fmt(d.totalTax)}</td>
              <td colSpan={2} />
              <td className="px-2 py-2 text-right font-mono">{fmt(d.totalNet)}</td>
            </tr>
          </tfoot>
        </table>
      </Card>
      <p className="mt-2 text-xs text-gray-400">คอลัมน์ "ปกส./ภาษีคำนวณ" = ระบบคำนวณเทียบกับที่กรอก (แดง/เหลือง = ต่างกัน ควรตรวจ)</p>
    </div>
  )
}
