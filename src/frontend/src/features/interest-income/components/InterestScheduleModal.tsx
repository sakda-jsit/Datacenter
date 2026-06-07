import Button from '../../../shared/components/ui/Button'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useInterestLoanDetail } from '../hooks/useInterestIncome'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  companyId: number
  fiscalYear: number
  loanId: number
  onClose: () => void
}

export default function InterestScheduleModal({ companyId, fiscalYear, loanId, onClose }: Props) {
  const { data, isLoading, isError } = useInterestLoanDetail(loanId, companyId, fiscalYear)

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm">
      <div className="my-8 w-full max-w-2xl rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <div>
            <h2 className="text-lg font-bold text-slate-800">ช่วงดอกเบี้ย ปี {fiscalYear}</h2>
            {data && <p className="text-xs text-gray-500">{data.item.name} · อัตรา {fmt(data.item.annualRatePct)}% · ฐาน {data.item.dayCountBasis} วัน</p>}
          </div>
          <button type="button" onClick={onClose} className="text-2xl leading-none text-slate-400 hover:text-slate-600">×</button>
        </div>

        <div className="px-6 py-4">
          {isError && <StateMessage tone="error">โหลดไม่สำเร็จ</StateMessage>}
          {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}
          {data && (
            <>
              <div className="mb-4 grid grid-cols-2 gap-3 text-sm sm:grid-cols-3">
                <Field label="เงินต้นต้นปี">{fmt(data.asOf.openingBalance)}</Field>
                <Field label="เงินต้นปลายปี">{fmt(data.asOf.closingBalance)}</Field>
                <Field label="ดอกเบี้ยรับในปี"><span className="text-sky-700">{fmt(data.asOf.interestForYear)}</span></Field>
                <Field label={`ภาษีธุรกิจเฉพาะ (${fmt(data.item.sbtRatePct)}%)`}>{fmt(data.asOf.sbt)}</Field>
                <Field label={`ส่วนท้องถิ่น (${fmt(data.item.localTaxPctOfSbt)}% ของ SBT)`}>{fmt(data.asOf.localTax)}</Field>
                <Field label="รวมภาษีนำส่ง">{fmt(data.asOf.totalTax)}</Field>
              </div>

              {data.segments.length === 0 ? (
                <StateMessage centered>ไม่มีช่วงดอกเบี้ยในปีนี้</StateMessage>
              ) : (
                <div className="overflow-hidden rounded border border-gray-200">
                  <table className="w-full text-xs">
                    <thead className="bg-slate-50 text-gray-600">
                      <tr>
                        <th className="px-3 py-2 text-left font-medium">ตั้งแต่</th>
                        <th className="px-3 py-2 text-left font-medium">ถึง</th>
                        <th className="px-3 py-2 text-right font-medium">เงินต้นคงเหลือ</th>
                        <th className="px-3 py-2 text-right font-medium">วัน</th>
                        <th className="px-3 py-2 text-right font-medium">ดอกเบี้ย</th>
                      </tr>
                    </thead>
                    <tbody>
                      {data.segments.map((s, i) => (
                        <tr key={i} className="border-t border-gray-100">
                          <td className="px-3 py-1.5 text-gray-600">{s.fromDate.slice(0, 10)}</td>
                          <td className="px-3 py-1.5 text-gray-600">{s.toDate.slice(0, 10)}</td>
                          <td className="px-3 py-1.5 text-right font-mono">{fmt(s.balance)}</td>
                          <td className="px-3 py-1.5 text-right font-mono">{s.days}</td>
                          <td className="px-3 py-1.5 text-right font-mono text-sky-700">{fmt(s.interest)}</td>
                        </tr>
                      ))}
                    </tbody>
                    <tfoot>
                      <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
                        <td className="px-3 py-2" colSpan={4}>รวมดอกเบี้ยรับในปี</td>
                        <td className="px-3 py-2 text-right font-mono text-sky-700">{fmt(data.asOf.interestForYear)}</td>
                      </tr>
                    </tfoot>
                  </table>
                </div>
              )}
            </>
          )}
          <div className="mt-5 flex justify-end">
            <Button type="button" variant="secondary" onClick={onClose}>ปิด</Button>
          </div>
        </div>
      </div>
    </div>
  )
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <p className="mb-0.5 text-xs font-medium text-gray-500">{label}</p>
      <p className="font-mono text-slate-700">{children}</p>
    </div>
  )
}
