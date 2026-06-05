import { useMemo, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import SearchableSelect from '../../../shared/components/ui/SearchableSelect'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useAccountList } from '../../trial-balance/hooks/useTrialBalance'
import { useAssetAccountMappings, useUpsertAssetAccountMappings } from '../hooks/useFixedAssets'
import type { AssetAccountMappingInput } from '../types/fixedAsset.types'

interface Props {
  companyId: number
  onClose: () => void
}

interface Row {
  categoryCode: string
  description: string
  assetAccountId: number
  accumDepreciationAccountId: number
  depreciationExpenseAccountId: number
  assetCount: number
}

export default function AccountMappingModal({ companyId, onClose }: Props) {
  const { data: mappings, isLoading } = useAssetAccountMappings(companyId)
  const { data: accounts } = useAccountList(companyId)
  const upsert = useUpsertAssetAccountMappings(companyId)

  const [rows, setRows] = useState<Row[] | null>(null)
  const [error, setError] = useState('')
  const [saved, setSaved] = useState(false)

  if (rows === null && mappings) {
    setRows(
      mappings.map((m) => ({
        categoryCode: m.categoryCode,
        description: m.description ?? '',
        assetAccountId: m.assetAccountId ?? 0,
        accumDepreciationAccountId: m.accumDepreciationAccountId ?? 0,
        depreciationExpenseAccountId: m.depreciationExpenseAccountId ?? 0,
        assetCount: m.assetCount,
      })),
    )
  }

  const accountOptions = useMemo(
    () =>
      (accounts ?? [])
        .filter((a) => a.isPostable)
        .map((a) => ({ value: a.id, label: `${a.accountCode} — ${a.accountName}`, searchText: `${a.accountCode} ${a.accountName}` })),
    [accounts],
  )

  function patch(idx: number, p: Partial<Row>) {
    setRows((prev) => prev!.map((r, i) => (i === idx ? { ...r, ...p } : r)))
    setSaved(false)
  }

  async function handleSave() {
    if (!rows) return
    setError('')
    const payload: AssetAccountMappingInput[] = rows.map((r) => ({
      categoryCode: r.categoryCode,
      description: r.description.trim() || null,
      assetAccountId: r.assetAccountId || null,
      accumDepreciationAccountId: r.accumDepreciationAccountId || null,
      depreciationExpenseAccountId: r.depreciationExpenseAccountId || null,
    }))
    try {
      await upsert.mutateAsync(payload)
      setSaved(true)
    } catch (err) {
      const msg = (err as { response?: { data?: { detail?: string; title?: string } } })?.response?.data
      setError(msg?.detail ?? msg?.title ?? 'บันทึกไม่สำเร็จ')
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm">
      <div className="my-8 w-full max-w-5xl rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <div>
            <h2 className="text-lg font-bold text-slate-800">แมพหมวดสินทรัพย์ → บัญชี GL</h2>
            <p className="text-xs text-gray-500">หมวดจาก Express (ACCCOD) → บัญชีค่าเสื่อมสะสม/ค่าเสื่อมราคา/สินทรัพย์ · บันทึกแล้วระบบเติมให้สินทรัพย์ที่ยังว่าง</p>
          </div>
          <button type="button" onClick={onClose} className="text-2xl leading-none text-slate-400 hover:text-slate-600">×</button>
        </div>

        <div className="px-6 py-4">
          {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}
          {rows && rows.length === 0 && (
            <StateMessage centered>ยังไม่มีหมวด — นำเข้าจาก Express ก่อน หรือยังไม่มีสินทรัพย์ที่มีหมวด</StateMessage>
          )}

          {rows && rows.length > 0 && (
            <div className="overflow-x-auto rounded border border-gray-200">
              <table className="w-full text-xs">
                <thead className="bg-slate-50 text-gray-600">
                  <tr>
                    <th className="px-3 py-2 text-left font-medium w-28">หมวด</th>
                    <th className="px-3 py-2 text-left font-medium">คำอธิบาย</th>
                    <th className="px-3 py-2 text-left font-medium">ค่าเสื่อมสะสม (contra) *</th>
                    <th className="px-3 py-2 text-left font-medium">ค่าเสื่อมราคา (P&L) *</th>
                    <th className="px-3 py-2 text-left font-medium">สินทรัพย์ (ราคาทุน)</th>
                  </tr>
                </thead>
                <tbody>
                  {rows.map((r, i) => (
                    <tr key={r.categoryCode} className="border-t border-gray-100 align-top">
                      <td className="px-3 py-2">
                        <span className="font-mono font-semibold text-slate-700">{r.categoryCode}</span>
                        <span className="ml-1 block text-[10px] text-gray-400">{r.assetCount} สินทรัพย์</span>
                      </td>
                      <td className="px-3 py-2">
                        <input value={r.description} onChange={(e) => patch(i, { description: e.target.value })}
                          className="w-full rounded border border-gray-300 px-2 py-1.5 text-xs" placeholder="เช่น ยานพาหนะ" />
                      </td>
                      <td className="px-3 py-2 min-w-[200px]">
                        <SearchableSelect value={r.accumDepreciationAccountId} options={accountOptions}
                          onChange={(v) => patch(i, { accumDepreciationAccountId: Number(v) })} placeholder="เลือกบัญชี" />
                      </td>
                      <td className="px-3 py-2 min-w-[200px]">
                        <SearchableSelect value={r.depreciationExpenseAccountId} options={accountOptions}
                          onChange={(v) => patch(i, { depreciationExpenseAccountId: Number(v) })} placeholder="เลือกบัญชี" />
                      </td>
                      <td className="px-3 py-2 min-w-[200px]">
                        <SearchableSelect value={r.assetAccountId} options={accountOptions}
                          onChange={(v) => patch(i, { assetAccountId: Number(v) })} placeholder="(ไม่บังคับ)" />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {error && <p className="mt-3 rounded bg-red-50 px-3 py-2 text-sm text-red-600">{error}</p>}
          {saved && <p className="mt-3 rounded bg-green-50 px-3 py-2 text-sm text-green-700">บันทึกแล้ว — เติมบัญชีให้สินทรัพย์ที่ยังว่างเรียบร้อย</p>}

          <div className="mt-5 flex justify-end gap-2">
            <Button type="button" variant="secondary" onClick={onClose}>ปิด</Button>
            <Button type="button" onClick={handleSave} disabled={!rows || rows.length === 0 || upsert.isPending}>
              {upsert.isPending ? 'กำลังบันทึก...' : 'บันทึกการแมพ'}
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
