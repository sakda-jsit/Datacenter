import { useEffect, useMemo, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import Card from '../../../shared/components/ui/Card'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { usePayrollRun, useSavePayrollItems, useSetRunStatus } from '../hooks/usePayroll'
import { MONTH_TH, PAYROLL_RUN_STATUS_LABEL, type PayrollItemInput, type PayrollRunDetail } from '../types/payroll.types'

interface Props {
  companyId: number
  runId: number
  onBack: () => void
}

type Editable = Omit<PayrollItemInput, 'note'> & { note?: string | null }

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

export default function PayrollRunGrid({ companyId, runId, onBack }: Props) {
  const { data, isLoading } = usePayrollRun(companyId, runId)
  const save = useSavePayrollItems(companyId, runId)
  const setStatus = useSetRunStatus(companyId)
  const [rows, setRows] = useState<Record<number, Editable>>({})

  useEffect(() => {
    if (data) {
      const m: Record<number, Editable> = {}
      for (const it of data.items) {
        m[it.id] = {
          id: it.id, salary: it.salary, dailyWageDays: it.dailyWageDays, dailyWageRate: it.dailyWageRate,
          housingAllowance: it.housingAllowance, foodAllowance: it.foodAllowance, overtime: it.overtime,
          diligence: it.diligence, bonus: it.bonus, otherIncome: it.otherIncome, ssoWageBase: it.ssoWageBase,
          ssoEmployee: it.ssoEmployee, withholdingTax: it.withholdingTax, absence: it.absence,
          otherDeduction: it.otherDeduction, note: it.note,
        }
      }
      setRows(m)
    }
  }, [data])

  const floor = data?.rateWageFloor ?? 1650
  const cap = data?.rateWageCap ?? 15000
  const empPct = data?.rateSsoEmployeePct ?? 5

  function set(id: number, k: keyof Editable, v: number) {
    setRows((p) => ({ ...p, [id]: { ...p[id], [k]: v } }))
  }
  function grossOf(r: Editable) {
    return r.salary + r.dailyWageDays * r.dailyWageRate + r.housingAllowance + r.foodAllowance
      + r.overtime + r.diligence + r.bonus + r.otherIncome
  }
  function ssoCalcOf(r: Editable) {
    const b = r.ssoWageBase <= 0 ? 0 : Math.min(Math.max(r.ssoWageBase, floor), cap)
    return Math.round((b * empPct) / 100 * 100) / 100
  }
  function netOf(r: Editable) {
    return grossOf(r) - r.absence - r.ssoEmployee - r.withholdingTax - r.otherDeduction
  }

  const totals = useMemo(() => {
    const arr = Object.values(rows)
    return {
      gross: arr.reduce((s, r) => s + grossOf(r), 0),
      sso: arr.reduce((s, r) => s + r.ssoEmployee, 0),
      tax: arr.reduce((s, r) => s + r.withholdingTax, 0),
      net: arr.reduce((s, r) => s + netOf(r), 0),
    }
  }, [rows]) // eslint-disable-line react-hooks/exhaustive-deps

  if (isLoading || !data) return <StateMessage>กำลังโหลด...</StateMessage>

  async function handleSave() {
    await save.mutateAsync(Object.values(rows).map((r) => ({ ...r })))
  }

  const d: PayrollRunDetail = data
  const N = (id: number, k: keyof Editable) => (
    <input
      type="number" value={(rows[id]?.[k] as number) ?? 0}
      onChange={(e) => set(id, k, Number(e.target.value))}
      className="w-20 rounded border border-gray-200 px-1 py-0.5 text-right text-xs focus:border-slate-400 focus:outline-none"
    />
  )

  return (
    <div>
      <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-3">
          <Button type="button" variant="secondary" onClick={onBack} className="px-3 py-1">← กลับ</Button>
          <div>
            <p className="text-sm font-semibold text-slate-800">งวด {MONTH_TH[d.month]} {d.year}</p>
            <p className="text-xs text-gray-500">
              {d.items.length} คน · อัตรา ปกส. {d.rateSsoEmployeePct ?? '—'}% (เพดาน {cap.toLocaleString()}) · สถานะ {PAYROLL_RUN_STATUS_LABEL[d.status]}
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          {d.status === 0 && <Button type="button" variant="secondary" onClick={() => setStatus.mutate({ runId, status: 1 })}>ทำเครื่องหมายบันทึกแล้ว</Button>}
          {d.status === 1 && <Button type="button" variant="secondary" onClick={() => setStatus.mutate({ runId, status: 0 })}>กลับเป็นร่าง</Button>}
          <Button type="button" onClick={handleSave} disabled={save.isPending}>{save.isPending ? 'กำลังบันทึก...' : 'บันทึกงวด'}</Button>
        </div>
      </div>

      <Card className="overflow-x-auto">
        <table className="w-full text-xs">
          <thead className="border-b bg-slate-50 text-gray-600">
            <tr>
              <th className="px-2 py-2 text-left font-medium">พนักงาน</th>
              <th className="px-2 py-2 text-right font-medium">เงินเดือน</th>
              <th className="px-2 py-2 text-right font-medium">วัน</th>
              <th className="px-2 py-2 text-right font-medium">เรท/วัน</th>
              <th className="px-2 py-2 text-right font-medium">ค่าที่พัก</th>
              <th className="px-2 py-2 text-right font-medium">ค่าอาหาร</th>
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
              const r = rows[it.id]
              if (!r) return null
              const ssoCalc = ssoCalcOf(r)
              const ssoMismatch = Math.abs(r.ssoEmployee - ssoCalc) > 0.01
              const taxMismatch = Math.abs(it.taxDiff) > 0.01
              return (
                <tr key={it.id} className="border-b border-gray-100">
                  <td className="px-2 py-1 whitespace-nowrap">
                    <span className="text-gray-800">{it.employeeName}</span>
                    <span className="block font-mono text-[10px] text-gray-400">{it.employeeCode}</span>
                  </td>
                  <td className="px-2 py-1 text-right">{N(it.id, 'salary')}</td>
                  <td className="px-2 py-1 text-right">{N(it.id, 'dailyWageDays')}</td>
                  <td className="px-2 py-1 text-right">{N(it.id, 'dailyWageRate')}</td>
                  <td className="px-2 py-1 text-right">{N(it.id, 'housingAllowance')}</td>
                  <td className="px-2 py-1 text-right">{N(it.id, 'foodAllowance')}</td>
                  <td className="px-2 py-1 text-right">{N(it.id, 'overtime')}</td>
                  <td className="px-2 py-1 text-right">{N(it.id, 'diligence')}</td>
                  <td className="px-2 py-1 text-right">{N(it.id, 'bonus')}</td>
                  <td className="px-2 py-1 text-right font-mono font-semibold bg-slate-50">{fmt(grossOf(r))}</td>
                  <td className="px-2 py-1 text-right">{N(it.id, 'ssoWageBase')}</td>
                  <td className="px-2 py-1 text-right">{N(it.id, 'ssoEmployee')}</td>
                  <td className={`px-2 py-1 text-right font-mono ${ssoMismatch ? 'bg-red-50 text-red-600' : 'text-gray-400'}`} title={ssoMismatch ? 'ต่างจากที่กรอก' : 'ตรง'}>{fmt(ssoCalc)}</td>
                  <td className="px-2 py-1 text-right">{N(it.id, 'withholdingTax')}</td>
                  <td className={`px-2 py-1 text-right font-mono ${taxMismatch ? 'bg-amber-50 text-amber-700' : 'text-gray-400'}`} title="ประมาณการ (อัปเดตเมื่อบันทึก)">{fmt(it.taxCalc)}</td>
                  <td className="px-2 py-1 text-right">{N(it.id, 'absence')}</td>
                  <td className="px-2 py-1 text-right font-mono font-semibold bg-slate-50">{fmt(netOf(r))}</td>
                </tr>
              )
            })}
          </tbody>
          <tfoot>
            <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
              <td className="px-2 py-2">รวม</td>
              <td colSpan={8} />
              <td className="px-2 py-2 text-right font-mono">{fmt(totals.gross)}</td>
              <td />
              <td className="px-2 py-2 text-right font-mono">{fmt(totals.sso)}</td>
              <td />
              <td className="px-2 py-2 text-right font-mono">{fmt(totals.tax)}</td>
              <td />
              <td />
              <td className="px-2 py-2 text-right font-mono">{fmt(totals.net)}</td>
            </tr>
          </tfoot>
        </table>
      </Card>
      <p className="mt-2 text-xs text-gray-400">คอลัมน์ "ปกส.คำนวณ/ภาษีคำนวณ" = ระบบคำนวณเทียบ (แดง/เหลือง = ต่างจากที่กรอก) · ภาษีคำนวณอัปเดตหลังกดบันทึกงวด</p>
    </div>
  )
}
