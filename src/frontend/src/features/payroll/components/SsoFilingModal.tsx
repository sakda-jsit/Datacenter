import Button from '../../../shared/components/ui/Button'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { payrollApi } from '../services/payrollApi'
import { useSsoFiling } from '../hooks/usePayroll'
import { MONTH_TH } from '../types/payroll.types'

interface Props {
  companyId: number
  runId: number
  onClose: () => void
}

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

async function download(blob: Blob, name: string) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = name
  a.click()
  setTimeout(() => URL.revokeObjectURL(url), 30000)
}

export default function SsoFilingModal({ companyId, runId, onClose }: Props) {
  const { data: d, isLoading, isError } = useSsoFiling(companyId, runId)

  const noAccount = d && !d.ssoAccountNo

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm">
      <div className="my-8 w-full max-w-4xl rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <div>
            <h2 className="text-lg font-bold text-slate-800">สปส.1-10 — แบบรายการแสดงการส่งเงินสมทบ</h2>
            {d && <p className="text-xs text-gray-500">งวด {MONTH_TH[d.month]} {d.year + 543} · {d.companyName}</p>}
          </div>
          <button type="button" onClick={onClose} className="text-2xl leading-none text-slate-400 hover:text-slate-600">×</button>
        </div>

        <div className="max-h-[72vh] space-y-4 overflow-y-auto px-6 py-4">
          {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
          {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

          {d && (
            <>
              {noAccount && (
                <StateMessage tone="error">
                  ยังไม่ได้กรอกเลขที่บัญชีนายจ้าง ปกส. — ไปกรอกที่หน้าข้อมูลบริษัท (ลูกค้า) ก่อนเพื่อให้ฟอร์มสมบูรณ์
                </StateMessage>
              )}

              {/* สรุป */}
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                <Info label="เลขที่บัญชี" value={d.ssoAccountNo || '—'} />
                <Info label="ลำดับสาขา" value={d.ssoBranchCode} />
                <Info label="อัตราเงินสมทบ" value={`${d.ratePct}%`} />
                <Info label="ผู้ประกันตน" value={`${d.insuredCount} คน`} />
              </div>

              {d.rows.length === 0 ? (
                <StateMessage centered>ไม่มีผู้ประกันตนที่มีค่าจ้างยื่น ปกส.ในงวดนี้</StateMessage>
              ) : (
                <div className="overflow-x-auto rounded-lg border border-slate-100">
                  <table className="w-full text-sm">
                    <thead className="border-b bg-slate-50 text-xs text-gray-600">
                      <tr>
                        <th className="px-3 py-2 text-center font-medium w-14">ลำดับ</th>
                        <th className="px-3 py-2 text-left font-medium">เลขประจำตัวประชาชน</th>
                        <th className="px-3 py-2 text-left font-medium">ชื่อ-สกุล</th>
                        <th className="px-3 py-2 text-right font-medium">ค่าจ้าง</th>
                        <th className="px-3 py-2 text-right font-medium">เงินสมทบ</th>
                      </tr>
                    </thead>
                    <tbody>
                      {d.rows.map((r) => (
                        <tr key={r.seq} className="border-b border-gray-100">
                          <td className="px-3 py-1.5 text-center text-gray-500">{r.seq}</td>
                          <td className="px-3 py-1.5 font-mono text-xs">{r.nationalId}</td>
                          <td className="px-3 py-1.5">{r.prefix}{r.firstName} {r.lastName}</td>
                          <td className="px-3 py-1.5 text-right font-mono">{fmt(r.wage)}</td>
                          <td className="px-3 py-1.5 text-right font-mono">{fmt(r.contribution)}</td>
                        </tr>
                      ))}
                    </tbody>
                    <tfoot>
                      <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
                        <td colSpan={3} className="px-3 py-2 text-right">ยอดรวม</td>
                        <td className="px-3 py-2 text-right font-mono">{fmt(d.totalWage)}</td>
                        <td className="px-3 py-2 text-right font-mono">{fmt(d.totalEmployee)}</td>
                      </tr>
                    </tfoot>
                  </table>
                </div>
              )}

              {/* ยอดสมทบ */}
              <div className="grid grid-cols-2 gap-3 rounded-lg bg-slate-50 px-4 py-3 text-sm sm:grid-cols-4">
                <Info label="เงินค่าจ้างทั้งสิ้น" value={fmt(d.totalWage)} />
                <Info label="เงินสมทบผู้ประกันตน" value={fmt(d.totalEmployee)} />
                <Info label="เงินสมทบนายจ้าง" value={fmt(d.totalEmployer)} />
                <Info label="รวมนำส่งทั้งสิ้น" value={fmt(d.grandTotal)} strong />
              </div>
              <p className="text-xs text-gray-500">({d.grandTotalText})</p>
            </>
          )}
        </div>

        <div className="flex items-center justify-end gap-2 border-t border-slate-100 px-6 py-4">
          <Button type="button" variant="secondary" onClick={onClose} className="px-4">ปิด</Button>
          <Button
            type="button"
            variant="secondary"
            disabled={!d || d.rows.length === 0}
            onClick={async () => download(await payrollApi.downloadSsoExcel(runId, companyId), `sso1-10-${runId}.xlsx`)}
          >
            ⬇ Excel (อัปโหลด e-Service)
          </Button>
          <Button
            type="button"
            disabled={!d || d.rows.length === 0}
            onClick={async () => download(await payrollApi.downloadSsoPdf(runId, companyId), `sso1-10-${runId}.pdf`)}
          >
            ⬇ PDF ฟอร์ม สปส.1-10
          </Button>
        </div>
      </div>
    </div>
  )
}

function Info({ label, value, strong }: { label: string; value: string; strong?: boolean }) {
  return (
    <div>
      <p className="text-xs text-gray-500">{label}</p>
      <p className={`font-mono ${strong ? 'text-base font-bold text-slate-800' : 'text-sm text-slate-700'}`}>{value}</p>
    </div>
  )
}
