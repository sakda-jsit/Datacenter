import { useMemo, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import SearchableSelect from '../../../shared/components/ui/SearchableSelect'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useAccountList } from '../../trial-balance/hooks/useTrialBalance'
import { useCreateLeaseContract, useLeaseContract, useUpdateLeaseContract } from '../hooks/useLeasing'
import { CONTRACT_TYPE_OPTIONS, ContractType } from '../types/leasing.types'
import type { LeaseContractInput } from '../types/leasing.types'

interface Props {
  companyId: number
  fiscalYear: number
  editingId: number | null
  onClose: () => void
}

interface FormState {
  contractType: number
  contractNo: string
  assetName: string
  assetCode: string
  lessor: string
  contractDate: string
  firstInstallmentDate: string
  numberOfPeriods: string
  paymentsPerYear: string
  cashPrice: string
  downPayment: string
  financedPrincipal: string
  installmentAmount: string
  vatPerPeriod: string
  liabilityAccountId: number
  deferredInterestAccountId: number
  inputVatUndueAccountId: number
  interestExpenseAccountId: number
  notes: string
  isActive: boolean
}

function todayIso() {
  return new Date().toISOString().slice(0, 10)
}

function blank(): FormState {
  return {
    contractType: ContractType.HirePurchase,
    contractNo: '', assetName: '', assetCode: '', lessor: '',
    contractDate: todayIso(), firstInstallmentDate: todayIso(),
    numberOfPeriods: '', paymentsPerYear: '12',
    cashPrice: '', downPayment: '', financedPrincipal: '', installmentAmount: '', vatPerPeriod: '',
    liabilityAccountId: 0, deferredInterestAccountId: 0, inputVatUndueAccountId: 0, interestExpenseAccountId: 0,
    notes: '', isActive: true,
  }
}

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

