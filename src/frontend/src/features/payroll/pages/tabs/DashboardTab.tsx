import { useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import { usePayrollDashboard } from '../../hooks/usePayroll'
import { MONTH_TH, type PayrollChecklistMonth } from '../../types/payroll.types'
import FilingStatusModal from '../../components/FilingStatusModal'
import ExpressPostingModal from '../../components/ExpressPostingModal'

interface Props {
  companyId: number
}

function fmt(n: number) {
  if (!n) return '-'
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

const STATUS_LABEL = ['ร่าง', 'บันทึกแล้ว', 'ปิดงวด']

function Check({ ok }: { ok: boolean }) {
  return <span className={ok ? 'text-emerald-600' : 'text-gray-300'}>{ok ? '✓' : '–'}</span>
}

function FiledBadge({ filed, receipt }: { filed: boolean; receipt: boolean }) {
  if (receipt) return <span className="rounded bg-emerald-100 px-1.5 py-0.5 text-[10px] text-emerald-700">ได้ใบเสร็จ</span>
  if (filed) return <span className="rounded bg-sky-100 px-1.5 py-0.5 text-[10px] text-sky-700">ยื่นแล้ว</span>
  return <span className="rounded bg-gray-100 px-1.5 py-0.5 text-[10px] text-gray-500">ยังไม่ยื่น</span>
}

function ReconCard({ title, ok, value, note }: { title: string; ok: boolean; value: string; note: string }) {
  return (
    <div className={`rounded-xl border p-4 ${ok ? 'border-emerald-200 bg-emerald-50' : 'border-amber-200 bg-amber-50'}`}>
      <div className="flex items-center justify-between">
        <p className="text-sm font-semibold text-slate-700">{title}</p>
        <span className={`text-lg ${ok ? 'text-emerald-600' : 'text-amber-600'}`}>{ok ? '✓' : '⚠'}</span>
      </div>
      <p className="mt-1 font-mono text-base font-semibold text-slate-800">{value}</p>
      <p className="mt-1 text-xs text-gray-500">{note}</p>
    </div>
  )
}

export default function DashboardTab({ companyId }: Props) {
  const thisYear = new Date().getFullYear()
  const [year, setYear] = useState(thisYear)
  const [pnd1Month, setPnd1Month] = useState<number | null>(null)
  const [expressMonth, setExpressMonth] = useState<number | null>(null)
  const qc = useQueryClient()
  const { data, isLoading, isError } = usePayrollDashboard(companyId, year)
  const yearOptions = Array.from({ length: 6 }, (_, i) => thisYear - i)

  // กระทบยอด #1 slip↔ระบบ: ผ่านถ้าทุกเดือน |diff| < 2 บาท
  const ssoOk = !!data && data.months.filter((m) => m.hasRun).every((m) => Math.abs(m.ssoCrossCheckDiff) < 2)
  const maxSsoDiff = data ? Math.max(0, ...data.months.map((m) => Math.abs(m.ssoCrossCheckDiff))) : 0
  // กระทบยอด #2 เงินเดือน↔GL: ใบสำคัญดุลทุกเดือน
  const balancedOk = !!data && data.months.filter((m) => m.hasRun).every((m) => m.postingBalanced)

  return (
    <div>
      <Card className="mb-4 flex flex-wrap items-center justify-between gap-3 px-6 py-4">
        <div>
          <p className="text-sm font-semibold text-slate-800">ภาพรวม / กระทบยอดเงินเดือน</p>
          <p className="text-xs text-gray-500">สถานะรายเดือน + กระทบยอด 3 ทาง (slip ↔ ระบบ ↔ GL/แบบที่ยื่น)</p>
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
        <>
          {/* การ์ดสรุปรายปี */}
          <div className="mb-4 grid grid-cols-2 gap-3 md:grid-cols-4">
            <Card className="px-4 py-3">
              <p className="text-xs text-gray-500">เดือนที่บันทึกแล้ว</p>
              <p className="text-xl font-bold text-slate-800">{data.monthsWithRun}/12</p>
            </Card>
            <Card className="px-4 py-3">
              <p className="text-xs text-gray-500">รายได้รวมทั้งปี</p>
              <p className="text-xl font-bold text-slate-800">{fmt(data.yearGross)}</p>
            </Card>
            <Card className="px-4 py-3">
              <p className="text-xs text-gray-500">ปกส.รวม (ลูกจ้าง+นายจ้าง)</p>
              <p className="text-xl font-bold text-slate-800">{fmt(data.yearSsoTotal)}</p>
            </Card>
            <Card className="px-4 py-3">
              <p className="text-xs text-gray-500">ภาษีหัก ณ ที่จ่ายรวม</p>
              <p className="text-xl font-bold text-slate-800">{fmt(data.yearTax)}</p>
            </Card>
          </div>

          {/* กระทบยอด 3 ทาง */}
          <div className="mb-4 grid grid-cols-1 gap-3 md:grid-cols-3">
            <ReconCard
              title="① slip ↔ ระบบคำนวณ (ปกส.)"
              ok={ssoOk}
              value={ssoOk ? 'ตรงกัน' : `ส่วนต่างสูงสุด ${fmt(maxSsoDiff)}`}
              note="เทียบ ปกส.ที่หักจริงจาก slip กับยอดที่ระบบคำนวณ (คลาดเคลื่อนปัดเศษ < 2 บาท = ผ่าน)"
            />
            <ReconCard
              title="② เงินเดือน ↔ GL (ใบสำคัญ)"
              ok={balancedOk}
              value={balancedOk ? 'ใบสำคัญดุลทุกเดือน' : 'มีงวดที่ยังไม่ดุล/ยังไม่แมพบัญชี'}
              note="เดบิต = เครดิตในใบสำคัญลงบัญชี · ดูส่วนต่างเทียบ GL จริงในตารางด้านล่าง"
            />
            <ReconCard
              title="③ ภาษี ↔ ภ.ง.ด.1ก"
              ok={data.taxConsistent}
              value={data.taxConsistent ? 'ตรงกัน' : `ส่วนต่าง ${fmt(data.taxConsistencyDiff)}`}
              note={`Σ ภาษีรายเดือน เทียบยอด ภ.ง.ด.1ก (${data.pnd1kPersonCount} ราย, ภาษี ${fmt(data.pnd1kTotalTax)})`}
            />
          </div>

          {/* แบบรายปีที่พร้อม + ความคืบหน้าการยื่น ปกส. */}
          <Card className="mb-4 flex flex-wrap gap-x-8 gap-y-2 px-6 py-3 text-sm">
            <div>
              <span className="text-gray-500">ยื่น ปกส.:</span>{' '}
              <span className="font-semibold">{data.months.filter((m) => m.ssoFiled).length}/{data.monthsWithRun} เดือน</span>
              {' · '}<span className="text-gray-500">ได้ใบเสร็จ</span> <span className="font-semibold">{data.months.filter((m) => m.ssoReceiptReceived).length} เดือน</span>
            </div>
            <div>
              <span className="text-gray-500">คีย์ Express (เงินเดือน):</span>{' '}
              <span className="font-semibold">{data.months.filter((m) => m.expressPosted).length}/{data.monthsWithRun} เดือน</span>
            </div>
            <div>
              <span className="text-gray-500">ภ.ง.ด.1ก:</span> <span className="font-semibold">{data.pnd1kPersonCount} ราย</span> · ภาษี {fmt(data.pnd1kTotalTax)}
              {' '}<FiledBadge filed={data.pnd1kFiled} receipt={data.pnd1kReceipt} />
            </div>
            <div><span className="text-gray-500">50 ทวิ เงินเดือน:</span> <span className="font-semibold">{data.pnd1kPersonCount} ฉบับ</span></div>
            <div>
              <span className="text-gray-500">กท.20ก:</span> <span className="font-semibold">{data.kt20EmployeeCount} ลูกจ้าง</span> · สมทบ {fmt(data.kt20Contribution)}
              {' '}<FiledBadge filed={data.kt20Filed} receipt={data.kt20Receipt} />
            </div>
          </Card>

          {/* checklist รายเดือน */}
          <Card className="overflow-x-auto">
            <table className="w-full text-xs">
              <thead>
                <tr className="bg-slate-50 text-gray-600">
                  <th className="px-3 py-2 text-left font-medium">เดือน</th>
                  <th className="px-3 py-2 text-center font-medium">สถานะ</th>
                  <th className="px-3 py-2 text-right font-medium">พนักงาน</th>
                  <th className="px-3 py-2 text-right font-medium">รายได้รวม</th>
                  <th className="px-3 py-2 text-right font-medium">ปกส. (ลจ.)</th>
                  <th className="px-3 py-2 text-right font-medium">ภาษี</th>
                  <th className="px-3 py-2 text-center font-medium">slip↔ระบบ</th>
                  <th className="px-3 py-2 text-center font-medium">ลงบัญชีดุล</th>
                  <th className="px-3 py-2 text-center font-medium">ยื่น ปกส.</th>
                  <th className="px-3 py-2 text-center font-medium">ใบเสร็จ</th>
                  <th className="px-3 py-2 text-center font-medium">ภ.ง.ด.1</th>
                  <th className="px-3 py-2 text-center font-medium">ลง Express</th>
                  <th className="px-3 py-2 text-right font-medium">ส่วนต่าง GL</th>
                </tr>
              </thead>
              <tbody>
                {data.months.map((m: PayrollChecklistMonth) => (
                  <tr key={m.month} className={`border-b border-gray-100 ${m.hasRun ? 'hover:bg-slate-50' : 'text-gray-300'}`}>
                    <td className="whitespace-nowrap px-3 py-1.5 font-medium text-slate-700">{MONTH_TH[m.month]}</td>
                    <td className="px-3 py-1.5 text-center">
                      {m.hasRun ? (
                        <span className={`rounded px-2 py-0.5 text-[10px] ${m.status >= 2 ? 'bg-emerald-100 text-emerald-700' : m.status === 1 ? 'bg-sky-100 text-sky-700' : 'bg-gray-100 text-gray-600'}`}>
                          {STATUS_LABEL[m.status] ?? '-'}
                        </span>
                      ) : (
                        <span className="text-[10px] text-gray-300">ยังไม่มีงวด</span>
                      )}
                    </td>
                    <td className="px-3 py-1.5 text-right font-mono">{m.hasRun ? m.employeeCount : '-'}</td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(m.totalGross)}</td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(m.ssoEmployee)}</td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(m.tax)}</td>
                    <td className="px-3 py-1.5 text-center font-mono">
                      {m.hasRun ? (Math.abs(m.ssoCrossCheckDiff) < 2 ? <Check ok /> : <span className="text-amber-600">{fmt(m.ssoCrossCheckDiff)}</span>) : '-'}
                    </td>
                    <td className="px-3 py-1.5 text-center"><Check ok={m.postingBalanced} /></td>
                    <td className="px-3 py-1.5 text-center"><Check ok={m.ssoFiled} /></td>
                    <td className="px-3 py-1.5 text-center">
                      {m.ssoReceiptReceived
                        ? (m.ssoReceiptMatch ? <Check ok /> : <span className="text-amber-600" title="ยอดใบเสร็จไม่ตรง">⚠</span>)
                        : <span className="text-gray-300">–</span>}
                    </td>
                    <td className="px-3 py-1.5 text-center">
                      {m.hasRun ? (
                        <button type="button" onClick={() => setPnd1Month(m.month)}
                          className="text-xs underline decoration-dotted underline-offset-2 hover:text-sky-600"
                          title={m.tax > 0 ? 'มีภาษีหัก — ต้องยื่น ภ.ง.ด.1' : 'ไม่มีภาษีหัก'}>
                          {m.pnd1Filed ? <span className="text-emerald-600">✓</span> : (m.tax > 0 ? <span className="text-amber-600">ต้องยื่น</span> : <span className="text-gray-300">–</span>)}
                        </button>
                      ) : '-'}
                    </td>
                    <td className="px-3 py-1.5 text-center">
                      {m.hasRun ? (
                        <button type="button" onClick={() => setExpressMonth(m.month)}
                          className="text-xs underline decoration-dotted underline-offset-2 hover:text-sky-600" title="บันทึกการคีย์ลง Express">
                          {m.expressPosted ? <span className="text-emerald-600">✓</span> : <span className="text-amber-600">คีย์</span>}
                        </button>
                      ) : '-'}
                    </td>
                    <td className="px-3 py-1.5 text-right font-mono text-gray-500">{m.hasRun ? fmt(m.glDiff) : '-'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Card>

          <p className="mt-2 text-xs text-gray-400">
            หมายเหตุ: “ส่วนต่าง GL” = ผลต่างยอดที่ควรลง (ใบสำคัญ) กับความเคลื่อนไหวจริงใน GL เดือนนั้น ·
            หากกิจการคีย์ค่าใช้จ่ายเงินเดือนแบบลงรวมปลายปี ส่วนต่างรายเดือนจะสูง (ยอดไปกองที่ ธ.ค.) ถือเป็นปกติของข้อมูลที่นำเข้า
          </p>
        </>
      )}

      {pnd1Month !== null && (
        <FilingStatusModal companyId={companyId} filingType={1} year={year} month={pnd1Month}
          title={`ภ.ง.ด.1 (${MONTH_TH[pnd1Month]})`} baseLabel="เงินได้สุทธิ" amountLabel="ภาษีนำส่ง"
          onClose={() => { setPnd1Month(null); qc.invalidateQueries({ queryKey: ['payroll-dashboard', companyId, year] }) }} />
      )}
      {expressMonth !== null && (
        <ExpressPostingModal companyId={companyId} sourceType={1} year={year} month={expressMonth}
          title={`ค่าใช้จ่ายเงินเดือน (${MONTH_TH[expressMonth]})`}
          onClose={() => { setExpressMonth(null); qc.invalidateQueries({ queryKey: ['payroll-dashboard', companyId, year] }) }} />
      )}
    </div>
  )
}
