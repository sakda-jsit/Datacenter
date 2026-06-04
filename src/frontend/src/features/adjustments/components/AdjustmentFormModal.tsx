import { useMemo, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import SearchableSelect from '../../../shared/components/ui/SearchableSelect'
import { useAccountList } from '../../trial-balance/hooks/useTrialBalance'
import { useCreateAdjustment, useUpdateAdjustment } from '../hooks/useAdjustments'
import { SOURCE_TYPE_OPTIONS } from '../types/adjustment.types'
import type { AdjustmentEntryDto } from '../types/adjustment.types'

interface LineRow {
  accountId: number
  debit: string
  credit: string
  description: string
}

interface Props {
  companyId: number
  fiscalYear: number
  /** entry ที่กำลังแก้ไข (null = สร้างใหม่) */
  editing: AdjustmentEntryDto | null
  onClose: () => void
}

function emptyLine(): LineRow {
  return { accountId: 0, debit: '', credit: '', description: '' }
}

function todayIso() {
  return new Date().toISOString().slice(0, 10)
}

export default function AdjustmentFormModal({ companyId, fiscalYear, editing, onClose }: Props) {
  const { data: accounts } = useAccountList(companyId)
  const create = useCreateAdjustment(companyId, fiscalYear)
  const update = useUpdateAdjustment(companyId, fiscalYear)

  const [entryDate, setEntryDate] = useState(editing ? editing.entryDate.slice(0, 10) : todayIso())
  const [sourceType, setSourceType] = useState<number>(editing?.sourceType ?? 0)
  const [reference, setReference] = useState(editing?.reference ?? '')
  const [reason, setReason] = useState(editing?.reason ?? '')
  const [attachmentPath, setAttachmentPath] = useState(editing?.attachmentPath ?? '')
  const [lines, setLines] = useState<LineRow[]>(
    editing
      ? editing.lines.map((l) => ({
          accountId: l.accountId,
          debit: l.debitAmount ? String(l.debitAmount) : '',
          credit: l.creditAmount ? String(l.creditAmount) : '',
          description: l.description ?? '',
        }))
      : [emptyLine(), emptyLine()],
  )
  const [error, setError] = useState('')

  const accountOptions = useMemo(
    () =>
      (accounts ?? [])
        .filter((a) => a.isPostable)
        .map((a) => ({
          value: a.id,
          label: `${a.accountCode} — ${a.accountName}`,
          searchText: `${a.accountCode} ${a.accountName}`,
        })),
    [accounts],
  )

  const totalDebit = lines.reduce((s, l) => s + (Number(l.debit) || 0), 0)
  const totalCredit = lines.reduce((s, l) => s + (Number(l.credit) || 0), 0)
  const balanced = Math.round((totalDebit - totalCredit) * 100) === 0 && totalDebit > 0

  function updateLine(idx: number, patch: Partial<LineRow>) {
    setLines((prev) => prev.map((l, i) => (i === idx ? { ...l, ...patch } : l)))
  }

  function fmt(n: number) {
    return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')

    const cleanLines = lines
      .filter((l) => l.accountId > 0 && ((Number(l.debit) || 0) > 0 || (Number(l.credit) || 0) > 0))
      .map((l) => ({
        accountId: l.accountId,
        debitAmount: Number(l.debit) || 0,
        creditAmount: Number(l.credit) || 0,
        description: l.description || null,
      }))

    if (cleanLines.length === 0) return setError('ต้องมีอย่างน้อย 1 บรรทัดที่ระบุบัญชีและจำนวนเงิน')
    if (cleanLines.some((l) => l.debitAmount > 0 && l.creditAmount > 0))
      return setError('แต่ละบรรทัดต้องมีเดบิตหรือเครดิตอย่างใดอย่างหนึ่ง')
    if (!balanced) return setError('รายการปรับปรุงต้องสมดุล (รวมเดบิต = รวมเครดิต)')
    if (!reason.trim()) return setError('ต้องระบุเหตุผลการปรับปรุง')

    try {
      if (editing) {
        await update.mutateAsync({
          id: editing.id,
          clientCompanyId: companyId,
          entryDate,
          sourceType,
          reference: reference || null,
          reason: reason.trim(),
          attachmentPath: attachmentPath || null,
          lines: cleanLines,
        })
      } else {
        await create.mutateAsync({
          clientCompanyId: companyId,
          fiscalYear,
          entryDate,
          sourceType,
          reference: reference || null,
          reason: reason.trim(),
          attachmentPath: attachmentPath || null,
          lines: cleanLines,
        })
      }
      onClose()
    } catch (err) {
      const msg =
        (err as { response?: { data?: { detail?: string; title?: string; errors?: Record<string, string[]> } } })
          ?.response?.data
      setError(msg?.detail ?? msg?.title ?? 'บันทึกไม่สำเร็จ')
    }
  }

  const saving = create.isPending || update.isPending

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm">
      <div className="my-8 w-full max-w-3xl rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <div>
            <h2 className="text-lg font-bold text-slate-800">
              {editing ? `แก้ไขรายการปรับปรุง ${editing.documentNo}` : 'สร้างรายการปรับปรุง'}
            </h2>
            <p className="text-xs text-gray-500">ปีบัญชี {fiscalYear}</p>
          </div>
          <button type="button" onClick={onClose} className="text-2xl leading-none text-slate-400 hover:text-slate-600">
            ×
          </button>
        </div>

        <form onSubmit={handleSubmit} className="px-6 py-4">
          {/* Header fields */}
          <div className="mb-4 grid grid-cols-1 gap-3 sm:grid-cols-2">
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">วันที่</label>
              <input
                type="date" value={entryDate} onChange={(e) => setEntryDate(e.target.value)}
                className="w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
              />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">ที่มา</label>
              <select
                value={sourceType} onChange={(e) => setSourceType(Number(e.target.value))}
                className="w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
              >
                {SOURCE_TYPE_OPTIONS.map((s) => (
                  <option key={s.value} value={s.value}>{s.label}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">อ้างอิง</label>
              <input
                value={reference} onChange={(e) => setReference(e.target.value)} placeholder="เลขสัญญา / เอกสาร"
                className="w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
              />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">ไฟล์แนบ (พาธ)</label>
              <input
                value={attachmentPath} onChange={(e) => setAttachmentPath(e.target.value)} placeholder="ไม่บังคับ"
                className="w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
              />
            </div>
            <div className="sm:col-span-2">
              <label className="mb-1 block text-xs font-medium text-gray-600">เหตุผลการปรับปรุง *</label>
              <input
                value={reason} onChange={(e) => setReason(e.target.value)} required placeholder="เช่น บันทึกค่าเสื่อมราคาเพิ่มเติม"
                className="w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
              />
            </div>
          </div>

          {/* Lines */}
          <div className="mb-2 flex items-center justify-between">
            <p className="text-sm font-semibold text-slate-700">รายการบรรทัด</p>
            <Button type="button" variant="secondary" onClick={() => setLines((p) => [...p, emptyLine()])} className="px-3 py-1 text-xs">
              + เพิ่มบรรทัด
            </Button>
          </div>
          <div className="overflow-visible rounded border border-gray-200">
            <table className="w-full text-sm">
              <thead className="bg-slate-50 text-xs text-gray-600">
                <tr>
                  <th className="px-2 py-2 text-left font-medium">บัญชี</th>
                  <th className="px-2 py-2 text-right font-medium w-32">เดบิต</th>
                  <th className="px-2 py-2 text-right font-medium w-32">เครดิต</th>
                  <th className="px-2 py-2 text-left font-medium w-40">คำอธิบาย</th>
                  <th className="w-8" />
                </tr>
              </thead>
              <tbody>
                {lines.map((line, idx) => (
                  <tr key={idx} className="border-t border-gray-100">
                    <td className="px-2 py-1.5">
                      <SearchableSelect
                        value={line.accountId}
                        options={accountOptions}
                        onChange={(v) => updateLine(idx, { accountId: Number(v) })}
                        placeholder="เลือกบัญชี"
                      />
                    </td>
                    <td className="px-2 py-1.5">
                      <input
                        type="number" step="0.01" min="0" value={line.debit}
                        onChange={(e) => updateLine(idx, { debit: e.target.value, credit: e.target.value ? '' : line.credit })}
                        className="w-full rounded border border-gray-300 px-2 py-1.5 text-right font-mono text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
                      />
                    </td>
                    <td className="px-2 py-1.5">
                      <input
                        type="number" step="0.01" min="0" value={line.credit}
                        onChange={(e) => updateLine(idx, { credit: e.target.value, debit: e.target.value ? '' : line.debit })}
                        className="w-full rounded border border-gray-300 px-2 py-1.5 text-right font-mono text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
                      />
                    </td>
                    <td className="px-2 py-1.5">
                      <input
                        value={line.description}
                        onChange={(e) => updateLine(idx, { description: e.target.value })}
                        className="w-full rounded border border-gray-300 px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
                      />
                    </td>
                    <td className="px-2 py-1.5 text-center">
                      {lines.length > 1 && (
                        <button
                          type="button" onClick={() => setLines((p) => p.filter((_, i) => i !== idx))}
                          className="text-red-400 hover:text-red-600" title="ลบบรรทัด"
                        >
                          ×
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr className={`border-t-2 ${balanced ? 'border-green-200 bg-green-50' : 'border-amber-200 bg-amber-50'}`}>
                  <td className="px-2 py-2 text-right text-xs font-semibold text-slate-600">รวม</td>
                  <td className="px-2 py-2 text-right font-mono text-sm font-semibold">{fmt(totalDebit)}</td>
                  <td className="px-2 py-2 text-right font-mono text-sm font-semibold">{fmt(totalCredit)}</td>
                  <td colSpan={2} className="px-2 py-2 text-xs">
                    {balanced ? (
                      <span className="text-green-700">✓ สมดุล</span>
                    ) : (
                      <span className="text-amber-700">ผลต่าง {fmt(totalDebit - totalCredit)}</span>
                    )}
                  </td>
                </tr>
              </tfoot>
            </table>
          </div>

          {error && <p className="mt-3 rounded bg-red-50 px-3 py-2 text-sm text-red-600">{error}</p>}

          <div className="mt-5 flex justify-end gap-2">
            <Button type="button" variant="secondary" onClick={onClose}>ยกเลิก</Button>
            <Button type="submit" disabled={saving || !balanced}>{saving ? 'กำลังบันทึก...' : 'บันทึก'}</Button>
          </div>
        </form>
      </div>
    </div>
  )
}
