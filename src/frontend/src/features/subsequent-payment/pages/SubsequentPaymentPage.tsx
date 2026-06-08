import { Fragment, useState } from 'react'
import PageHeader from '../../../shared/components/ui/PageHeader'
import Card from '../../../shared/components/ui/Card'
import StateMessage from '../../../shared/components/ui/StateMessage'
import ExportMenu from '../../../shared/components/ui/ExportMenu'
import type { ExportSection } from '../../../shared/utils/exportTable'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import { useSubsequentPaymentCheck } from '../hooks/useSubsequentPayment'
import type { SubsequentPaymentRow, SubsequentPaymentStatus } from '../types/subsequentPayment.types'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}
function fmtDate(s?: string | null) {
  if (!s) return '-'
  const iso = /[zZ]|[+-]\d\d:?\d\d$/.test(s) ? s : s + 'Z'
  return new Date(iso).toLocaleDateString('th-TH', { dateStyle: 'medium' })
}

const STATUS_META: Record<SubsequentPaymentStatus, { label: string; cls: string }> = {
  paid: { label: 'จ่ายแล้ว', cls: 'bg-green-100 text-green-700' },
  partial: { label: 'จ่ายบางส่วน', cls: 'bg-amber-100 text-amber-700' },
  unpaid: { label: 'ยังไม่พบการจ่าย', cls: 'bg-red-100 text-red-700' },
  unmatched: { label: 'ตรวจปีถัดไปไม่ได้', cls: 'bg-slate-100 text-slate-500' },
}

function StatusBadge({ status }: { status: SubsequentPaymentStatus }) {
  const m = STATUS_META[status]
  return <span className={`rounded-full px-2 py-0.5 text-[11px] font-medium ${m.cls}`}>{m.label}</span>
}

