import { useMemo, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import SearchableSelect from '../../../shared/components/ui/SearchableSelect'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useAccountList } from '../../trial-balance/hooks/useTrialBalance'
import { useFixedAssetWorkpaper, useGenerateDisposalAdjustment } from '../hooks/useFixedAssets'
import { STATUS_LABEL } from '../types/fixedAsset.types'
import type { FixedAssetWorkpaperRow } from '../types/fixedAsset.types'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  companyId: number
  fiscalYear: number
  onClose: () => void
}

export default function DisposalAdjustmentModal({ companyId, fiscalYear, onClose }: Props) {
  const { data } = useFixedAssetWorkpaper(companyId, fiscalYear)
  const { data: accounts } = useAccountList(companyId)
  const generate = useGenerateDisposalAdjustment(companyId)

  const [selected, setSelected] = useState<Set<number>>(new Set())
  const [gainAcc, setGainAcc] = useState(0)
  const [lossAcc, setLossAcc] = useState(0)
  const [proceedsAcc, setProceedsAcc] = useState(0)
  const [result, setResult] = useState<string | null>(null)
  const [error, setError] = useState('')

  // เฉพาะที่จำหน่าย/ขายในปีงบนี้
  const disposed = useMemo(
    () => (data?.rows ?? []).filter((r) => r.disposal && r.disposal.disposalDate.slice(0, 4) === String(fiscalYear)),
    [data, fiscalYear],
  )

  const accountOptions = useMemo(
    () =>
      (accounts ?? [])
        .filter((a) => a.isPostable)
        .map((a) => ({ value: a.id, label: `${a.accountCode} — ${a.accountName}`, searchText: `${a.accountCode} ${a.accountName}` })),
    [accounts],
  )

  const hasProceeds = disposed.some((r) => selected.has(r.assetId) && (r.disposal?.proceeds ?? 0) > 0)

  function toggle(id: number) {
    setSelected((p) => {
      const n = new Set(p)
      if (n.has(id)) n.delete(id); else n.add(id)
      return n
    })
    setResult(null)
  }

  async function handleGenerate() {
    setError(''); setResult(null)
    if (!gainAcc || !lossAcc) return setError('ต้องเลือกบัญชีกำไรและขาดทุนจากการจำหน่าย')
    if (hasProceeds && !proceedsAcc) return setError('มีรายการที่มีราคาขาย — ต้องเลือกบัญชีเงินรับ/เงินสด')
    try {
      const adj = await generate.mutateAsync({
        fiscalYear, assetIds: [...selected],
        gainAccountId: gainAcc, lossAccountId: lossAcc,
        proceedsAccountId: proceedsAcc || null,
      })
      setResult(`สร้างรายการ ${adj.documentNo} แล้ว (ยอดรวม ${fmt(adj.totalDebit)}) — ดูที่หน้า "กระดาษทำการปิดงบ"`)
      setSelected(new Set())
    } catch (err) {
      const msg = (err as { response?: { data?: { detail?: string; title?: string } } })?.response?.data
      setError(msg?.detail ?? msg?.title ?? 'สร้างรายการไม่สำเร็จ')
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm">
      <div className="my-8 w-full max-w-4xl rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <div>
            <h2 className="text-lg font-bold text-slate-800">สร้างรายการตัดจำหน่าย/ขายสินทรัพย์</h2>
            <p className="text-xs text-gray-500">
              ตัดสินทรัพย์ออก: Dr ค่าเสื่อมสะสม + Dr เงินรับ(ราคาขาย) + Dr ขาดทุน / Cr ราคาทุน + Cr กำไร · เฉพาะที่จำหน่ายในปี {fiscalYear}
            </p>
          </div>
          <button type="button" onClick={onClose} className="text-2xl leading-none text-slate-400 hover:text-slate-600">×</button>
        </div>

        <div className="px-6 py-4">
          {disposed.length === 0 ? (
            <StateMessage centered>{`ไม่มีสินทรัพย์ที่จำหน่าย/ขายในปี ${fiscalYear}`}</StateMessage>
          ) : (
            <>
              <div className="overflow-x-auto rounded border border-gray-200">
                <table className="w-full text-xs">
                  <thead className="bg-slate-50 text-gray-600">
                    <tr>
                      <th className="px-3 py-2 text-left font-medium">สินทรัพย์</th>
                      <th className="px-3 py-2 text-left font-medium">สถานะ</th>
                      <th className="px-3 py-2 text-left font-medium">วันจำหน่าย</th>
                      <th className="px-3 py-2 text-right font-medium">ราคาทุน</th>
                      <th className="px-3 py-2 text-right font-medium">มูลค่าสุทธิ</th>
                      <th className="px-3 py-2 text-right font-medium">ราคาขาย</th>
                      <th className="px-3 py-2 text-right font-medium">กำไร/ขาดทุน</th>
                    </tr>
                  </thead>
                  <tbody>
                    {disposed.map((r: FixedAssetWorkpaperRow) => {
                      const d = r.disposal!
                      return (
                        <tr key={r.assetId} className="border-t border-gray-100">
                          <td className="px-3 py-1.5">
                            <label className="flex items-center gap-2">
                              <input type="checkbox" checked={selected.has(r.assetId)} onChange={() => toggle(r.assetId)} className="rounded" />
                              <span><span className="font-mono text-gray-500">{r.assetCode}</span> {r.assetName}</span>
                            </label>
                          </td>
                          <td className="px-3 py-1.5 text-gray-600">{STATUS_LABEL[r.status]}</td>
                          <td className="px-3 py-1.5 text-gray-600">{d.disposalDate.slice(0, 10)}</td>
                          <td className="px-3 py-1.5 text-right font-mono">{fmt(r.cost)}</td>
                          <td className="px-3 py-1.5 text-right font-mono">{fmt(d.netBookValueAtDisposal)}</td>
                          <td className="px-3 py-1.5 text-right font-mono">{fmt(d.proceeds)}</td>
                          <td className={`px-3 py-1.5 text-right font-mono ${d.gainLoss >= 0 ? 'text-green-600' : 'text-red-600'}`}>{fmt(d.gainLoss)}</td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>

              <div className="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-3">
                <Field label="บัญชีกำไรจากการจำหน่าย *" value={gainAcc} options={accountOptions} onChange={setGainAcc} />
                <Field label="บัญชีขาดทุนจากการจำหน่าย *" value={lossAcc} options={accountOptions} onChange={setLossAcc} />
                <Field label={`บัญชีเงินรับ/เงินสด ${hasProceeds ? '*' : ''}`} value={proceedsAcc} options={accountOptions} onChange={setProceedsAcc} />
              </div>

              {error && <p className="mt-3 rounded bg-red-50 px-3 py-2 text-sm text-red-600">{error}</p>}
              {result && <p className="mt-3 rounded bg-green-50 px-3 py-2 text-sm text-green-700">{result}</p>}

              <div className="mt-5 flex justify-end gap-2">
                <Button type="button" variant="secondary" onClick={onClose}>ปิด</Button>
                <Button type="button" onClick={handleGenerate} disabled={selected.size === 0 || generate.isPending}>
                  {generate.isPending ? 'กำลังสร้าง...' : `สร้างรายการตัดจำหน่าย (${selected.size})`}
                </Button>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  )
}

function Field({
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
