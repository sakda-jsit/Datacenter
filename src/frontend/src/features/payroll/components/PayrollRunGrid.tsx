import { useMemo, useRef, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import Card from '../../../shared/components/ui/Card'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { payrollApi } from '../services/payrollApi'
import { useImportPayrollRun, usePayrollRun, useSetRunStatus } from '../hooks/usePayroll'
import { MONTH_TH, PAYROLL_RUN_STATUS_LABEL, type PayrollItemRow } from '../types/payroll.types'

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

  // แยกตามฝ่าย: ฝ่ายบริการก่อน ฝ่ายผลิตหลัง (ฝ่ายอื่น/ไม่ระบุต่อท้าย)
  const groups = useMemo(() => {
    if (!d) return []
    const map = new Map<string, PayrollItemRow[]>()
    for (const it of d.items) {
      const key = (it.department && it.department.trim()) || 'ไม่ระบุฝ่าย'
      const arr = map.get(key) ?? []
      arr.push(it)
      map.set(key, arr)
    }
    const rank = (name: string) =>
      /บริการ|บริหาร|สำนักงาน|ออฟฟิศ/.test(name) ? 0 : /ผลิต/.test(name) ? 1 : 2
    return [...map.entries()]
      .map(([dept, items]) => ({ dept, items }))
      .sort((a, b) => rank(a.dept) - rank(b.dept) || a.dept.localeCompare(b.dept, 'th'))
  }, [d])

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

      {groups.map((g) => (
        <DeptTable key={g.dept} dept={g.dept} items={g.items} />
      ))}

      {groups.length > 1 && (
        <Card className="overflow-x-auto">
          <table className="w-full text-xs">
            <tbody>
              <tr className="bg-slate-100 font-semibold text-slate-800">
                <td className="px-2 py-2 w-[18%]">รวมทุกฝ่าย ({d.items.length} คน)</td>
                <td className="px-2 py-2 text-right font-mono">{fmt(d.totalGross)}</td>
                <td className="px-2 py-2 text-right font-mono">{fmt(d.totalSsoEmployee)}</td>
                <td className="px-2 py-2 text-right font-mono">{fmt(d.totalTax)}</td>
                <td className="px-2 py-2 text-right font-mono">{fmt(d.totalNet)}</td>
              </tr>
              <tr className="text-[10px] text-gray-400">
                <td />
                <td className="px-2 text-right">รวมรายได้</td>
                <td className="px-2 text-right">หัก ปกส.</td>
                <td className="px-2 text-right">ภาษี</td>
                <td className="px-2 text-right">สุทธิ</td>
              </tr>
            </tbody>
          </table>
        </Card>
      )}
      <p className="mt-2 text-xs text-gray-400">
        คอลัมน์จัดกลุ่มตาม sheet “รายได้ทั้งปี” · คอลัมน์ <span className="text-sky-700">สีฟ้า</span> = ระบบคำนวณเทียบ (ปกส.คำนวณ/นายจ้างสมทบ) · ช่องแดง = ปกส.จริงต่างจากที่คำนวณ ควรตรวจ
      </p>
    </div>
  )
}

// ── คอลัมน์ต่อพนักงาน (จัดกลุ่มตาม sheet รายได้ทั้งปี) ───────────────────────────
const DEPT_GROUPS: { label: string; span: number; cls: string }[] = [
  { label: 'รายได้', span: 11, cls: 'bg-emerald-50 text-emerald-800' },
  { label: 'กรอกในแบบ กท.20 ก', span: 2, cls: 'bg-violet-50 text-violet-800' },
  { label: 'รายการหัก', span: 7, cls: 'bg-rose-50 text-rose-800' },
  { label: 'รวม', span: 4, cls: 'bg-slate-100 text-slate-800' },
]

type DeptCol = {
  label: string
  val: (i: PayrollItemRow) => number
  strong?: boolean
  calc?: boolean
  groupStart?: boolean
  ssoCheck?: boolean // ไฮไลต์แดงเมื่อ ปกส.จริงต่างจากคำนวณ
}