export default function SubsequentPaymentPage() {
  const currentYear = new Date().getFullYear()
  const { companyId } = useCurrentCompany()
  // ปีปิดงบ default = ปีก่อนหน้า (ตรวจการจ่ายในปีปัจจุบัน)
  const [year, setYear] = useState(currentYear - 1)
  const [expanded, setExpanded] = useState<Set<number>>(new Set())

  const { data, isLoading, isError } = useSubsequentPaymentCheck(companyId, year)

  function toggle(id: number) {
    setExpanded((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  const exportSections = (rows: SubsequentPaymentRow[]): ExportSection[] => [
    {
      name: 'ตรวจการจ่ายชำระหลังปิดงบ',
      columns: [
        { key: 'accountCode', header: 'รหัสบัญชี' },
        { key: 'accountName', header: 'ชื่อบัญชี' },
        { key: 'yearEndPayable', header: 'ยอดค้าง ณ สิ้นปี', align: 'right' },
        { key: 'subsequentPaid', header: 'จ่ายปีถัดไป', align: 'right' },
        { key: 'remaining', header: 'คงเหลือ', align: 'right' },
        { key: 'statusLabel', header: 'สถานะ' },
      ],
      rows: rows.map((r) => ({ ...r, statusLabel: STATUS_META[r.status].label })),
    },
  ]

  return (
    <div>
      <PageHeader
        title="ตรวจการจ่ายชำระหลังปิดงบ"
        description="ตรวจรายการค้างจ่าย ณ สิ้นปีปิดงบ ว่าถูกจ่ายชำระจริงในปีถัดไปหรือยัง (RPT-019) — หลักฐานประกอบการสอบทาน"
      />

      <Card className="mb-4 p-4">
        <div className="flex flex-wrap items-end gap-3">
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ปีปิดงบ (AD)</label>
            <input
              type="number" value={year} min={2000} max={2100}
              onChange={(e) => setYear(Number(e.target.value))}
              className="w-24 rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
            />
          </div>
          <p className="pb-2 text-xs text-gray-400">
            ตรวจยอดค้างจ่าย ณ สิ้นปี {year} เทียบกับการจ่ายชำระจริงในปี {year + 1}
          </p>
        </div>
      </Card>

      {!companyId && <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>}
      {companyId > 0 && (
        <>
          {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
          {isLoading && <StateMessage>กำลังตรวจสอบจาก Express...</StateMessage>}

          {data && (
            <>
              {/* แถบหลักการ: ตรวจสด + reference เท่านั้น */}
              <div className="mb-3 rounded-lg border border-sky-200 bg-sky-50 px-4 py-2 text-xs text-sky-800">
                {data.expressAvailable ? (
                  <>
                    🔎 ตรวจการจ่ายชำระในปี <b>{data.subsequentYear}</b> สดจากสมุดรายวัน Express (GLJNLIT) ณ{' '}
                    {fmtDate(data.checkedAt)} — ข้อมูลปีถัดไปเป็น<b>หลักฐานประกอบเท่านั้น ไม่นำมารวมยอดปีปิดงบ</b>
                  </>
                ) : (
                  <>
                    ⚠️ อ่านข้อมูลปี {data.subsequentYear} จาก Express ไม่ได้ (ไฟล์ไม่พร้อม/ออฟไลน์) — สถานะแสดงเป็น
                    "ตรวจปีถัดไปไม่ได้". เปิดให้ Express เข้าถึงโฟลเดอร์ข้อมูลแล้วลองใหม่
                  </>
                )}
              </div>

              {data.rows.length === 0 ? (
                <Card><StateMessage centered>{`ไม่มีบัญชีหนี้สินที่มียอดค้างจ่าย ณ สิ้นปี ${year}`}</StateMessage></Card>
              ) : (
                <>
                  {/* สรุป */}
                  <div className="mb-4 grid grid-cols-2 gap-3 md:grid-cols-4">
                    <SummaryCard label="ยอดค้างจ่ายรวม" value={fmt(data.totalYearEndPayable)} tone="slate" />
                    <SummaryCard label="จ่ายแล้ว (บัญชี)" value={`${data.paidCount}`} tone="green" />
                    <SummaryCard label="จ่ายบางส่วน (บัญชี)" value={`${data.partialCount}`} tone="amber" />
                    <SummaryCard label="ยังไม่พบการจ่าย (บัญชี)" value={`${data.unpaidCount}`} tone="red" />
                  </div>

                  <Card className="overflow-x-auto">
                    <div className="flex items-start justify-between border-b px-4 py-3">
                      <div>
                        <p className="text-sm font-semibold text-slate-800">รายการค้างจ่าย ณ สิ้นปี {year}</p>
                        <p className="text-xs text-gray-500">{data.clientName}</p>
                      </div>
                      <ExportMenu
                        meta={{
                          title: `ตรวจการจ่ายชำระหลังปิดงบ ปี ${year}`,
                          subtitle: `${data.clientName} — จ่ายชำระในปี ${data.subsequentYear}`,
                          fileName: `subsequent-payment-${data.clientCode}-${year}`,
                        }}
                        getSections={() => exportSections(data.rows)}
                      />
                    </div>
                    <table className="w-full text-xs">
                      <thead className="bg-slate-50 text-gray-600">
                        <tr>
                          <th className="px-3 py-2 text-left font-medium">บัญชีค้างจ่าย</th>
                          <th className="px-3 py-2 text-right font-medium">ยอดค้าง ณ สิ้นปี</th>
                          <th className="px-3 py-2 text-right font-medium">จ่ายปี {data.subsequentYear}</th>
                          <th className="px-3 py-2 text-right font-medium">คงเหลือ</th>
                          <th className="px-3 py-2 text-center font-medium">สถานะ</th>
                          <th className="px-3 py-2 text-center font-medium">หลักฐาน</th>
                        </tr>
                      </thead>
                      <tbody>
                        {data.rows.map((r) => {
                          const isOpen = expanded.has(r.accountId)
                          return (
                            <Fragment key={r.accountId}>
                              <tr className="border-t border-gray-100 hover:bg-slate-50">
                                <td className="px-3 py-1.5">
                                  <span className="font-mono text-gray-500">{r.accountCode}</span> {r.accountName}
                                </td>
                                <td className="px-3 py-1.5 text-right font-mono">{fmt(r.yearEndPayable)}</td>
                                <td className="px-3 py-1.5 text-right font-mono text-sky-700">{fmt(r.subsequentPaid)}</td>
                                <td className={`px-3 py-1.5 text-right font-mono ${r.remaining > 0.01 ? 'text-amber-600' : 'text-gray-400'}`}>
                                  {fmt(r.remaining)}
                                </td>
                                <td className="px-3 py-1.5 text-center"><StatusBadge status={r.status} /></td>
                                <td className="px-3 py-1.5 text-center">
                                  {r.payments.length > 0 ? (
                                    <button
                                      type="button" onClick={() => toggle(r.accountId)}
                                      className="rounded border border-slate-200 px-2 py-0.5 text-[11px] text-slate-600 hover:bg-slate-100"
                                    >
                                      {isOpen ? 'ซ่อน' : `${r.payments.length} รายการ`}
                                    </button>
                                  ) : (
                                    <span className="text-gray-300">—</span>
                                  )}
                                </td>
                              </tr>
                              {isOpen && r.payments.length > 0 && (
                                <tr className="bg-slate-50/60">
                                  <td colSpan={6} className="px-6 py-2">
                                    <table className="w-full text-[11px]">
                                      <thead className="text-gray-500">
                                        <tr>
                                          <th className="px-2 py-1 text-left font-medium">วันที่</th>
                                          <th className="px-2 py-1 text-left font-medium">เลขที่</th>
                                          <th className="px-2 py-1 text-left font-medium">รายละเอียด</th>
                                          <th className="px-2 py-1 text-right font-medium">จำนวนเงิน</th>
                                        </tr>
                                      </thead>
                                      <tbody>
                                        {r.payments.map((p, i) => (
                                          <tr key={`${r.accountId}-${i}`} className="border-t border-gray-100">
                                            <td className="px-2 py-1">{fmtDate(p.date)}</td>
                                            <td className="px-2 py-1 font-mono text-gray-600">{p.voucher}</td>
                                            <td className="px-2 py-1 text-gray-600">{p.description}</td>
                                            <td className="px-2 py-1 text-right font-mono">{fmt(p.amount)}</td>
                                          </tr>
                                        ))}
                                      </tbody>
                                    </table>
                                  </td>
                                </tr>
                              )}
                            </Fragment>
                          )
                        })}
                      </tbody>
                      <tfoot>
                        <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
                          <td className="px-3 py-2 text-right">รวม</td>
                          <td className="px-3 py-2 text-right font-mono">{fmt(data.totalYearEndPayable)}</td>
                          <td className="px-3 py-2 text-right font-mono text-sky-700">{fmt(data.totalSubsequentPaid)}</td>
                          <td className="px-3 py-2 text-right font-mono text-amber-600">{fmt(data.totalRemaining)}</td>
                          <td className="px-3 py-2" colSpan={2} />
                        </tr>
                      </tfoot>
                    </table>
                  </Card>
                </>
              )}
            </>
          )}
        </>
      )}
    </div>
  )
}

function SummaryCard({ label, value, tone }: { label: string; value: string; tone: 'slate' | 'green' | 'amber' | 'red' }) {
  const cls = {
    slate: 'border-slate-200 text-slate-700',
    green: 'border-green-200 text-green-700',
    amber: 'border-amber-200 text-amber-700',
    red: 'border-red-200 text-red-700',
  }[tone]
  return (
    <div className={`rounded-lg border bg-white px-4 py-3 ${cls}`}>
      <p className="text-[11px] text-gray-500">{label}</p>
      <p className="mt-1 text-lg font-bold">{value}</p>
    </div>
  )
}
