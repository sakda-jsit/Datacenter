import { useState } from 'react'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import { usePayrollYearSummary } from '../../hooks/usePayroll'
import { MONTH_TH, type PayrollSummaryRow } from '../../types/payroll.types'

interface Props {
  companyId: number
}

function fmt(n: number) {
  if (!n) return '-'
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

// ลำดับคอลัมน์ตรงกับ sheet "รายได้ทั้งปี" — [field, ไฮไลต์รวม?]
const COLS: { key: keyof PayrollSummaryRow; label: string; strong?: boolean; calc?: boolean }[] = [
  // รายได้
  { key: 'salary', label: 'เงินเดือน' },
  { key: 'absenceLate', label: 'หยุดงาน+มาสาย' },
  { key: 'netSalary', label: 'เงินเดือนสุทธิ' },
  { key: 'housing', label: 'ค่าที่พักอาศัย' },
  { key: 'food', label: 'ค่าอาหาร' },
  { key: 'overtime', label: 'ค่าล่วงเวลา' },
  { key: 'diligence', label: 'เบี้ยขยัน' },
  { key: 'bonus', label: 'โบนัส' },
  { key: 'netIncomeAfterAbsence', label: 'รายได้สุทธิหลังหักลามาสาย (ภงด.1ก)' },
  { key: 'totalIncome', label: 'รวมรายได้ทั้งหมด (ยังไม่หักลา)', strong: true },
  // กท.20ก
  { key: 'wage', label: 'ค่าจ้าง' },
  { key: 'wageOver20000', label: 'ค่าจ้างส่วนที่เกิน 20,000' },
  // รายการหัก
  { key: 'ssoReportable', label: 'รายได้ยื่นปกส.' },
  { key: 'ssoCalc', label: 'คำนวณหักปกส.ได้', calc: true },
  { key: 'ssoShortfall', label: 'ผลต่าง (ขาดไป)', calc: true },
  { key: 'ssoActual', label: 'หักปกส.จริง' },
  { key: 'tax', label: 'TAX' },
  { key: 'absence', label: 'ขาดงาน' },
  { key: 'advance', label: 'เบิกล่วงหน้า' },
  // รวม
  { key: 'totalDeduction', label: 'รวมรายการหัก', strong: true },
  { key: 'pnd1Income', label: 'รายได้ยื่น ภงด.1 ประจำเดือน' },
  { key: 'employerSso', label: 'นายจ้างสมทบ', calc: true },
  { key: 'netPay', label: 'เงินสุทธิ', strong: true },
]

const GROUPS: { label: string; span: number; cls: string }[] = [
  { label: 'รายได้', span: 10, cls: 'bg-emerald-50 text-emerald-800' },
  { label: 'กรอกในแบบ กท.20 ก', span: 2, cls: 'bg-violet-50 text-violet-800' },
  { label: 'รายการหัก', span: 7, cls: 'bg-rose-50 text-rose-800' },
  { label: 'รวม', span: 3, cls: 'bg-slate-100 text-slate-800' },
]

export default function YearSummaryTab({ companyId }: Props) {
  const thisYear = new Date().getFullYear()
  const [year, setYear] = useState(thisYear)
  const { data, isLoading, isError } = usePayrollYearSummary(companyId, year)

  const yearOptions = Array.from({ length: 6 }, (_, i) => thisYear - i)

  function cell(row: PayrollSummaryRow, c: (typeof COLS)[number]) {
    const v = row[c.key] as number
    const tone = c.calc ? 'text-sky-700' : c.strong ? 'font-semibold text-slate-800' : 'text-gray-700'
    return (
      <td key={c.key} className={`whitespace-nowrap px-2 py-1 text-right font-mono ${tone} ${c.strong ? 'bg-slate-50' : ''}`}>
        {fmt(v)}
      </td>
    )
  }

  return (
    <div>
      <Card className="mb-4 flex flex-wrap items-center justify-between gap-3 px-6 py-4">
        <div>
          <p className="text-sm font-semibold text-slate-800">สรุปรายได้ทั้งปี</p>
          <p className="text-xs text-gray-500">
            รวมทุกงวด/ทุกพนักงานเป็นรายเดือน · คอลัมน์ตาม sheet “รายได้ทั้งปี” · คอลัมน์ <span className="text-sky-700">สีฟ้า</span> = ระบบคำนวณเทียบ
          </p>
        </div>
        <label className="flex items-center gap-2 text-xs text-gray-600">
          ปี
          <select
            value={year}
            onChange={(e) => setYear(Number(e.target.value))}
            className="rounded-md border border-gray-300 px-2 py-1 text-sm"
          >
            {yearOptions.map((y) => (
              <option key={y} value={y}>{y}</option>
            ))}
          </select>
        </label>
      </Card>

      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {data && (
        <Card className="overflow-x-auto">
          <table className="w-full text-[11px]">
            <thead>
              <tr>
                <th rowSpan={2} className="sticky left-0 z-10 border-b border-r bg-white px-2 py-2 text-left align-bottom font-medium text-gray-600">
                  เดือน
                </th>
                {GROUPS.map((g) => (
                  <th key={g.label} colSpan={g.span} className={`border-b border-l px-2 py-1 text-center font-semibold ${g.cls}`}>
                    {g.label}
                  </th>
                ))}
              </tr>
              <tr className="bg-slate-50 text-gray-600">
                {COLS.map((c, idx) => {
                  // เส้นแบ่งกลุ่ม (คอลัมน์แรกของแต่ละกลุ่ม)
                  const groupStart = [0, 10, 12, 19].includes(idx)
                  return (
                    <th key={c.key} className={`border-b px-2 py-2 text-right align-bottom font-medium ${groupStart ? 'border-l' : ''}`}>
                      {c.label}
                    </th>
                  )
                })}
              </tr>
            </thead>
            <tbody>
              {data.months.map((row) => (
                <tr key={row.month} className={`border-b border-gray-100 ${row.hasRun ? 'hover:bg-slate-50' : 'text-gray-300'}`}>
                  <td className="sticky left-0 z-10 whitespace-nowrap border-r bg-white px-2 py-1 font-medium text-slate-700">
                    {MONTH_TH[row.month]}
                    {!row.hasRun && <span className="ml-1 text-[10px] text-gray-300">(ยังไม่มีงวด)</span>}
                  </td>
                  {COLS.map((c) => cell(row, c))}
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="border-t-2 border-slate-300 bg-slate-100 font-semibold text-slate-800">
                <td className="sticky left-0 z-10 whitespace-nowrap border-r bg-slate-100 px-2 py-2">รวมทั้งปี</td>
                {COLS.map((c) => (
                  <td key={c.key} className="whitespace-nowrap px-2 py-2 text-right font-mono">
                    {fmt(data.total[c.key] as number)}
                  </td>
                ))}
              </tr>
            </tfoot>
          </table>
        </Card>
      )}

      <p className="mt-2 text-xs text-gray-400">
        หมายเหตุ: “ค่าจ้าง/รายได้ยื่นปกส.” อ้างฐานยื่น ปกส. ที่กรอกในแต่ละงวด · “ส่วนที่เกิน 20,000” = ส่วนที่เกินเพดานฐานกองทุนทดแทน (กท.20ก) ต่อคน/เดือน
      </p>
    </div>
  )
}