export default function LeaseContractFormModal({ companyId, fiscalYear, editingId, onClose }: Props) {
  const { data: accounts } = useAccountList(companyId)
  const create = useCreateLeaseContract(companyId)
  const update = useUpdateLeaseContract(companyId)
  const { data: detail, isLoading: loadingDetail } = useLeaseContract(
    editingId ?? 0, companyId, fiscalYear, editingId !== null,
  )

  const [form, setForm] = useState<FormState | null>(null)
  const [error, setError] = useState('')

  // init form เมื่อโหลด detail เสร็จ (edit) หรือทันที (create)
  const initialized = form !== null
  if (!initialized) {
    if (editingId === null) {
      setForm(blank())
    } else if (detail) {
      const c = detail.contract
      setForm({
        contractType: c.contractType,
        contractNo: c.contractNo, assetName: c.assetName, assetCode: c.assetCode ?? '', lessor: c.lessor ?? '',
        contractDate: c.contractDate.slice(0, 10), firstInstallmentDate: c.firstInstallmentDate.slice(0, 10),
        numberOfPeriods: String(c.numberOfPeriods), paymentsPerYear: String(c.paymentsPerYear),
        cashPrice: String(c.cashPrice), downPayment: String(c.downPayment),
        financedPrincipal: String(c.financedPrincipal), installmentAmount: String(c.installmentAmount),
        vatPerPeriod: String(c.vatPerPeriod),
        liabilityAccountId: c.liabilityAccountId, deferredInterestAccountId: c.deferredInterestAccountId ?? 0,
        inputVatUndueAccountId: c.inputVatUndueAccountId ?? 0, interestExpenseAccountId: c.interestExpenseAccountId,
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
      <Overlay onClose={onClose} title="แก้ไขสัญญา">
        <StateMessage centered>กำลังโหลด...</StateMessage>
      </Overlay>
    )
  }
  if (!form) return null

  const f = form
  const set = (patch: Partial<FormState>) => setForm((p) => ({ ...(p as FormState), ...patch }))

  const isHP = f.contractType === ContractType.HirePurchase
  const principal = Number(f.financedPrincipal) || 0
  const installment = Number(f.installmentAmount) || 0
  const periods = Number(f.numberOfPeriods) || 0
  const totalPayable = installment * periods
  const totalInterest = totalPayable - principal
  const interestValid = totalInterest >= -0.005

  const saving = create.isPending || update.isPending

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')

    if (!f.contractNo.trim()) return setError('ต้องระบุเลขที่สัญญา')
    if (!f.assetName.trim()) return setError('ต้องระบุชื่อทรัพย์สิน/รายการ')
    if (periods < 1) return setError('จำนวนงวดต้องมากกว่า 0')
    if (principal <= 0) return setError('เงินต้นที่จัดไฟแนนซ์ต้องมากกว่า 0')
    if (installment <= 0) return setError('ค่างวดต้องมากกว่า 0')
    if (!interestValid) return setError('ค่างวด × จำนวนงวด ต้องไม่น้อยกว่าเงินต้น')
    if (!f.liabilityAccountId) return setError('ต้องเลือกบัญชีหนี้สิน')
    if (!f.interestExpenseAccountId) return setError('ต้องเลือกบัญชีดอกเบี้ยจ่าย')
    if (isHP && Number(f.vatPerPeriod) > 0 && !f.inputVatUndueAccountId)
      return setError('เช่าซื้อที่มี VAT ต้องเลือกบัญชีภาษีซื้อยังไม่ถึงกำหนด')

    const data: LeaseContractInput = {
      contractType: f.contractType,
      contractNo: f.contractNo.trim(),
      assetName: f.assetName.trim(),
      assetCode: f.assetCode.trim() || null,
      lessor: f.lessor.trim() || null,
      contractDate: f.contractDate,
      firstInstallmentDate: f.firstInstallmentDate,
      numberOfPeriods: periods,
      paymentsPerYear: Number(f.paymentsPerYear) || 12,
      cashPrice: Number(f.cashPrice) || 0,
      downPayment: Number(f.downPayment) || 0,
      financedPrincipal: principal,
      installmentAmount: installment,
      vatPerPeriod: Number(f.vatPerPeriod) || 0,
      liabilityAccountId: f.liabilityAccountId,
      deferredInterestAccountId: isHP && f.deferredInterestAccountId ? f.deferredInterestAccountId : null,
      inputVatUndueAccountId: isHP && f.inputVatUndueAccountId ? f.inputVatUndueAccountId : null,
      interestExpenseAccountId: f.interestExpenseAccountId,
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

  const inputCls =
    'w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400'
  const numCls = inputCls + ' text-right font-mono'

  return (
    <Overlay onClose={onClose} title={editingId !== null ? 'แก้ไขสัญญา' : 'สร้างสัญญา'}>
      <form onSubmit={handleSubmit} className="px-6 py-4">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ประเภท</label>
            <select value={f.contractType} onChange={(e) => set({ contractType: Number(e.target.value) })} className={inputCls}>
              {CONTRACT_TYPE_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">เลขที่สัญญา *</label>
            <input value={f.contractNo} onChange={(e) => set({ contractNo: e.target.value })} className={inputCls} placeholder="เช่น E057-2025" />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ทรัพย์สิน/รายการ *</label>
            <input value={f.assetName} onChange={(e) => set({ assetName: e.target.value })} className={inputCls} placeholder="เช่น SOLAR ROOFTOP" />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">รหัสทรัพย์สิน</label>
            <input value={f.assetCode} onChange={(e) => set({ assetCode: e.target.value })} className={inputCls} />
          </div>
          <div className="sm:col-span-2">
            <label className="mb-1 block text-xs font-medium text-gray-600">ผู้ให้เช่า/เจ้าหนี้</label>
            <input value={f.lessor} onChange={(e) => set({ lessor: e.target.value })} className={inputCls} />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">วันที่สัญญา</label>
            <input type="date" value={f.contractDate} onChange={(e) => set({ contractDate: e.target.value })} className={inputCls} />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">วันครบกำหนดงวดแรก</label>
            <input type="date" value={f.firstInstallmentDate} onChange={(e) => set({ firstInstallmentDate: e.target.value })} className={inputCls} />
          </div>
        </div>

        {/* เงื่อนไขการเงิน */}
        <p className="mb-2 mt-4 text-sm font-semibold text-slate-700">เงื่อนไขการเงิน</p>
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ราคาเงินสด</label>
            <input type="number" step="0.01" value={f.cashPrice}
              onChange={(e) => {
                const cash = e.target.value
                const fin = (Number(cash) || 0) - (Number(f.downPayment) || 0)
                set({ cashPrice: cash, financedPrincipal: fin > 0 ? String(Math.round(fin * 100) / 100) : f.financedPrincipal })
              }}
              className={numCls} />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">เงินดาวน์</label>
            <input type="number" step="0.01" value={f.downPayment}
              onChange={(e) => {
                const down = e.target.value
                const fin = (Number(f.cashPrice) || 0) - (Number(down) || 0)
                set({ downPayment: down, financedPrincipal: fin > 0 ? String(Math.round(fin * 100) / 100) : f.financedPrincipal })
              }}
              className={numCls} />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">เงินต้นจัดไฟแนนซ์ *</label>
            <input type="number" step="0.01" value={f.financedPrincipal} onChange={(e) => set({ financedPrincipal: e.target.value })} className={numCls} />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ค่างวด (ไม่รวม VAT) *</label>
            <input type="number" step="0.01" value={f.installmentAmount} onChange={(e) => set({ installmentAmount: e.target.value })} className={numCls} />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ภาษีซื้อ/งวด</label>
            <input type="number" step="0.01" value={f.vatPerPeriod} onChange={(e) => set({ vatPerPeriod: e.target.value })} className={numCls} disabled={!isHP} />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">จำนวนงวด *</label>
            <input type="number" value={f.numberOfPeriods} onChange={(e) => set({ numberOfPeriods: e.target.value })} className={numCls} />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">งวด/ปี</label>
            <input type="number" value={f.paymentsPerYear} onChange={(e) => set({ paymentsPerYear: e.target.value })} className={numCls} />
          </div>
        </div>

        {/* live preview */}
        {periods > 0 && installment > 0 && principal > 0 && (
          <div className={`mt-3 rounded px-3 py-2 text-xs ${interestValid ? 'bg-slate-50 text-slate-600' : 'bg-amber-50 text-amber-700'}`}>
            ยอดผ่อนรวม (ไม่รวม VAT) <b>{fmt(totalPayable)}</b> = เงินต้น {fmt(principal)} + ดอกเบี้ยรวม <b>{fmt(totalInterest)}</b>
            {f.vatPerPeriod && Number(f.vatPerPeriod) > 0 && <> · VAT รวม {fmt(Number(f.vatPerPeriod) * periods)}</>}
            {!interestValid && <> — ดอกเบี้ยติดลบ (ค่างวด×งวด &lt; เงินต้น)</>}
          </div>
        )}

        {/* บัญชี GL */}
        <p className="mb-2 mt-4 text-sm font-semibold text-slate-700">บัญชี GL ที่ผูก (สำหรับ generate adjustment)</p>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <AccountField label="หนี้สินตามสัญญา (gross) *" value={f.liabilityAccountId} options={accountOptions} onChange={(v) => set({ liabilityAccountId: v })} />
          <AccountField label="ดอกเบี้ยจ่าย (P&L) *" value={f.interestExpenseAccountId} options={accountOptions} onChange={(v) => set({ interestExpenseAccountId: v })} />
          {isHP && (
            <>
              <AccountField label="ดอกเบี้ยเช่าซื้อรอตัดบัญชี" value={f.deferredInterestAccountId} options={accountOptions} onChange={(v) => set({ deferredInterestAccountId: v })} />
              <AccountField label="ภาษีซื้อยังไม่ถึงกำหนด" value={f.inputVatUndueAccountId} options={accountOptions} onChange={(v) => set({ inputVatUndueAccountId: v })} />
            </>
          )}
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
      <div className="my-8 w-full max-w-3xl rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <h2 className="text-lg font-bold text-slate-800">{title}</h2>
          <button type="button" onClick={onClose} className="text-2xl leading-none text-slate-400 hover:text-slate-600">×</button>
        </div>
        {children}
      </div>
    </div>
  )
}