const DEPT_COLS: DeptCol[] = [
  // รายได้
  { label: 'เงินเดือน', val: (i) => i.salary, groupStart: true },
  { label: 'ค่าจ้างวัน', val: (i) => i.dailyWageDays * i.dailyWageRate },
  { label: 'หยุดงาน+มาสาย', val: (i) => i.absence },
  { label: 'เงินเดือนสุทธิ', val: (i) => i.salary - i.absence },
  { label: 'ค่าที่พักอาศัย', val: (i) => i.housingAllowance },
  { label: 'ค่าอาหาร', val: (i) => i.foodAllowance },
  { label: 'ค่าล่วงเวลา', val: (i) => i.overtime },
  { label: 'เบี้ยขยัน', val: (i) => i.diligence },
  { label: 'โบนัส', val: (i) => i.bonus },
  { label: 'รายได้สุทธิหลังหักลา (ภงด.1ก)', val: (i) => i.grossIncome - i.absence },
  { label: 'รวมรายได้ทั้งหมด', val: (i) => i.grossIncome, strong: true },
  // กท.20ก
  { label: 'ค่าจ้าง', val: (i) => i.ssoWageBase, groupStart: true },
  { label: 'ส่วนที่เกิน 20,000', val: (i) => Math.max(i.ssoWageBase - 20000, 0) },
  // รายการหัก
  { label: 'รายได้ยื่นปกส.', val: (i) => i.ssoWageBase, groupStart: true },
  { label: 'คำนวณหักปกส.ได้', val: (i) => i.ssoEmployeeCalc, calc: true },
  { label: 'ผลต่าง (ขาดไป)', val: (i) => Math.max(i.ssoEmployeeCalc - i.ssoEmployee, 0), calc: true },
  { label: 'หักปกส.จริง', val: (i) => i.ssoEmployee, ssoCheck: true },
  { label: 'TAX', val: (i) => i.withholdingTax },
  { label: 'ขาดงาน', val: (i) => i.absence },
  { label: 'เบิกล่วงหน้า', val: (i) => i.advance },
  // รวม
  { label: 'รวมรายการหัก', val: (i) => i.absence + i.ssoEmployee + i.withholdingTax + i.advance + i.otherDeduction, strong: true, groupStart: true },
  { label: 'รายได้ยื่น ภงด.1', val: (i) => i.grossIncome - i.absence },
  { label: 'นายจ้างสมทบ', val: (i) => i.ssoEmployerCalc, calc: true },
  { label: 'เงินสุทธิ', val: (i) => i.netPay, strong: true },
]

function DeptTable({ dept, items }: { dept: string; items: PayrollItemRow[] }) {
  return (
    <div className="mb-4">
      <div className="mb-1.5 flex items-baseline gap-2">
        <h3 className="text-sm font-semibold text-slate-800">{dept}</h3>
        <span className="text-xs text-gray-500">{items.length} คน</span>
      </div>
      <Card className="overflow-x-auto">
        <table className="w-full text-[11px]">
          <thead>
            <tr>
              <th rowSpan={2} className="sticky left-0 z-10 border-b border-r bg-white px-2 py-2 text-left align-bottom font-medium text-gray-600">
                พนักงาน
              </th>
              {DEPT_GROUPS.map((g) => (
                <th key={g.label} colSpan={g.span} className={`border-b border-l px-2 py-1 text-center font-semibold ${g.cls}`}>
                  {g.label}
                </th>
              ))}
            </tr>
            <tr className="bg-slate-50 text-gray-600">
              {DEPT_COLS.map((c) => (
                <th key={c.label} className={`border-b px-2 py-2 text-right align-bottom font-medium ${c.groupStart ? 'border-l' : ''}`}>
                  {c.label}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {items.map((it) => {
              const ssoMismatch = Math.abs(it.ssoDiff) > 0.01
              return (
                <tr key={it.id} className="border-b border-gray-100 hover:bg-slate-50">
                  <td className="sticky left-0 z-10 whitespace-nowrap border-r bg-white px-2 py-1">
                    <span className="text-gray-800">{it.employeeName}</span>
                    <span className="block font-mono text-[10px] text-gray-400">{it.employeeCode}</span>
                  </td>
                  {DEPT_COLS.map((c) => {
                    const v = c.val(it)
                    const tone = c.ssoCheck && ssoMismatch
                      ? 'bg-red-50 text-red-600'
                      : c.calc ? 'text-sky-700'
                      : c.strong ? 'font-semibold text-slate-800 bg-slate-50'
                      : 'text-gray-700'
                    return (
                      <td key={c.label} className={`whitespace-nowrap px-2 py-1 text-right font-mono ${tone} ${c.groupStart ? 'border-l border-gray-100' : ''}`}
                        title={c.ssoCheck && ssoMismatch ? 'ต่างจากที่คำนวณ' : undefined}>
                        {v ? fmt(v) : '-'}
                      </td>
                    )
                  })}
                </tr>
              )
            })}
          </tbody>
          <tfoot>
            <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold text-slate-800">
              <td className="sticky left-0 z-10 whitespace-nowrap border-r bg-slate-50 px-2 py-2">รวม {dept}</td>
              {DEPT_COLS.map((c) => {
                const total = items.reduce((a, i) => a + c.val(i), 0)
                return (
                  <td key={c.label} className={`whitespace-nowrap px-2 py-2 text-right font-mono ${c.groupStart ? 'border-l border-gray-200' : ''}`}>
                    {total ? fmt(total) : '-'}
                  </td>
                )
              })}
            </tr>
          </tfoot>
        </table>
      </Card>
    </div>
  )
}
