import { useEffect, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { payrollApi } from '../services/payrollApi'
import type { StatutoryFiling } from '../types/payroll.types'

interface Props {
  companyId: number
  filingType: number // 1=ภ.ง.ด.1, 2=ภ.ง.ด.1ก, 3=กท.20ก
  year: number
  month?: number // 0 = รายปี
  title: string
  baseLabel: string   // เช่น "เงินได้รวม" / "ค่าจ้างรวม"
  amountLabel: string // เช่น "ภาษีนำส่ง" / "เงินสมทบ"
  onClose: () => void
}

const STATUS = ['ยังไม่ยื่น', 'ยื่นแล้ว', 'ได้รับใบเสร็จ']

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

async function save(blob: Blob, name: string) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url; a.download = name
  document.body.appendChild(a); a.click(); document.body.removeChild(a)
  setTimeout(() => URL.revokeObjectURL(url), 1000)
}

export default function FilingStatusModal({ companyId, filingType, year, month = 0, title, baseLabel, amountLabel, onClose }: Props) {
  const [d, setD] = useState<StatutoryFiling | null>(null)
  const [loadErr, setLoadErr] = useState(false)
  const [submittedDate, setSubmittedDate] = useState('')
  const [receiptDate, setReceiptDate] = useState('')
  const [receiptAmount, setReceiptAmount] = useState('')
  const [receiptNo, setReceiptNo] = useState('')
  const [note, setNote] = useState('')
  const [busy, setBusy] = useState(false)
  const [msg, setMsg] = useState('')

  async function load() {
    setLoadErr(false)
    try {
      const res = await payrollApi.getStatutoryFiling(companyId, filingType, year, month)
      setD(res)
      setSubmittedDate(res.submittedDate?.slice(0, 10) ?? '')
      setReceiptDate(res.receiptDate?.slice(0, 10) ?? '')
      setReceiptAmount(res.receiptAmount != null ? String(res.receiptAmount) : '')
      setReceiptNo(res.receiptNo ?? '')
      setNote(res.note ?? '')
    } catch {
      setLoadErr(true)
    }
  }

  useEffect(() => { load() /* eslint-disable-next-line react-hooks/exhaustive-deps */ }, [companyId, filingType, year, month])

  async function onSave() {
    setBusy(true); setMsg('')
    try {
      await payrollApi.setStatutoryFilingStatus(companyId, filingType, year, month, {
        submittedDate: submittedDate || null,
        receiptDate: receiptDate || null,
        receiptAmount: receiptAmount ? Number(receiptAmount) : null,
        receiptNo: receiptNo || null,
        note: note || null,
      })
      await load(); setMsg('บันทึกแล้ว')
    } catch { setMsg('บันทึกไม่สำเร็จ') } finally { setBusy(false) }
  }

  async function upload(kind: 'form' | 'receipt', file: File) {
    setBusy(true); setMsg('')
    try {
      await payrollApi.uploadStatutoryFilingDoc(companyId, filingType, year, month, kind, file)
      await load(); setMsg('อัปโหลดแล้ว')
    } catch { setMsg('อัปโหลดไม่สำเร็จ') } finally { setBusy(false) }
  }

  const statusIdx = d?.status ?? 0
  const statusCls = statusIdx >= 2 ? 'bg-emerald-100 text-emerald-700' : statusIdx === 1 ? 'bg-sky-100 text-sky-700' : 'bg-gray-100 text-gray-600'

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm">
      <div className="my-8 w-full max-w-2xl rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <div>
            <h2 className="text-lg font-bold text-slate-800">สถานะการยื่น — {title}</h2>
            <p className="text-xs text-gray-500">ปี {year + 543}{month ? ` เดือน ${month}` : ' (รายปี)'}</p>
          </div>
          <span className={`rounded px-2 py-0.5 text-xs ${statusCls}`}>{STATUS[statusIdx]}</span>
        </div>

        <div className="space-y-4 px-6 py-4">
          {loadErr && <StateMessage tone="error">โหลดข้อมูลไม่สำเร็จ</StateMessage>}
          {!d && !loadErr && <StateMessage>กำลังโหลด...</StateMessage>}
          {d && (
            <>
              {/* ยอดปัจจุบัน + กระทบยอด */}
              <div className="grid grid-cols-3 gap-3 rounded-lg bg-slate-50 px-4 py-3 text-sm">
                <Info label={baseLabel} value={fmt(d.currentBase)} />
                <Info label={amountLabel} value={fmt(d.currentAmount)} strong />
                <Info label="จำนวนคน" value={String(d.currentCount)} />
              </div>
              {statusIdx >= 1 && (
                <div className="flex flex-wrap gap-2 text-xs">
                  <span className={`rounded px-2 py-1 ${d.amountMatch ? 'bg-emerald-50 text-emerald-700' : 'bg-amber-50 text-amber-700'}`}>
                    {d.amountMatch ? '✓' : '⚠'} ยอดที่ยื่น {d.amountMatch ? 'ตรงกับปัจจุบัน' : `≠ ปัจจุบัน (ยื่นไว้ ${fmt(d.snapshotAmount)})`}
                  </span>
                  {d.receiptAmount != null && (
                    <span className={`rounded px-2 py-1 ${d.receiptMatch ? 'bg-emerald-50 text-emerald-700' : 'bg-amber-50 text-amber-700'}`}>
                      {d.receiptMatch ? '✓' : '⚠'} ใบเสร็จ {d.receiptMatch ? 'ตรงยอด' : `≠ ยอด (${fmt(d.currentAmount)})`}
                    </span>
                  )}
                </div>
              )}

              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                <label className="text-xs text-gray-600">วันที่ยื่น
                  <input type="date" value={submittedDate} onChange={(e) => setSubmittedDate(e.target.value)}
                    className="mt-1 w-full rounded-md border border-gray-300 px-2 py-1 text-sm" /></label>
                <label className="text-xs text-gray-600">วันที่ใบเสร็จ
                  <input type="date" value={receiptDate} onChange={(e) => setReceiptDate(e.target.value)}
                    className="mt-1 w-full rounded-md border border-gray-300 px-2 py-1 text-sm" /></label>
                <label className="text-xs text-gray-600">ยอดในใบเสร็จ
                  <input type="number" step="0.01" value={receiptAmount} onChange={(e) => setReceiptAmount(e.target.value)}
                    placeholder={fmt(d.currentAmount)}
                    className="mt-1 w-full rounded-md border border-gray-300 px-2 py-1 text-right font-mono text-sm" /></label>
                <label className="text-xs text-gray-600">เลขที่ใบเสร็จ
                  <input type="text" value={receiptNo} onChange={(e) => setReceiptNo(e.target.value)}
                    className="mt-1 w-full rounded-md border border-gray-300 px-2 py-1 text-sm" /></label>
                <label className="text-xs text-gray-600 sm:col-span-2">หมายเหตุ
                  <input type="text" value={note} onChange={(e) => setNote(e.target.value)}
                    className="mt-1 w-full rounded-md border border-gray-300 px-2 py-1 text-sm" /></label>
              </div>

              <div className="flex flex-wrap items-center gap-4 text-xs">
                <FileField label="แบบที่ยื่น" has={d.hasForm}
                  onPick={(f) => upload('form', f)}
                  onDownload={async () => save(await payrollApi.downloadStatutoryFilingDoc(companyId, filingType, year, month, 'form'), `${title}-form`)} />
                <FileField label="ใบเสร็จ" has={d.hasReceipt}
                  onPick={(f) => upload('receipt', f)}
                  onDownload={async () => save(await payrollApi.downloadStatutoryFilingDoc(companyId, filingType, year, month, 'receipt'), `${title}-receipt`)} />
              </div>
            </>
          )}
        </div>

        <div className="flex items-center justify-end gap-3 border-t border-slate-100 px-6 py-4">
          {msg && <span className="mr-auto text-xs text-gray-500">{msg}</span>}
          <Button type="button" variant="secondary" onClick={onClose} className="px-4">ปิด</Button>
          <Button type="button" onClick={onSave} disabled={busy || !d} className="px-4">
            {busy ? 'กำลังบันทึก...' : 'บันทึกสถานะ'}
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

function FileField({ label, has, onPick, onDownload }: {
  label: string; has: boolean; onPick: (f: File) => void; onDownload: () => void
}) {
  return (
    <div className="flex items-center gap-2">
      <span className="text-gray-600">{label}:</span>
      {has
        ? <button type="button" onClick={onDownload} className="text-sky-600 underline">มีไฟล์ (ดาวน์โหลด)</button>
        : <span className="text-gray-400">ยังไม่มี</span>}
      <label className="cursor-pointer rounded border border-gray-300 px-2 py-0.5 text-gray-600 hover:bg-gray-50">
        {has ? 'แทนที่' : 'อัปโหลด'}
        <input type="file" className="hidden" onChange={(e) => { const f = e.target.files?.[0]; if (f) onPick(f); e.currentTarget.value = '' }} />
      </label>
    </div>
  )
}
