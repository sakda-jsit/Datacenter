import { useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import Button from '../../../shared/components/ui/Button'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { payrollApi } from '../services/payrollApi'
import { useSsoFiling } from '../hooks/usePayroll'
import { MONTH_TH, type SsoFiling } from '../types/payroll.types'

interface Props {
  companyId: number
  runId: number
  onClose: () => void
}

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

const STATUS = ['ยังไม่ยื่น', 'ยื่นแล้ว', 'ได้รับใบเสร็จ']

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

              <FilingStatusSection companyId={companyId} runId={runId} d={d} />
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

// ── สถานะยื่น + ใบเสร็จ + กระทบยอด ──────────────────────────────────────────────
function FilingStatusSection({ companyId, runId, d }: { companyId: number; runId: number; d: SsoFiling }) {
  const qc = useQueryClient()
  const fs = d.filingStatus
  const [submittedDate, setSubmittedDate] = useState(fs?.submittedDate?.slice(0, 10) ?? '')
  const [receiptDate, setReceiptDate] = useState(fs?.receiptDate?.slice(0, 10) ?? '')
  const [receiptAmount, setReceiptAmount] = useState(fs?.receiptAmount != null ? String(fs.receiptAmount) : '')
  const [receiptNo, setReceiptNo] = useState(fs?.receiptNo ?? '')
  const [note, setNote] = useState(fs?.note ?? '')
  const [saving, setSaving] = useState(false)
  const [msg, setMsg] = useState('')

  const refresh = () => qc.invalidateQueries({ queryKey: ['sso-filing', companyId, runId] })

  async function save() {
    setSaving(true); setMsg('')
    try {
      await payrollApi.setSsoFilingStatus(runId, companyId, {
        submittedDate: submittedDate || null,
        receiptDate: receiptDate || null,
        receiptAmount: receiptAmount ? Number(receiptAmount) : null,
        receiptNo: receiptNo || null,
        note: note || null,
      })
      await refresh()
      setMsg('บันทึกแล้ว')
    } catch {
      setMsg('บันทึกไม่สำเร็จ')
    } finally {
      setSaving(false)
    }
  }

  async function upload(kind: 'form' | 'receipt', file: File) {
    setSaving(true); setMsg('')
    try {
      await payrollApi.uploadSsoFilingDoc(runId, companyId, kind, file)
      await refresh()
      setMsg('อัปโหลดแล้ว')
    } catch {
      setMsg('อัปโหลดไม่สำเร็จ')
    } finally {
      setSaving(false)
    }
  }

  const statusIdx = fs?.status ?? 0
  const statusCls = statusIdx >= 2 ? 'bg-emerald-100 text-emerald-700' : statusIdx === 1 ? 'bg-sky-100 text-sky-700' : 'bg-gray-100 text-gray-600'

  return (
    <div className="rounded-lg border border-slate-200 p-4">
      <div className="mb-3 flex items-center justify-between">
        <h3 className="text-sm font-semibold text-slate-700">การยื่น สปส.1-10 + ใบเสร็จ (กระทบยอด)</h3>
        <span className={`rounded px-2 py-0.5 text-xs ${statusCls}`}>{STATUS[statusIdx]}</span>
      </div>

      {/* recon badges */}
      {fs && (statusIdx >= 1) && (
        <div className="mb-3 flex flex-wrap gap-2 text-xs">
          <span className={`rounded px-2 py-1 ${fs.payrollMatch ? 'bg-emerald-50 text-emerald-700' : 'bg-amber-50 text-amber-700'}`}>
            {fs.payrollMatch ? '✓' : '⚠'} ยอดที่ยื่น {fs.payrollMatch ? 'ตรงกับงวดปัจจุบัน' : `≠ งวดปัจจุบัน (ยื่นไว้ ${fmt(fs.snapshotGrandTotal)})`}
          </span>
          {(fs.receiptAmount != null) && (
            <span className={`rounded px-2 py-1 ${fs.receiptMatch ? 'bg-emerald-50 text-emerald-700' : 'bg-amber-50 text-amber-700'}`}>
              {fs.receiptMatch ? '✓' : '⚠'} ใบเสร็จ {fs.receiptMatch ? 'ตรงยอดนำส่ง' : `≠ ยอดนำส่ง (${fmt(d.grandTotal)})`}
            </span>
          )}
        </div>
      )}

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <label className="text-xs text-gray-600">
          วันที่ยื่น
          <input type="date" value={submittedDate} onChange={(e) => setSubmittedDate(e.target.value)}
            className="mt-1 w-full rounded-md border border-gray-300 px-2 py-1 text-sm" />
        </label>
        <label className="text-xs text-gray-600">
          วันที่ใบเสร็จ
          <input type="date" value={receiptDate} onChange={(e) => setReceiptDate(e.target.value)}
            className="mt-1 w-full rounded-md border border-gray-300 px-2 py-1 text-sm" />
        </label>
        <label className="text-xs text-gray-600">
          ยอดในใบเสร็จ
          <input type="number" step="0.01" value={receiptAmount} onChange={(e) => setReceiptAmount(e.target.value)}
            placeholder={fmt(d.grandTotal)}
            className="mt-1 w-full rounded-md border border-gray-300 px-2 py-1 text-right font-mono text-sm" />
        </label>
        <label className="text-xs text-gray-600">
          เลขที่ใบเสร็จ
          <input type="text" value={receiptNo} onChange={(e) => setReceiptNo(e.target.value)}
            className="mt-1 w-full rounded-md border border-gray-300 px-2 py-1 text-sm" />
        </label>
        <label className="text-xs text-gray-600 sm:col-span-2">
          หมายเหตุ
          <input type="text" value={note} onChange={(e) => setNote(e.target.value)}
            className="mt-1 w-full rounded-md border border-gray-300 px-2 py-1 text-sm" />
        </label>
      </div>

      {/* แนบไฟล์ */}
      <div className="mt-3 flex flex-wrap items-center gap-4 text-xs">
        <FileField label="แบบที่ยื่น" has={!!fs?.hasForm}
          onPick={(f) => upload('form', f)}
          onDownload={async () => download(await payrollApi.downloadSsoFilingDoc(runId, companyId, 'form'), `sso-form-${runId}`)} />
        <FileField label="ใบเสร็จ" has={!!fs?.hasReceipt}
          onPick={(f) => upload('receipt', f)}
          onDownload={async () => download(await payrollApi.downloadSsoFilingDoc(runId, companyId, 'receipt'), `sso-receipt-${runId}`)} />
      </div>

      <div className="mt-3 flex items-center gap-3">
        <Button type="button" onClick={save} disabled={saving} className="px-4">
          {saving ? 'กำลังบันทึก...' : 'บันทึกสถานะ'}
        </Button>
        {msg && <span className="text-xs text-gray-500">{msg}</span>}
      </div>
    </div>
  )
}

function FileField({ label, has, onPick, onDownload }: {
  label: string; has: boolean; onPick: (f: File) => void; onDownload: () => void
}) {
  return (
    <div className="flex items-center gap-2">
      <span className="text-gray-600">{label}:</span>
      {has ? (
        <button type="button" onClick={onDownload} className="text-sky-600 underline">มีไฟล์ (ดาวน์โหลด)</button>
      ) : (
        <span className="text-gray-400">ยังไม่มี</span>
      )}
      <label className="cursor-pointer rounded border border-gray-300 px-2 py-0.5 text-gray-600 hover:bg-gray-50">
        {has ? 'แทนที่' : 'อัปโหลด'}
        <input type="file" className="hidden" onChange={(e) => { const f = e.target.files?.[0]; if (f) onPick(f); e.currentTarget.value = '' }} />
      </label>
    </div>
  )
}
