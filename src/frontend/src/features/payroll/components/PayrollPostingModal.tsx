import Button from '../../../shared/components/ui/Button'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { usePayrollPosting } from '../hooks/usePayroll'
import { MONTH_TH } from '../types/payroll.types'

interface Props {
  companyId: number
  runId: number
  onClose: () => void
}

function fmt(n: number) {
  if (!n) return '-'
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

export default function PayrollPostingModal({ companyId, runId, onClose }: Props) {
  const { data: d, isLoading, isError } = usePayrollPosting(companyId, runId)

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm">
      <div className="my-8 w-full max-w-5xl rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <div>
            <h2 className="text-lg font-bold text-slate-800">ใบสำคัญลงบัญชีเงินเดือน + กระทบยอด GL</h2>
            {d && <p className="text-xs text-gray-500">งวด {MONTH_TH[d.month]} {d.year + 543} · บันทึกลง Express ตามรายการนี้</p>}
          </div>
          <button type="button" onClick={onClose} className="text-2xl leading-none text-slate-400 hover:text-slate-600">×</button>
        </div>

        <div className="max-h-[72vh] space-y-4 overflow-y-auto px-6 py-4">
          {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
          {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

          {d && (
            <>
              {d.warnings.length > 0 && (
                <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-2 text-sm text-red-700">
                  ยังแมพบัญชีไม่ครบ — ไปแท็บ “แมพบัญชีเงินเดือน” เพิ่มบัญชีตามบทบาท:
                  <ul className="mt-1 list-disc pl-5 text-xs">{d.warnings.map((w, i) => <li key={i}>{w}</li>)}</ul>
                </div>
              )}

              <div className={`rounded-lg px-4 py-2 text-sm ${d.balanced ? 'bg-green-50 text-green-700' : 'bg-red-50 text-red-700'}`}>
                {d.balanced ? '✓ รายการดุล (เดบิต = เครดิต)' : '⚠ รายการไม่ดุล — ตรวจการแมพบัญชี'}
                {' · '}เดบิตรวม {fmt(d.totalDebit)} · เครดิตรวม {fmt(d.totalCredit)}
              </div>

              <div className="overflow-x-auto rounded-lg border border-slate-100">
                <table className="w-full text-sm">
                  <thead className="border-b bg-slate-50 text-xs text-gray-600">
                    <tr>
                      <th className="px-3 py-2 text-left font-medium">บัญชี</th>
                      <th className="px-3 py-2 text-left font-medium">รายการ / ฝ่าย</th>
                      <th className="px-3 py-2 text-right font-medium">เดบิต</th>
                      <th className="px-3 py-2 text-right font-medium">เครดิต</th>
                      <th className="px-3 py-2 text-right font-medium" title="ความเคลื่อนไหวจริงใน GL เดือนนี้ (debit−credit)">GL เดือนนี้</th>
                      <th className="px-3 py-2 text-right font-medium" title="ที่ควรลง − ที่ลงจริง">ผลต่าง</th>
                    </tr>
                  </thead>
                  <tbody>
                    {d.lines.map((l, i) => {
                      const hasGl = !!l.accountCode
                      const mismatch = hasGl && Math.abs(l.diff) > 0.01
                      return (
                        <tr key={i} className="border-b border-gray-100">
                          <td className="px-3 py-1.5 whitespace-nowrap">
                            {l.accountCode
                              ? <><span className="font-mono text-xs text-slate-700">{l.accountCode}</span>
                                  <span className="ml-1 text-xs text-gray-500">{l.accountName}</span></>
                              : <span className="text-red-500">— ยังไม่แมพ —</span>}
                          </td>
                          <td className="px-3 py-1.5 text-gray-700">
                            {l.roleLabel}{l.department ? <span className="text-gray-400"> · {l.department}</span> : null}
                          </td>
                          <td className="px-3 py-1.5 text-right font-mono">{fmt(l.debit)}</td>
                          <td className="px-3 py-1.5 text-right font-mono">{fmt(l.credit)}</td>
                          <td className="px-3 py-1.5 text-right font-mono text-gray-500">{hasGl ? fmt(l.glMovement) : ''}</td>
                          <td className={`px-3 py-1.5 text-right font-mono ${mismatch ? 'bg-amber-50 text-amber-700' : 'text-gray-400'}`}>
                            {hasGl ? fmt(l.diff) : ''}
                          </td>
                        </tr>
                      )
                    })}
                  </tbody>
                  <tfoot>
                    <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
                      <td colSpan={2} className="px-3 py-2">รวม</td>
                      <td className="px-3 py-2 text-right font-mono">{fmt(d.totalDebit)}</td>
                      <td className="px-3 py-2 text-right font-mono">{fmt(d.totalCredit)}</td>
                      <td colSpan={2} />
                    </tr>
                  </tfoot>
                </table>
              </div>

              <p className="text-xs text-gray-400">
                “GL เดือนนี้” = ความเคลื่อนไหวจริงของบัญชีใน GL (นำเข้าจาก Express) เดือนนั้น · “ผลต่าง” = ยอดที่ควรลง − ที่ลงจริง
                (เหลือง = ยังไม่ตรง ควรตรวจว่าคีย์ลง Express ครบหรือยัง) · ระบบไม่โพสต์ทับ GL
              </p>
            </>
          )}
        </div>

        <div className="flex items-center justify-end gap-2 border-t border-slate-100 px-6 py-4">
          <Button type="button" variant="secondary" onClick={onClose} className="px-4">ปิด</Button>
        </div>
      </div>
    </div>
  )
}
