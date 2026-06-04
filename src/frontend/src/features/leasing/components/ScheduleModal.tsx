import Button from '../../../shared/components/ui/Button'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useLeaseContract } from '../hooks/useLeasing'
import { CONTRACT_TYPE_LABEL } from '../types/leasing.types'
import type { LeaseAccountBreakdown } from '../types/leasing.types'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

/** ISO (yyyy-mm-dd...) → dd/mm/yyyy */
function fmtDate(iso: string) {
  const [y, m, d] = iso.slice(0, 10).split('-')
  return `${d}/${m}/${y}`
}

interface Props {
  companyId: number
  fiscalYear: number
  contractId: number
  onClose: () => void
}

export default function ScheduleModal({ companyId, fiscalYear, contractId, onClose }: Props) {
  const { data, isLoading, isError } = useLeaseContract(contractId, companyId, fiscalYear)

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm">
      <div className="my-8 w-full max-w-5xl rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <div>
            <h2 className="text-lg font-bold text-slate-800">
              ตารางตัดบัญชี {data ? `· ${data.contract.contractNo}` : ''}
            </h2>
            {data && (
              <p className="text-xs text-gray-500">
                {data.contract.assetName} · {CONTRACT_TYPE_LABEL[data.contract.contractType]} · ปีบัญชี {fiscalYear}
                {' · '}อัตราต่องวด {(data.contract.effectiveRatePerPeriod * 100).toFixed(4)}%
              </p>
            )}
          </div>
          <button type="button" onClick={onClose} className="text-2xl leading-none text-slate-400 hover:text-slate-600">×</button>
        </div>

        <div className="px-6 py-4">
          {isError && <StateMessage tone="error">เกิดข้อผิดพลาด</StateMessage>}
          {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

          {data && (
            <>
              {/* สรุปสิ้นปี */}
              <p className="mb-2 text-sm font-semibold text-slate-700">สรุป ณ สิ้นปีบัญชี {fiscalYear}</p>
              <div className="mb-5 overflow-x-auto rounded border border-gray-200">
                <table className="w-full text-xs">
                  <thead className="bg-slate-50 text-gray-600">
                    <tr>
                      <th className="px-3 py-2 text-left font-medium">องค์ประกอบ</th>
                      <th className="px-3 py-2 text-right font-medium">ยอดยกมา</th>
                      <th className="px-3 py-2 text-right font-medium">ชำระในปี</th>
                      <th className="px-3 py-2 text-right font-medium">คงเหลือ</th>
                      <th className="px-3 py-2 text-right font-medium">ส่วนถึงกำหนด 1 ปี</th>
                      <th className="px-3 py-2 text-right font-medium">ระยะยาว</th>
                    </tr>
                  </thead>
                  <tbody>
                    <SummaryRow label="หนี้สินตามสัญญา (gross)" b={data.yearEnd.grossLiability} />
                    {data.contract.contractType === 0 && (
                      <>
                        <SummaryRow label="ดอกเบี้ยรอตัดบัญชี" b={data.yearEnd.deferredInterest} />
                        <SummaryRow label="ภาษีซื้อยังไม่ถึงกำหนด" b={data.yearEnd.vatUndue} />
                      </>
                    )}
                    <SummaryRow label="เงินต้นคงเหลือ (net)" b={data.yearEnd.netPrincipal} />
                  </tbody>
                  <tfoot>
                    <tr className="border-t-2 border-slate-200 bg-slate-50">
                      <td className="px-3 py-2 font-semibold text-slate-700" colSpan={6}>
                        ดอกเบี้ยรับรู้ในปี {fiscalYear}: <span className="font-mono text-sky-700">{fmt(data.yearEnd.interestRecognizedInYear)}</span>
                      </td>
                    </tr>
                  </tfoot>
                </table>
              </div>

              {/* ตารางตัดบัญชีเต็ม */}
              <p className="mb-2 text-sm font-semibold text-slate-700">ตารางตัดบัญชี ({data.schedule.length} งวด)</p>
              <div className="max-h-96 overflow-auto rounded border border-gray-200">
                <table className="w-full text-xs">
                  <thead className="sticky top-0 bg-slate-50 text-gray-600">
                    <tr>
                      <th className="px-3 py-2 text-right font-medium">งวด</th>
                      <th className="px-3 py-2 text-left font-medium">วันครบกำหนด</th>
                      <th className="px-3 py-2 text-right font-medium">ค่างวด</th>
                      <th className="px-3 py-2 text-right font-medium">เงินต้น</th>
                      <th className="px-3 py-2 text-right font-medium">ดอกเบี้ย</th>
                      <th className="px-3 py-2 text-right font-medium">VAT</th>
                      <th className="px-3 py-2 text-right font-medium">เงินต้นคงเหลือ</th>
                      <th className="px-3 py-2 text-right font-medium">หนี้คงเหลือ (gross)</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.schedule.map((p) => {
                      const inYear = p.dueDate.slice(0, 4) === String(fiscalYear)
                      return (
                        <tr key={p.periodNo} className={`border-t border-gray-100 ${inYear ? 'bg-sky-50/50' : ''}`}>
                          <td className="px-3 py-1 text-right text-gray-500">{p.periodNo}</td>
                          <td className="px-3 py-1 text-gray-600">{fmtDate(p.dueDate)}</td>
                          <td className="px-3 py-1 text-right font-mono">{fmt(p.installment)}</td>
                          <td className="px-3 py-1 text-right font-mono">{fmt(p.principal)}</td>
                          <td className="px-3 py-1 text-right font-mono">{fmt(p.interest)}</td>
                          <td className="px-3 py-1 text-right font-mono text-gray-500">{fmt(p.vat)}</td>
                          <td className="px-3 py-1 text-right font-mono">{fmt(p.closingNetPrincipal)}</td>
                          <td className="px-3 py-1 text-right font-mono text-gray-500">{fmt(p.closingGrossLiability)}</td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>
              <p className="mt-2 text-xs text-gray-400">แถวพื้นฟ้า = งวดที่ครบกำหนดในปีบัญชี {fiscalYear}</p>
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

function SummaryRow({ label, b }: { label: string; b: LeaseAccountBreakdown }) {
  return (
    <tr className="border-t border-gray-100">
      <td className="px-3 py-1.5 text-gray-700">{label}</td>
      <td className="px-3 py-1.5 text-right font-mono">{fmt(b.opening)}</td>
      <td className="px-3 py-1.5 text-right font-mono text-gray-500">{fmt(b.paidInYear)}</td>
      <td className="px-3 py-1.5 text-right font-mono font-semibold">{fmt(b.closing)}</td>
      <td className="px-3 py-1.5 text-right font-mono">{fmt(b.currentPortion)}</td>
      <td className="px-3 py-1.5 text-right font-mono">{fmt(b.longTerm)}</td>
    </tr>
  )
}
