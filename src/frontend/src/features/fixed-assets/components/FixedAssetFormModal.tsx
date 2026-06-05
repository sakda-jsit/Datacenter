import { useMemo, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import SearchableSelect from '../../../shared/components/ui/SearchableSelect'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useAccountList } from '../../trial-balance/hooks/useTrialBalance'
import { useAssetTypes, useCreateFixedAsset, useFixedAsset, useUpdateFixedAsset } from '../hooks/useFixedAssets'
import { AssetStatus, STATUS_OPTIONS } from '../types/fixedAsset.types'
import type { FixedAssetInput } from '../types/fixedAsset.types'

interface Props {
  companyId: number
  fiscalYear: number
  editingId: number | null
  onClose: () => void
}

interface FormState {
  assetCode: string
  assetName: string
  assetTypeId: number
  acquireDate: string
  cost: string
  salvageValue: string
  bookRatePct: string
  taxRatePct: string
  accumulatedBroughtForward: string
  broughtForwardYear: number
  assetGroupCode: string
  categoryCode: string
  status: number
  disposalDate: string
  disposalProceeds: string
  disposalNote: string
  assetAccountId: number
  accumDepreciationAccountId: number
  depreciationExpenseAccountId: number
  notes: string
  isActive: boolean
}

function todayIso() {
  return new Date().toISOString().slice(0, 10)
}

function blank(): FormState {
  return {
    assetCode: '', assetName: '', assetTypeId: 0,
    acquireDate: todayIso(), cost: '', salvageValue: '0',
    bookRatePct: '', taxRatePct: '',
    accumulatedBroughtForward: '0', broughtForwardYear: 0, assetGroupCode: '', categoryCode: '',
    status: AssetStatus.Active,
    disposalDate: '', disposalProceeds: '', disposalNote: '',
    assetAccountId: 0, accumDepreciationAccountId: 0, depreciationExpenseAccountId: 0,
    notes: '', isActive: true,
  }
}

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

