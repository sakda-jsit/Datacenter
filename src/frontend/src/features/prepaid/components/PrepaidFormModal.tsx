import { useMemo, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import SearchableSelect from '../../../shared/components/ui/SearchableSelect'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useAccountList } from '../../trial-balance/hooks/useTrialBalance'
import { useCreatePrepaid, usePrepaidDetail, useUpdatePrepaid } from '../hooks/usePrepaid'
import type { PrepaidExpenseInput } from '../types/prepaid.types'

interface Props {
  companyId: number
  fiscalYear: number
  editingId: number | null
  onClose: () => void
}

interface FormState {
  code: string
  name: string
  reference: string
  totalAmount: string
  startDate: string
  endDate: string
  prepaidAccountId: number
  expenseAccountId: number
  notes: string
  isActive: boolean
}

function todayIso() {
  return new Date().toISOString().slice(0, 10)
}

function blank(): FormState {
  return {
    code: '', name: '', reference: '', totalAmount: '',
    startDate: todayIso(), endDate: todayIso(),
    prepaidAccountId: 0, expenseAccountId: 0, notes: '', isActive: true,
  }
}

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function daysBetween(a: string, b: string) {
  const d1 = new Date(a)
  const d2 = new Date(b)
  return Math.floor((d2.getTime() - d1.getTime()) / 86400000) + 1
}

export default function PrepaidFormModal({ companyId, fiscalYear, editingId, onClose }: Props) {
  const { data: accounts } = useAccountList(companyId)
  const create = useCreatePrepaid(companyId)
  const update = useUpdatePrepaid(companyId)
  const { data: detail, isLoading: loadingDetail } = usePrepaidDetail(
    editingId ?? 0, companyId, fiscalYear, editingId !== null,
  )

  const [form, setForm] = useState<FormState | null>(null)
  const [error, setError] = useState('')

  const initialized = form !== null
  if (!initialized) {
    if (editingId === null) {
      setForm(blank())
    } else if (detail) {
      const c = detail.item
      setForm({
        code: c.code ?? '', name: c.name, reference: c.reference ?? '',
        totalAmount: String(c.totalAmount),
        startDate: c.startDate.slice(0, 10), endDate: c.endDate.slice(0, 10),
        prepaidAccountId: c.prepaidAccountId, expenseAccountId: c.expenseAccountId,
        notes: c.notes ?? '', isActive: c.isActive,
      })
    }
  }

  const accountOptions = useMemo(
    () =>
      (accounts ?? [])
        .filter((a) => a.isPostable)
        .map((a) => ({ value: a.id, label: `${a.accountCode} — ${a.accountName}`, searchText: `${a.accountCode} ${a.accountName}` })),
    [accounts],
  )

  if (editingId !== null && loadingDetail && !form) {
    return (
      <Overlay onClose={onClose} title="แก้ไขรายการ">
        <StateMessage centered>กำลังโหลด...</StateMessage>
      </Overlay>
    )
  }
  if (!form) return null

  const f = form
  const set = (patch: Partial<FormState>) => setForm((p) => ({ ...(p as FormState), ...patch }))

  const total = Number(f.totalAmount) || 0
  const dateValid = f.startDate <= f.endDate
  const totalDays = dateValid ? daysBetween(f.startDate, f.endDate) : 0
  const perDay = totalDays > 0 ? total / totalDays : 0
  const saving = create.isPending || update.isPending

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    if (!f.name.trim()) return setError('ต้องระบุรายละเอียด')
    if (total <= 0) return setError('มูลค่าตั้งต้นต้องมากกว่า 0')
    if (!dateValid) return setError('วันเริ่มต้องไม่เกินวันสิ้นสุด')
    if (!f.prepaidAccountId) return setError('ต้องเลือกบัญชีค่าใช้จ่ายจ่ายล่วงหน้า')
    if (!f.expenseAccountId) return setError('ต้องเลือกบัญชีค่าใช้จ่าย')

    const data: PrepaidExpenseInput = {
      code: f.code.trim() || null,
      name: f.name.trim(),
      reference: f.reference.trim() || null,
      totalAmount: total,
      startDate: f.startDate,
      endDate: f.endDate,
      prepaidAccountId: f.prepaidAccountId,
      expenseAccountId: f.expenseAccountId,
      notes: f.notes.trim() || null,
      attachmentPath: null,
      isActive: f.isActive,
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
  const numCls = inputCls + ' text-right font-mono'

  return (
    <Overlay onClose={onClose} title={editingId !== null ? 'แก้ไขค่าใช้จ่ายจ่ายล่วงหน้า' : 'เพิ่มค่าใช้จ่ายจ่ายล่วงหน้า'}>
      <form onSubmit={handleSubmit} className="px-6 py-4">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">รหัส</label>
            <input value={f.code} onChange={(e) => set({ code: e.target.value })} className={inputCls} placeholder="เช่น PP-01" />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">เอกสารอ้างอิง</label>
            <input value={f.reference} onChange={(e) => set({ reference: e.target.value })} className={inputCls} placeholder="เลขที่ใบกำกับ/สัญญา" />
          </div>
          <div className="sm:col-span-2">
            <label className="mb-1 block text-xs font-medium text-gray-600">รายละเอียด *</label>
            <input value={f.name} onChange={(e) => set({ name: e.target.value })} className={inputCls} placeholder="เช่น ค่าเบี้ยประกันภัยรถยนต์, ค่า Antivirus รายปี" />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">มูลค่าตั้งต้น *</label>
            <input type="number" step="0.01" value={f.totalAmount} onChange={(e) => set({ totalAmount: e.target.value })} className={numCls} />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">วันเริ่ม *</label>
              <input type="date" value={f.startDate} onChange={(e) => set({ startDate: e.target.value })} className={inputCls} />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">วันสิ้นสุด *</label>
              <input type="date" value={f.endDate} onChange={(e) => set({ endDate: e.target.value })} className={inputCls} />
            </div>
          </div>
        </div>

        {/* live preview */}
        {total > 0 && dateValid && (
          <div className="mt-3 rounded bg-slate-50 px-3 py-2 text-xs text-slate-600">
            ตัดจ่าย <b>{totalDays}</b> วัน · เฉลี่ยวันละ <b>{fmt(perDay)}</b> · เต็มจำนวน {fmt(total)} (เส้นตรงตามวัน)
          </div>
        )}
        {!dateValid && <div className="mt-3 rounded bg-amber-50 px-3 py-2 text-xs text-amber-700">วันเริ่มต้องไม่เกินวันสิ้นสุด</div>}

        <p className="mb-2 mt-4 text-sm font-semibold text-slate-700">บัญชี GL ที่ผูก (สำหรับ generate adjustment)</p>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <AccountField label="ค่าใช้จ่ายจ่ายล่วงหน้า (สินทรัพย์) *" value={f.prepaidAccountId} options={accountOptions} onChange={(v) => set({ prepaidAccountId: v })} />
          <AccountField label="ค่าใช้จ่าย (P&L) *" value={f.expenseAccountId} options={accountOptions} onChange={(v) => set({ expenseAccountId: v })} />
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

function AccountField({
  label, value, options, onChange,
}: {
  label: string
  value: number
  options: { value: number; label: string; searchText: string }[]
  onChange: (v: number) => void
}) {
  return (
    <div>
      <label className="mb-1 block text-xs font-medium text-gray-600">{label}</label>
      <SearchableSelect value={value} options={options} onChange={(v) => onChange(Number(v))} placeholder="เลือกบัญชี" />
    </div>
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
