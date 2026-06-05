import { useMemo, useState } from 'react'
import Button from '../../../../shared/components/ui/Button'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import ExportMenu from '../../../../shared/components/ui/ExportMenu'
import { useWhtEntries } from '../../hooks/useWht'
import { EMAIL_STATUS_CLASS, EMAIL_STATUS_LABEL, MONTH_LABEL, WHT_FORM_LABEL } from '../../types/wht.types'
import type { WhtSendResult } from '../../types/wht.types'
import type { ExportSection } from '../../../../shared/utils/exportTable'
import PayeeEmailModal, { type PayeeRow } from '../../components/PayeeEmailModal'
import CertificatePreviewModal from '../../components/CertificatePreviewModal'
import WhtSendModal from '../../components/WhtSendModal'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function fmtDateTime(s?: string) {
  if (!s) return ''
  const d = new Date(s)
  return d.toLocaleString('th-TH', { dateStyle: 'short', timeStyle: 'short' })
}

interface Props {
  companyId: number
  year: number
}

export default function WhtEntriesTab({ companyId, year }: Props) {
  const [month, setMonth] = useState(0)
  const [form, setForm] = useState(0) // 0 = ทั้งหมด
  const { data, isLoading, isError } = useWhtEntries(companyId, year, month, form || undefined)

  const [selected, setSelected] = useState<Set<number>>(new Set())
  const [emailModal, setEmailModal] = useState(false)
  const [previewIds, setPreviewIds] = useState<number[] | null>(null)
  const [sendOpen, setSendOpen] = useState(false)
  const [sendResult, setSendResult] = useState<WhtSendResult[] | null>(null)
  const [error, setError] = useState('')

  const rows = useMemo(() => data ?? [], [data])

  const payeeRows = useMemo<PayeeRow[]>(() => {
    const map = new Map<string, PayeeRow>()
    for (const r of rows) {
      const tax = r.payeeTaxId ?? ''
      if (!tax || map.has(tax)) continue
      map.set(tax, { taxId: tax, name: r.payeeName ?? '', email: r.payeeEmail ?? '' })
    }
    return [...map.values()]
  }, [rows])

  if (!companyId) {
    return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>
  }

  function toggle(id: number) {
    setSelected((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }
  function toggleAll() {
    setSelected((prev) => (prev.size === rows.length ? new Set() : new Set(rows.map((r) => r.id))))
  }

  function handleSent(res: WhtSendResult[]) {
    setError('')
    setSendResult(res)
    setSelected(new Set())
  }

  return (
    <div>
      <Card className="mb-4 p-4">
        <div className="flex flex-wrap items-end justify-between gap-3">
          <div className="flex flex-wrap items-end gap-3">
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">เดือนภาษี</label>
              <select value={month} onChange={(e) => { setMonth(Number(e.target.value)); setSelected(new Set()) }} className="rounded border border-gray-300 px-3 py-2 text-sm">
                <option value={0}>ทั้งปี</option>
                {Array.from({ length: 12 }, (_, i) => i + 1).map((m) => (
                  <option key={m} value={m}>{MONTH_LABEL[m]}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">แบบ</label>
              <select value={form} onChange={(e) => { setForm(Number(e.target.value)); setSelected(new Set()) }} className="rounded border border-gray-300 px-3 py-2 text-sm">
                <option value={0}>ทั้งหมด</option>
                <option value={3}>ภ.ง.ด.3 (บุคคลธรรมดา)</option>
                <option value={53}>ภ.ง.ด.53 (นิติบุคคล)</option>
              </select>
            </div>
          </div>
          <div className="flex items-end gap-2">
            <Button type="button" variant="secondary" onClick={() => setEmailModal(true)} disabled={payeeRows.length === 0}>
              อีเมลผู้ถูกหัก
            </Button>
            <Button type="button" variant="secondary" onClick={() => setPreviewIds([...selected])} disabled={selected.size === 0}>
              Preview หัก ณ ที่จ่าย ({selected.size})
            </Button>
            <Button type="button" onClick={() => setSendOpen(true)} disabled={selected.size === 0}>
              ส่งเมล ({selected.size})
            </Button>
          </div>
        </div>
      </Card>

      {error && <StateMessage tone="error">{error}</StateMessage>}
      {sendResult && (
        <Card className="mb-4 px-4 py-3">
          <p className="mb-1 text-sm font-semibold text-slate-800">ผลการส่งอีเมล</p>
          <ul className="space-y-1 text-xs">
            {sendResult.map((r) => (
              <li key={r.payeeTaxId} className={r.success ? 'text-green-700' : 'text-red-600'}>
                {r.success ? '✓' : '✗'} {r.payeeName} ({r.email || 'ไม่มีอีเมล'}) — {r.entryCount} ฉบับ
                {r.error ? ` · ${r.error}` : ' · ส่งสำเร็จ'}
              </li>
            ))}
          </ul>
        </Card>
      )}

      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {data && rows.length === 0 && (
        <Card><StateMessage centered>ไม่มีรายการตามเงื่อนไขที่เลือก</StateMessage></Card>
      )}

      {rows.length > 0 && (
        <Card className="overflow-x-auto">
          <div className="flex items-start justify-between border-b px-4 py-3">
            <p className="text-sm font-semibold text-slate-800">
              รายละเอียดภาษีหัก ณ ที่จ่าย · {month === 0 ? `ทั้งปี ${year}` : `${MONTH_LABEL[month]} ${year}`} ({rows.length} รายการ)
            </p>
            <ExportMenu
              meta={{
                title: `รายละเอียดภาษีหัก ณ ที่จ่าย ปี ${year}${month ? ' เดือน ' + MONTH_LABEL[month] : ''}`,
                fileName: `wht-entries-${companyId}-${year}${month ? '-' + month : ''}`,
              }}
              getSections={(): ExportSection[] => [
                {
                  name: 'รายการหัก ณ ที่จ่าย',
                  columns: [
                    { key: 'taxPeriod', header: 'เดือนภาษี', value: (r) => String(r.taxPeriod).slice(0, 7) },
                    { key: 'formType', header: 'แบบ', value: (r) => WHT_FORM_LABEL[r.formType] },
                    { key: 'payeeName', header: 'ผู้ถูกหัก' },
                    { key: 'payeeTaxId', header: 'เลขผู้เสียภาษี' },
                    { key: 'payeeEmail', header: 'อีเมล' },
                    { key: 'incomeType', header: 'ประเภทเงินได้' },
                    { key: 'baseAmount', header: 'จำนวนเงิน', align: 'right' },
                    { key: 'taxAmount', header: 'ภาษีหัก', align: 'right' },
                    { key: 'emailStatus', header: 'สถานะส่งเมล', value: (r) => EMAIL_STATUS_LABEL[r.emailStatus] },
                  ],
                  rows,
                },
              ]}
            />
          </div>
          <table className="w-full text-xs">
            <thead className="bg-slate-50 text-gray-600">
              <tr>
                <th className="px-3 py-2 text-left font-medium">
                  <input type="checkbox" checked={selected.size === rows.length && rows.length > 0} onChange={toggleAll} className="mr-2 rounded" />
                  ผู้ถูกหัก
                </th>
                <th className="px-3 py-2 text-left font-medium">แบบ</th>
                <th className="px-3 py-2 text-left font-medium">อีเมล</th>
                <th className="px-3 py-2 text-left font-medium">ประเภทเงินได้</th>
                <th className="px-3 py-2 text-right font-medium">จำนวนเงิน</th>
                <th className="px-3 py-2 text-right font-medium">ภาษีหัก</th>
                <th className="px-3 py-2 text-left font-medium">สถานะส่งเมล</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => (
                <tr key={r.id} className="border-t border-gray-100 hover:bg-slate-50">
                  <td className="px-3 py-1.5">
                    <label className="flex items-start gap-2">
                      <input type="checkbox" checked={selected.has(r.id)} onChange={() => toggle(r.id)} className="mt-0.5 rounded" />
                      <span>
                        {(r.payeePrefix ? r.payeePrefix + ' ' : '') + (r.payeeName || '—')}
                        <span className="block font-mono text-[10px] text-gray-400">{r.payeeTaxId} · {String(r.taxPeriod).slice(0, 7)}</span>
                      </span>
                    </label>
                  </td>
                  <td className="px-3 py-1.5">
                    <span className={r.formType === 3 ? 'text-sky-700' : 'text-amber-700'}>{WHT_FORM_LABEL[r.formType]}</span>
                  </td>
                  <td className="px-3 py-1.5">
                    {r.payeeEmail
                      ? <span className="text-gray-700">{r.payeeEmail}</span>
                      : <span className="text-[10px] text-amber-600">ยังไม่กำหนด</span>}
                  </td>
                  <td className="px-3 py-1.5 text-gray-600">{r.incomeType || '—'}</td>
                  <td className="px-3 py-1.5 text-right font-mono">{fmt(r.baseAmount)}</td>
                  <td className="px-3 py-1.5 text-right font-mono">{fmt(r.taxAmount)}</td>
                  <td className="px-3 py-1.5">
                    <span className={`inline-block rounded px-2 py-0.5 text-[11px] ${EMAIL_STATUS_CLASS[r.emailStatus]}`}>
                      {EMAIL_STATUS_LABEL[r.emailStatus]}
                    </span>
                    {r.emailStatus === 2 && (
                      <span className="block text-[10px] text-gray-400">{fmtDateTime(r.emailSentAt)} · {r.emailSentBy}</span>
                    )}
                    {r.emailStatus === 3 && r.emailError && (
                      <span className="block text-[10px] text-red-500" title={r.emailError}>{r.emailError}</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
                <td className="px-3 py-2" colSpan={4}>รวม {rows.length} รายการ</td>
                <td className="px-3 py-2 text-right font-mono">{fmt(rows.reduce((s, r) => s + r.baseAmount, 0))}</td>
                <td className="px-3 py-2 text-right font-mono">{fmt(rows.reduce((s, r) => s + r.taxAmount, 0))}</td>
                <td className="px-3 py-2" />
              </tr>
            </tfoot>
          </table>
        </Card>
      )}

      {emailModal && (
        <PayeeEmailModal companyId={companyId} payees={payeeRows} onClose={() => setEmailModal(false)} />
      )}
      {previewIds && previewIds.length > 0 && (
        <CertificatePreviewModal companyId={companyId} entryIds={previewIds} onClose={() => setPreviewIds(null)} />
      )}
      {sendOpen && (
        <WhtSendModal
          companyId={companyId}
          entryIds={[...selected]}
          onClose={() => setSendOpen(false)}
          onSent={handleSent}
        />
      )}
    </div>
  )
}
