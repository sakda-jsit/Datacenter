import { useMemo, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import SearchableSelect from '../../../shared/components/ui/SearchableSelect'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useAccountList } from '../../trial-balance/hooks/useTrialBalance'
import { useCashCountDetail, useCreateCashCount, useUpdateCashCount } from '../hooks/useCashCount'
import type { CashCountInput } from '../types/cashcount.types'

// ชนิดธนบัตร/เหรียญมาตรฐาน (มากไปน้อย)
const DENOMINATIONS = [1000, 500, 100, 50, 20, 10, 5, 2, 1, 0.5, 0.25]

interface Props {
  companyId: number
  fiscalYear: number
  editingId: number | null
  onClose: () => void
}

interface FormState {
  countDate: string
  reference: string
  cashAccountId: number
  notes: string
  isActive: boolean
  qty: Record<string, string> // denomination -> qty
}

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function blank(fiscalYear: number): FormState {
  return {
    countDate: `${fiscalYear}-12-31`,
    reference: '', cashAccountId: 0, notes: '', isActive: true,
    qty: Object.fromEntries(DENOMINATIONS.map((d) => [String(d), ''])),
  }
}

export default function CashCountFormModal({ companyId, fiscalYear, editingId, onClose }: Props) {
  const { data: accounts } = useAccountList(companyId)
  const create = useCreateCashCount(companyId)
  const update = useUpdateCashCount(companyId)
  const { data: detail, isLoading: loadingDetail } = useCashCountDetail(editingId ?? 0, companyId, editingId !== null)

  const [form, setForm] = useState<FormState | null>(null)
  const [error, setError] = useState('')

  if (form === null) {
    if (editingId === null) setForm(blank(fiscalYear))
    else if (detail) {
      const q = Object.fromEntries(DENOMINATIONS.map((d) => [String(d), '']))
      for (const l of detail.lines) q[String(l.denomination)] = String(l.quantity)
      setForm({
        countDate: detail.countDate.slice(0, 10),
        reference: detail.reference ?? '',
        cashAccountId: detail.cashAccountId,
        notes: detail.notes ?? '',
        isActive: detail.isActive,
        qty: q,
      })
    }
  }

  const accountOptions = useMemo(
    () => (accounts ?? [])
      .filter((a) => a.isPostable)
      .map((a) => ({ value: a.id, label: `${a.accountCode} — ${a.accountName}`, searchText: `${a.accountCode} ${a.accountName}` })),
    [accounts],
  )

  if (editingId !== null && loadingDetail && !form) {
    return <Overlay onClose={onClose} title="แก้ไขใบตรวจนับ"><StateMessage centered>กำลังโหลด...</StateMessage></Overlay>
  }
  if (!form) return null

  const f = form
  const set = (patch: Partial<FormState>) => setForm((p) => ({ ...(p as FormState), ...patch }))
  const setQty = (d: number, v: string) => setForm((p) => ({ ...(p as FormState), qty: { ...(p as FormState).qty, [String(d)]: v } }))

  const total = DENOMINATIONS.reduce((s, d) => s + d * (Number(f.qty[String(d)]) || 0), 0)
  const saving = create.isPending || update.isPending

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    if (!f.cashAccountId) return setError('ต้องเลือกบัญชีเงินสด')
    const lines = DENOMINATIONS
      .map((d) => ({ denomination: d, quantity: Number(f.qty[String(d)]) || 0 }))
      .filter((l) => l.quantity > 0)
    if (lines.length === 0) return setError('ต้องระบุจำนวนอย่างน้อย 1 ชนิด')

    const data: CashCountInput = {
      fiscalYear,
      countDate: f.countDate,
      reference: f.reference.trim() || null,
      cashAccountId: f.cashAccountId,
      notes: f.notes.trim() || null,
      attachmentPath: null,
      isActive: f.isActive,
      lines,
    }
    try {
      if (editingId !== null) await update.mutateAsync({ id: editingId, data })
      else await create.mutateAsync(data)
      onClose()
    } catch (err) {
      const msg = (err as { response?: { data?: { detail?: string; title?: string } } })?.response?.data
      setError(msg?.detail ?? msg?.title ?? 'บันทึกไม่สำเร็จ')
    }
  }

  const inputCls = 'w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400'

  return (
    <Overlay onClose={onClose} title={editingId !== null ? 'แก้ไขใบตรวจนับเงินสด' : 'เพิ่มใบตรวจนับเงินสด'}>
      <form onSubmit={handleSubmit} className="px-6 py-4">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">วันที่นับ *</label>
            <input type="date" value={f.countDate} onChange={(e) => set({ countDate: e.target.value })} className={inputCls} />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">จุดเก็บ/อ้างอิง</label>
            <input value={f.reference} onChange={(e) => set({ reference: e.target.value })} className={inputCls} placeholder="เช่น เงินสดย่อย, เงินสดในมือ" />
          </div>
          <div className="sm:col-span-2">
            <label className="mb-1 block text-xs font-medium text-gray-600">บัญชีเงินสด (GL) *</label>
            <SearchableSelect value={f.cashAccountId} options={accountOptions} onChange={(v) => set({ cashAccountId: Number(v) })} placeholder="เลือกบัญชีเงินสด" />
          </div>
        </div>

        <p className="mb-2 mt-4 text-sm font-semibold text-slate-700">รายการนับ (ชนิด × จำนวน)</p>
        <div className="overflow-hidden rounded border border-gray-200">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-xs text-gray-600">
              <tr>
                <th className="px-3 py-2 text-right font-medium w-32">มูลค่าหน้าตั๋ว</th>
                <th className="px-3 py-2 text-center font-medium w-32">จำนวน</th>
                <th className="px-3 py-2 text-right font-medium">มูลค่ารวม</th>
              </tr>
            </thead>
            <tbody>
              {DENOMINATIONS.map((d) => {
                const qty = Number(f.qty[String(d)]) || 0
                return (
                  <tr key={d} className="border-t border-gray-100">
                    <td className="px-3 py-1.5 text-right font-mono text-slate-700">{fmt(d)}</td>
                    <td className="px-3 py-1.5 text-center">
                      <input
                        type="number" min={0} value={f.qty[String(d)]}
                        onChange={(e) => setQty(d, e.target.value)}
                        className="w-24 rounded border border-gray-300 px-2 py-1 text-right text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
                      />
                    </td>
                    <td className="px-3 py-1.5 text-right font-mono text-slate-600">{qty > 0 ? fmt(d * qty) : '-'}</td>
                  </tr>
                )
              })}
            </tbody>
            <tfoot>
              <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
                <td className="px-3 py-2 text-right" colSpan={2}>รวมนับได้</td>
                <td className="px-3 py-2 text-right font-mono text-slate-800">{fmt(total)}</td>
              </tr>
            </tfoot>
          </table>
        </div>

        <div className="mt-3">
          <label className="mb-1 block text-xs font-medium text-gray-600">หมายเหตุ</label>
          <input value={f.notes} onChange={(e) => set({ notes: e.target.value })} className={inputCls} />
        </div>
        <label className="mt-3 flex items-center gap-2 text-sm text-gray-600">
          <input type="checkbox" checked={f.isActive} onChange={(e) => set({ isActive: e.target.checked })} className="rounded" />
          ใช้งาน (active)
        </label>

        {error && <p className="mt-3 rounded bg-red-50 px-3 py-2 text-sm text-red-600">{error}</p>}

        <div className="mt-5 flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose}>ยกเลิก</Button>
          <Button type="submit" disabled={saving}>{saving ? 'กำลังบันทึก...' : 'บันทึก'}</Button>
        </div>
      </form>
    </Overlay>
  )
}

function Overlay({ title, children, onClose }: { title: string; children: React.ReactNode; onClose: () => void }) {
  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm">
      <div className="my-8 w-full max-w-2xl rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <h2 className="text-lg font-bold text-slate-800">{title}</h2>
          <button type="button" onClick={onClose} className="text-2xl leading-none text-slate-400 hover:text-slate-600">×</button>
        </div>
        {children}
      </div>
    </div>
  )
}
