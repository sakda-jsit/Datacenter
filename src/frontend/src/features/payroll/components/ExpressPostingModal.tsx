import { useEffect, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { payrollApi } from '../services/payrollApi'
import type { ExpressPostingLink } from '../types/payroll.types'

interface Props {
  companyId: number
  sourceType: number // 1=ค่าใช้จ่ายเงินเดือน, 2=นำส่ง ปกส., 3=ใบแจ้งหนี้ กท., 4=นำส่ง กท.
  year: number
  month?: number // 0 = รายปี
  title: string
  onClose: () => void
}

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

export default function ExpressPostingModal({ companyId, sourceType, year, month = 0, title, onClose }: Props) {
  const [d, setD] = useState<ExpressPostingLink | null>(null)
  const [loadErr, setLoadErr] = useState(false)
  const [postedDate, setPostedDate] = useState('')
  const [docNo, setDocNo] = useState('')
  const [amount, setAmount] = useState('')
  const [note, setNote] = useState('')
  const [busy, setBusy] = useState(false)
  const [msg, setMsg] = useState('')

  async function load() {
    setLoadErr(false)
    try {
      const res = await payrollApi.getExpressPosting(companyId, sourceType, year, month)
      setD(res)
      setPostedDate(res.postedDate?.slice(0, 10) ?? '')
      setDocNo(res.expressDocNo ?? '')
      setAmount(res.postedAmount != null ? String(res.postedAmount) : '')
      setNote(res.note ?? '')
    } catch {
      setLoadErr(true)
    }
  }

  useEffect(() => { load() /* eslint-disable-next-line react-hooks/exhaustive-deps */ }, [companyId, sourceType, year, month])

  async function onSave() {
    setBusy(true); setMsg('')
    try {
      await payrollApi.setExpressPosting(companyId, sourceType, year, month, {
        postedDate: postedDate || null,
        expressDocNo: docNo || null,
        postedAmount: amount ? Number(amount) : null,
        note: note || null,
      })
      await load(); setMsg('บันทึกแล้ว')
    } catch { setMsg('บันทึกไม่สำเร็จ') } finally { setBusy(false) }
  }

  const posted = d?.posted ?? false

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm">
      <div className="my-8 w-full max-w-xl rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <div>
            <h2 className="text-lg font-bold text-slate-800">คีย์ลง Express — {title}</h2>
            <p className="text-xs text-gray-500">ปี {year + 543}{month ? ` เดือน ${month}` : ' (รายปี)'}</p>
          </div>
          <span className={`rounded px-2 py-0.5 text-xs ${posted ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-600'}`}>
            {posted ? 'คีย์แล้ว' : 'ยังไม่คีย์'}
          </span>
        </div>

        <div className="space-y-4 px-6 py-4">
          {loadErr && <StateMessage tone="error">โหลดข้อมูลไม่สำเร็จ</StateMessage>}
          {!d && !loadErr && <StateMessage>กำลังโหลด...</StateMessage>}
          {d && (
            <>
              <div className="grid grid-cols-2 gap-3 rounded-lg bg-slate-50 px-4 py-3 text-sm">
                <div>
                  <p className="text-xs text-gray-500">ยอดที่ควรลง (จากระบบ)</p>
                  <p className="font-mono text-base font-bold text-slate-800">{fmt(d.expectedAmount)}</p>
                </div>
                {posted && d.postedAmount != null && (
                  <div className="flex items-end">
                    <span className={`rounded px-2 py-1 text-xs ${d.amountMatch ? 'bg-emerald-50 text-emerald-700' : 'bg-amber-50 text-amber-700'}`}>
                      {d.amountMatch ? '✓ ยอดที่คีย์ตรงกับระบบ' : `⚠ ยอดที่คีย์ ≠ ระบบ (${fmt(d.expectedAmount)})`}
                    </span>
                  </div>
                )}
              </div>

              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                <label className="text-xs text-gray-600">วันที่คีย์ลง Express
                  <input type="date" value={postedDate} onChange={(e) => setPostedDate(e.target.value)}
                    className="mt-1 w-full rounded-md border border-gray-300 px-2 py-1 text-sm" /></label>
                <label className="text-xs text-gray-600">เลขที่เอกสาร Express
                  <input type="text" value={docNo} onChange={(e) => setDocNo(e.target.value)}
                    placeholder="เช่น JV-6801-015"
                    className="mt-1 w-full rounded-md border border-gray-300 px-2 py-1 text-sm" /></label>
                <label className="text-xs text-gray-600">ยอดที่คีย์จริง
                  <input type="number" step="0.01" value={amount} onChange={(e) => setAmount(e.target.value)}
                    placeholder={fmt(d.expectedAmount)}
                    className="mt-1 w-full rounded-md border border-gray-300 px-2 py-1 text-right font-mono text-sm" /></label>
                <label className="text-xs text-gray-600 sm:col-span-1">หมายเหตุ
                  <input type="text" value={note} onChange={(e) => setNote(e.target.value)}
                    className="mt-1 w-full rounded-md border border-gray-300 px-2 py-1 text-sm" /></label>
              </div>
              <p className="text-xs text-gray-400">
                * ระบบไม่เขียนกลับ Express โดยตรง (อ่านอย่างเดียว) — บันทึกนี้ไว้ติดตามว่าคีย์มือลง Express แล้วและกระทบยอด
              </p>
            </>
          )}
        </div>

        <div className="flex items-center justify-end gap-3 border-t border-slate-100 px-6 py-4">
          {msg && <span className="mr-auto text-xs text-gray-500">{msg}</span>}
          <Button type="button" variant="secondary" onClick={onClose} className="px-4">ปิด</Button>
          <Button type="button" onClick={onSave} disabled={busy || !d} className="px-4">
            {busy ? 'กำลังบันทึก...' : 'บันทึก'}
          </Button>
        </div>
      </div>
    </div>
  )
}
