import { useMemo, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import SearchableSelect from '../../../shared/components/ui/SearchableSelect'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useAccountList } from '../../trial-balance/hooks/useTrialBalance'
import { useCreateInterestLoan, useInterestLoanDetail, useUpdateInterestLoan } from '../hooks/useInterestIncome'
import type { InterestLoanInput } from '../types/interestincome.types'

interface Props {
  companyId: number
  fiscalYear: number
  editingId: number | null
  onClose: () => void
}

interface MovementRow { date: string; amount: string; description: string }

interface FormState {
  name: string
  reference: string
  annualRatePct: string
  sbtRatePct: string
  localTaxPctOfSbt: string
  dayCountBasis: string
  interestReceivableAccountId: number
  interestIncomeAccountId: number
  notes: string
  isActive: boolean
  movements: MovementRow[]
}

function blank(fiscalYear: number): FormState {
  return {
    name: '', reference: '', annualRatePct: '', sbtRatePct: '3', localTaxPctOfSbt: '10', dayCountBasis: '365',
    interestReceivableAccountId: 0, interestIncomeAccountId: 0, notes: '', isActive: true,
    movements: [{ date: `${fiscalYear}-01-01`, amount: '', description: '' }],
  }
}

export default function InterestLoanFormModal({ companyId, fiscalYear, editingId, onClose }: Props) {
  const { data: accounts } = useAccountList(companyId)
  const create = useCreateInterestLoan(companyId)
  const update = useUpdateInterestLoan(companyId)
  const { data: detail, isLoading: loadingDetail } = useInterestLoanDetail(editingId ?? 0, companyId, fiscalYear, editingId !== null)

  const [form, setForm] = useState<FormState | null>(null)
  const [error, setError] = useState('')

  if (form === null) {
    if (editingId === null) setForm(blank(fiscalYear))
    else if (detail) {
      const c = detail.item
      setForm({
        name: c.name, reference: c.reference ?? '',
        annualRatePct: String(c.annualRatePct), sbtRatePct: String(c.sbtRatePct),
        localTaxPctOfSbt: String(c.localTaxPctOfSbt), dayCountBasis: String(c.dayCountBasis),
        interestReceivableAccountId: c.interestReceivableAccountId, interestIncomeAccountId: c.interestIncomeAccountId,
        notes: c.notes ?? '', isActive: c.isActive,
        movements: c.movements.length > 0
          ? c.movements.map((m) => ({ date: m.date.slice(0, 10), amount: String(m.amount), description: m.description ?? '' }))
          : [{ date: `${fiscalYear}-01-01`, amount: '', description: '' }],
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
    return <Overlay onClose={onClose} title="แก้ไขเงินให้กู้"><StateMessage centered>กำลังโหลด...</StateMessage></Overlay>
  }
  if (!form) return null

  const f = form
  const set = (patch: Partial<FormState>) => setForm((p) => ({ ...(p as FormState), ...patch }))
  const setMov = (i: number, patch: Partial<MovementRow>) =>
    setForm((p) => {
      const m = [...(p as FormState).movements]
      m[i] = { ...m[i], ...patch }
      return { ...(p as FormState), movements: m }
    })
  const addMov = () => set({ movements: [...f.movements, { date: `${fiscalYear}-01-01`, amount: '', description: '' }] })
  const delMov = (i: number) => set({ movements: f.movements.filter((_, idx) => idx !== i) })

  const saving = create.isPending || update.isPending
  const inputCls = 'w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400'

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    if (!f.name.trim()) return setError('ต้องระบุชื่อ/ผู้กู้')
    if (!f.interestReceivableAccountId) return setError('ต้องเลือกบัญชีดอกเบี้ยค้างรับ')
    if (!f.interestIncomeAccountId) return setError('ต้องเลือกบัญชีรายได้ดอกเบี้ย')
    const movements = f.movements
      .filter((m) => m.amount !== '' && Number(m.amount) !== 0)
      .map((m) => ({ date: m.date, amount: Number(m.amount), description: m.description.trim() || null }))
    if (movements.length === 0) return setError('ต้องมีรายการเงินต้นอย่างน้อย 1 รายการ')

    const data: InterestLoanInput = {
      name: f.name.trim(),
      reference: f.reference.trim() || null,
      annualRatePct: Number(f.annualRatePct) || 0,
      sbtRatePct: Number(f.sbtRatePct) || 0,
      localTaxPctOfSbt: Number(f.localTaxPctOfSbt) || 0,
      dayCountBasis: Number(f.dayCountBasis) || 365,
      interestReceivableAccountId: f.interestReceivableAccountId,
      interestIncomeAccountId: f.interestIncomeAccountId,
      notes: f.notes.trim() || null,
      attachmentPath: null,
      isActive: f.isActive,
      movements,
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

  return (
    <Overlay onClose={onClose} title={editingId !== null ? 'แก้ไขเงินให้กู้' : 'เพิ่มเงินให้กู้'}>
      <form onSubmit={handleSubmit} className="px-6 py-4">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div className="sm:col-span-2">
            <label className="mb-1 block text-xs font-medium text-gray-600">ชื่อ/ผู้กู้ *</label>
            <input value={f.name} onChange={(e) => set({ name: e.target.value })} className={inputCls} placeholder="เช่น เงินให้กู้ยืมกรรมการ - นายสมชาย" />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">เอกสารอ้างอิง</label>
            <input value={f.reference} onChange={(e) => set({ reference: e.target.value })} className={inputCls} placeholder="เลขที่สัญญา" />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">อัตราดอกเบี้ย/ปี (%) *</label>
            <input type="number" step="0.0001" value={f.annualRatePct} onChange={(e) => set({ annualRatePct: e.target.value })} className={inputCls + ' text-right font-mono'} />
          </div>
          <div className="grid grid-cols-3 gap-2 sm:col-span-2">
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">ภาษีธุรกิจเฉพาะ (%)</label>
              <input type="number" step="0.0001" value={f.sbtRatePct} onChange={(e) => set({ sbtRatePct: e.target.value })} className={inputCls + ' text-right font-mono'} />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">ส่วนท้องถิ่น (% ของ SBT)</label>
              <input type="number" step="0.0001" value={f.localTaxPctOfSbt} onChange={(e) => set({ localTaxPctOfSbt: e.target.value })} className={inputCls + ' text-right font-mono'} />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">ฐานวัน/ปี</label>
              <input type="number" value={f.dayCountBasis} onChange={(e) => set({ dayCountBasis: e.target.value })} className={inputCls + ' text-right font-mono'} />
            </div>
          </div>
        </div>

        {/* movements */}
        <div className="mt-4 flex items-center justify-between">
          <p className="text-sm font-semibold text-slate-700">รายการเงินต้น (+ ให้กู้เพิ่ม / − รับคืน)</p>
          <Button type="button" variant="ghost" onClick={addMov} className="px-2 py-1 text-xs text-sky-600">+ เพิ่มแถว</Button>
        </div>
        <div className="overflow-hidden rounded border border-gray-200">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-xs text-gray-600">
              <tr>
                <th className="px-2 py-2 text-left font-medium w-40">วันที่</th>
                <th className="px-2 py-2 text-right font-medium w-40">จำนวน (+/−)</th>
                <th className="px-2 py-2 text-left font-medium">คำอธิบาย</th>
                <th className="px-2 py-2 w-10" />
              </tr>
            </thead>
            <tbody>
              {f.movements.map((m, i) => (
                <tr key={i} className="border-t border-gray-100">
                  <td className="px-2 py-1.5">
                    <input type="date" value={m.date} onChange={(e) => setMov(i, { date: e.target.value })} className="w-full rounded border border-gray-300 px-2 py-1 text-sm" />
                  </td>
                  <td className="px-2 py-1.5">
                    <input type="number" step="0.01" value={m.amount} onChange={(e) => setMov(i, { amount: e.target.value })} className="w-full rounded border border-gray-300 px-2 py-1 text-right font-mono text-sm" />
                  </td>
                  <td className="px-2 py-1.5">
                    <input value={m.description} onChange={(e) => setMov(i, { description: e.target.value })} className="w-full rounded border border-gray-300 px-2 py-1 text-sm" placeholder="เช่น เบิกเพิ่ม, รับชำระคืน" />
                  </td>
                  <td className="px-2 py-1.5 text-center">
                    {f.movements.length > 1 && (
                      <button type="button" onClick={() => delMov(i)} className="text-red-400 hover:text-red-600">×</button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <p className="mb-2 mt-4 text-sm font-semibold text-slate-700">บัญชี GL ที่ผูก (สำหรับ generate adjustment)</p>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ดอกเบี้ยค้างรับ (สินทรัพย์) *</label>
            <SearchableSelect value={f.interestReceivableAccountId} options={accountOptions} onChange={(v) => set({ interestReceivableAccountId: Number(v) })} placeholder="เลือกบัญชี" />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">รายได้ดอกเบี้ย (รายได้) *</label>
            <SearchableSelect value={f.interestIncomeAccountId} options={accountOptions} onChange={(v) => set({ interestIncomeAccountId: Number(v) })} placeholder="เลือกบัญชี" />
          </div>
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