export default function FixedAssetFormModal({ companyId, fiscalYear, editingId, onClose }: Props) {
  const { data: accounts } = useAccountList(companyId)
  const { data: assetTypes } = useAssetTypes()
  const create = useCreateFixedAsset(companyId)
  const update = useUpdateFixedAsset(companyId)
  const { data: detail, isLoading: loadingDetail } = useFixedAsset(
    editingId ?? 0, companyId, fiscalYear, editingId !== null,
  )

  const [form, setForm] = useState<FormState | null>(null)
  const [error, setError] = useState('')

  const initialized = form !== null
  if (!initialized) {
    if (editingId === null) {
      setForm(blank())
    } else if (detail) {
      const a = detail.asset
      setForm({
        assetCode: a.assetCode, assetName: a.assetName, assetTypeId: a.assetTypeId ?? 0,
        acquireDate: a.acquireDate.slice(0, 10), cost: String(a.cost), salvageValue: String(a.salvageValue),
        bookRatePct: String(a.bookRatePct), taxRatePct: String(a.taxRatePct),
        accumulatedBroughtForward: String(a.accumulatedBroughtForward), broughtForwardYear: a.broughtForwardYear,
        assetGroupCode: a.assetGroupCode ?? '', categoryCode: a.categoryCode ?? '',
        status: a.status,
        disposalDate: a.disposalDate?.slice(0, 10) ?? '', disposalProceeds: a.disposalProceeds != null ? String(a.disposalProceeds) : '',
        disposalNote: a.disposalNote ?? '',
        assetAccountId: a.assetAccountId ?? 0, accumDepreciationAccountId: a.accumDepreciationAccountId,
        depreciationExpenseAccountId: a.depreciationExpenseAccountId,
        notes: a.notes ?? '', isActive: a.isActive,
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
      <Overlay onClose={onClose} title="แก้ไขสินทรัพย์">
        <StateMessage centered>กำลังโหลด...</StateMessage>
      </Overlay>
    )
  }
  if (!form) return null

  const f = form
  const set = (patch: Partial<FormState>) => setForm((p) => ({ ...(p as FormState), ...patch }))

  function pickType(typeId: number) {
    const t = assetTypes?.find((x) => x.id === typeId)
    if (t) set({ assetTypeId: typeId, bookRatePct: String(t.defaultBookRatePct), taxRatePct: String(t.defaultTaxRatePct) })
    else set({ assetTypeId: typeId })
  }

  const isDisposed = f.status !== AssetStatus.Active
  const isSold = f.status === AssetStatus.Sold
  const cost = Number(f.cost) || 0
  const proceeds = Number(f.disposalProceeds) || 0
  const saving = create.isPending || update.isPending

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')

    if (!f.assetCode.trim()) return setError('ต้องระบุรหัสสินทรัพย์')
    if (!f.assetName.trim()) return setError('ต้องระบุชื่อสินทรัพย์')
    if (cost <= 0) return setError('ราคาทุนต้องมากกว่า 0')
    if ((Number(f.salvageValue) || 0) > cost) return setError('มูลค่าซากต้องไม่เกินราคาทุน')
    if (!f.accumDepreciationAccountId) return setError('ต้องเลือกบัญชีค่าเสื่อมราคาสะสม')
    if (!f.depreciationExpenseAccountId) return setError('ต้องเลือกบัญชีค่าเสื่อมราคา')
    if (isDisposed && !f.disposalDate) return setError('รายการจำหน่าย/ขาย/ตัดจำหน่ายต้องระบุวันที่จำหน่าย')
    if (isSold && !f.disposalProceeds) return setError('การขายสินทรัพย์ต้องระบุราคาขาย')

    const data: FixedAssetInput = {
      assetCode: f.assetCode.trim(),
      assetName: f.assetName.trim(),
      assetTypeId: f.assetTypeId || null,
      acquireDate: f.acquireDate,
      cost,
      salvageValue: Number(f.salvageValue) || 0,
      bookRatePct: Number(f.bookRatePct) || 0,
      taxRatePct: Number(f.taxRatePct) || 0,
      accumulatedBroughtForward: Number(f.accumulatedBroughtForward) || 0,
      broughtForwardYear: f.broughtForwardYear || 0,
      assetGroupCode: f.assetGroupCode.trim() || null,
      categoryCode: f.categoryCode.trim() || null,
      status: f.status,
      disposalDate: isDisposed ? f.disposalDate : null,
      disposalProceeds: isDisposed && f.disposalProceeds !== '' ? proceeds : null,
      disposalNote: isDisposed ? f.disposalNote.trim() || null : null,
      assetAccountId: f.assetAccountId || null,
      accumDepreciationAccountId: f.accumDepreciationAccountId,
      depreciationExpenseAccountId: f.depreciationExpenseAccountId,
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
    <Overlay onClose={onClose} title={editingId !== null ? 'แก้ไขสินทรัพย์' : 'สร้างสินทรัพย์'}>
      <form onSubmit={handleSubmit} className="px-6 py-4">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">รหัสสินทรัพย์ *</label>
            <input value={f.assetCode} onChange={(e) => set({ assetCode: e.target.value })} className={inputCls} placeholder="เช่น FA-001" />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ชื่อสินทรัพย์ *</label>
            <input value={f.assetName} onChange={(e) => set({ assetName: e.target.value })} className={inputCls} />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ประเภทสินทรัพย์</label>
            <select value={f.assetTypeId} onChange={(e) => pickType(Number(e.target.value))} className={inputCls}>
              <option value={0}>— ไม่ระบุ —</option>
              {(assetTypes ?? []).map((t) => (
                <option key={t.id} value={t.id}>{t.name} ({t.defaultBookRatePct}% / {t.defaultTaxRatePct}%)</option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">วันที่ได้มา</label>
            <input type="date" value={f.acquireDate} onChange={(e) => set({ acquireDate: e.target.value })} className={inputCls} />
          </div>
        </div>

        <p className="mb-2 mt-4 text-sm font-semibold text-slate-700">ราคาทุนและอัตราค่าเสื่อม</p>
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ราคาทุน *</label>
            <input type="number" step="0.01" value={f.cost} onChange={(e) => set({ cost: e.target.value })} className={numCls} />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">มูลค่าซาก</label>
            <input type="number" step="0.01" value={f.salvageValue} onChange={(e) => set({ salvageValue: e.target.value })} className={numCls} />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">อัตราบัญชี (%/ปี)</label>
            <input type="number" step="0.01" value={f.bookRatePct} onChange={(e) => set({ bookRatePct: e.target.value })} className={numCls} />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">อัตราภาษี (%/ปี)</label>
            <input type="number" step="0.01" value={f.taxRatePct} onChange={(e) => set({ taxRatePct: e.target.value })} className={numCls} />
          </div>
        </div>
        <p className="mt-2 text-xs text-gray-400">
          ค่าเสื่อมเส้นตรง: ปีเต็ม = ราคาทุน × อัตรา; ปีแรก/ปีจำหน่าย prorate ตามวัน · อัตรา 0 = ไม่คิดค่าเสื่อม (เช่น ที่ดิน)
        </p>

        {/* ยอดยกมา (เติมอัตโนมัติเมื่อนำเข้าจาก Express; ป้อนเองได้) */}
        <div className="mt-3 grid grid-cols-2 gap-3 sm:grid-cols-3">
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ค่าเสื่อมสะสมยกมา</label>
            <input type="number" step="0.01" value={f.accumulatedBroughtForward}
              onChange={(e) => set({ accumulatedBroughtForward: e.target.value })} className={numCls} />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ปีของยอดยกมา</label>
            <input type="number" value={f.broughtForwardYear || ''}
              onChange={(e) => set({ broughtForwardYear: Number(e.target.value) || 0 })} className={numCls} placeholder="เช่น 2025" />
          </div>
          {f.categoryCode && (
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">หมวด (Express)</label>
              <input value={f.categoryCode} readOnly className={inputCls + ' bg-slate-50 text-gray-500'} />
            </div>
          )}
        </div>
        <p className="mt-1 text-xs text-gray-400">
          ยอดยกมา &gt; 0: engine เริ่มสะสมจากยอดนี้ ณ ต้นปีที่ระบุ (ใช้กับสินทรัพย์ที่นำเข้าจาก Express เพื่อให้ตรงเป๊ะ)
        </p>

        {/* บัญชี GL */}
        <p className="mb-2 mt-4 text-sm font-semibold text-slate-700">บัญชี GL ที่ผูก (สำหรับ generate adjustment)</p>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <AccountField label="ค่าเสื่อมราคาสะสม (contra) *" value={f.accumDepreciationAccountId} options={accountOptions} onChange={(v) => set({ accumDepreciationAccountId: v })} />
          <AccountField label="ค่าเสื่อมราคา (P&L) *" value={f.depreciationExpenseAccountId} options={accountOptions} onChange={(v) => set({ depreciationExpenseAccountId: v })} />
          <AccountField label="บัญชีสินทรัพย์ (ราคาทุน)" value={f.assetAccountId} options={accountOptions} onChange={(v) => set({ assetAccountId: v })} />
        </div>

        {/* สถานะ + การจำหน่าย */}
        <p className="mb-2 mt-4 text-sm font-semibold text-slate-700">สถานะ</p>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">สถานะ</label>
            <select value={f.status} onChange={(e) => set({ status: Number(e.target.value) })} className={inputCls}>
              {STATUS_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
            </select>
          </div>
          {isDisposed && (
            <>
              <div>
                <label className="mb-1 block text-xs font-medium text-gray-600">วันที่จำหน่าย *</label>
                <input type="date" value={f.disposalDate} onChange={(e) => set({ disposalDate: e.target.value })} className={inputCls} />
              </div>
              <div>
                <label className="mb-1 block text-xs font-medium text-gray-600">ราคาขาย {isSold && '*'}</label>
                <input type="number" step="0.01" value={f.disposalProceeds} onChange={(e) => set({ disposalProceeds: e.target.value })} className={numCls} disabled={!isSold && f.status !== AssetStatus.Disposed} />
              </div>
              <div className="sm:col-span-2">
                <label className="mb-1 block text-xs font-medium text-gray-600">หมายเหตุการจำหน่าย</label>
                <input value={f.disposalNote} onChange={(e) => set({ disposalNote: e.target.value })} className={inputCls} />
              </div>
            </>
          )}
        </div>

        {isDisposed && cost > 0 && (
          <div className="mt-3 rounded bg-slate-50 px-3 py-2 text-xs text-slate-600">
            ราคาขาย <b>{fmt(proceeds)}</b> · กำไร/ขาดทุนคำนวณอัตโนมัติ = ราคาขาย − มูลค่าสุทธิ ณ วันจำหน่าย
            (ดูตัวเลขจริงในตารางค่าเสื่อม หลังบันทึก)
          </div>
        )}

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
