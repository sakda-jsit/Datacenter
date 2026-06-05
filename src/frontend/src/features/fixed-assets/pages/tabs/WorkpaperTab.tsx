import { useState } from 'react'
import Button from '../../../../shared/components/ui/Button'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import { useFixedAssetWorkpaper, useGenerateDepreciationAdjustment } from '../../hooks/useFixedAssets'
import DisposalAdjustmentModal from '../../components/DisposalAdjustmentModal'
import ExportMenu from '../../../../shared/components/ui/ExportMenu'
import { DEP_SET_OPTIONS, DepSet, STATUS_LABEL } from '../../types/fixedAsset.types'
import type { ExportSection } from '../../../../shared/utils/exportTable'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

const ROLE_LABEL: Record<string, string> = {
  AccumDepreciation: 'ค่าเสื่อมราคาสะสม',
  DepreciationExpense: 'ค่าเสื่อมราคา (P&L)',
}

interface Props {
  companyId: number
  fiscalYear: number
}

export default function WorkpaperTab({ companyId, fiscalYear }: Props) {
  const { data, isLoading, isError } = useFixedAssetWorkpaper(companyId, fiscalYear)
  const generate = useGenerateDepreciationAdjustment(companyId)
  const [selected, setSelected] = useState<Set<number>>(new Set())
  const [set, setSet] = useState<number>(DepSet.Book)
  const [result, setResult] = useState<string | null>(null)
  const [error, setError] = useState('')
  const [disposalOpen, setDisposalOpen] = useState(false)

  const disposedCount = (data?.rows ?? []).filter(
    (r) => r.disposal && r.disposal.disposalDate.slice(0, 4) === String(fiscalYear),
  ).length

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
    if (!data) return
    setSelected((prev) =>
      prev.size === data.rows.length ? new Set() : new Set(data.rows.map((r) => r.assetId)),
    )
  }

  async function handleGenerate() {
    setError('')
    setResult(null)
    try {
      const adj = await generate.mutateAsync({ fiscalYear, assetIds: [...selected], set })
      setResult(`สร้างรายการปรับปรุง ${adj.documentNo} แล้ว (ค่าเสื่อมรวม ${fmt(adj.totalDebit)}) — ดูที่หน้า "กระดาษทำการปิดงบ"`)
      setSelected(new Set())
    } catch (err) {
      const msg = (err as { response?: { data?: { detail?: string; title?: string } } })?.response?.data
      setError(msg?.detail ?? msg?.title ?? 'สร้างรายการปรับปรุงไม่สำเร็จ')
    }
  }

  return (
    <div>
      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {data && data.rows.length === 0 && (
        <Card><StateMessage centered>{`ยังไม่มีสินทรัพย์ที่ได้มาก่อน/ในปี ${fiscalYear} — เพิ่มที่แท็บ "ทะเบียนสินทรัพย์"`}</StateMessage></Card>
      )}

      {data && data.rows.length > 0 && (
        <>
          {/* per-asset */}
          <Card className="mb-4 overflow-x-auto">
            <div className="flex items-start justify-between border-b px-4 py-3">
              <div>
                <p className="text-sm font-semibold text-slate-800">กระดาษทำการสินทรัพย์ถาวร ณ สิ้นปี {fiscalYear}</p>
                <p className="text-xs text-gray-500">{data.clientName} · ยอดที่แสดงเป็นชุดบัญชี</p>
              </div>
              <ExportMenu
                meta={{
                  title: `กระดาษทำการสินทรัพย์ถาวร ปี ${fiscalYear}`,
                  subtitle: data.clientName,
                  fileName: `fixed-asset-workpaper-${data.clientCode}-${fiscalYear}`,
                }}
                getSections={(): ExportSection[] => [
                  {
                    name: 'รายสินทรัพย์',
                    columns: [
                      { key: 'assetCode', header: 'รหัส' },
                      { key: 'assetName', header: 'สินทรัพย์' },
                      { key: 'cost', header: 'ราคาทุน', align: 'right' },
                      { key: 'accClose', header: 'ค่าเสื่อมสะสมสิ้นปี', align: 'right', value: (r) => r.book.closingAccumulated },
                      { key: 'chargeBook', header: 'ค่าเสื่อมปีนี้(บัญชี)', align: 'right', value: (r) => r.book.charge },
                      { key: 'chargeTax', header: 'ค่าเสื่อมปีนี้(ภาษี)', align: 'right', value: (r) => r.tax.charge },
                      { key: 'nbv', header: 'มูลค่าสุทธิ', align: 'right', value: (r) => r.book.netBookValue },
                      { key: 'gl', header: 'กำไร/ขาดทุนจำหน่าย', align: 'right', value: (r) => r.disposal?.gainLoss ?? '' },
                    ],
                    rows: data.rows,
                  },
                  {
                    name: 'สรุปตามประเภท',
                    columns: [
                      { key: 'assetTypeName', header: 'ประเภท' },
                      { key: 'count', header: 'จำนวน', align: 'right' },
                      { key: 'cost', header: 'ราคาทุน', align: 'right' },
                      { key: 'chargeInYear', header: 'ค่าเสื่อมปีนี้', align: 'right' },
                      { key: 'bookClosingAccumulated', header: 'ค่าเสื่อมสะสมสิ้นปี', align: 'right' },
                      { key: 'bookNetBookValue', header: 'มูลค่าสุทธิ', align: 'right' },
                    ],
                    rows: data.typeSummary,
                  },
                  {
                    name: 'เทียบ GL',
                    columns: [
                      { key: 'accountCode', header: 'บัญชี' },
                      { key: 'accountName', header: 'ชื่อบัญชี' },
                      { key: 'role', header: 'บทบาท' },
                      { key: 'scheduleAmount', header: 'ตาม schedule', align: 'right' },
                      { key: 'glAmount', header: 'ตาม GL', align: 'right' },
                      { key: 'diff', header: 'ผลต่าง', align: 'right' },
                    ],
                    rows: data.glComparison,
                  },
                ]}
              />
            </div>
            <table className="w-full text-xs">
              <thead className="bg-slate-50 text-gray-600">
                <tr>
                  <th className="px-3 py-2 text-left font-medium">
                    <input type="checkbox" checked={selected.size === data.rows.length} onChange={toggleAll} className="mr-2 rounded" />
                    สินทรัพย์
                  </th>
                  <th className="px-3 py-2 text-right font-medium">ราคาทุน</th>
                  <th className="px-3 py-2 text-right font-medium">ค่าเสื่อมสะสมสิ้นปี</th>
                  <th className="px-3 py-2 text-right font-medium">ค่าเสื่อมปีนี้ (บัญชี)</th>
                  <th className="px-3 py-2 text-right font-medium">ค่าเสื่อมปีนี้ (ภาษี)</th>
                  <th className="px-3 py-2 text-right font-medium">มูลค่าสุทธิ</th>
                  <th className="px-3 py-2 text-right font-medium">กำไร/ขาดทุนจำหน่าย</th>
                </tr>
              </thead>
              <tbody>
                {data.rows.map((r) => (
                  <tr key={r.assetId} className="border-t border-gray-100 hover:bg-slate-50">
                    <td className="px-3 py-1.5">
                      <label className="flex items-center gap-2">
                        <input type="checkbox" checked={selected.has(r.assetId)} onChange={() => toggle(r.assetId)} className="rounded" />
                        <span>
                          <span className="font-mono text-gray-500">{r.assetCode}</span> {r.assetName}
                          {r.status !== 0 && <span className="ml-1 text-[10px] text-amber-600">({STATUS_LABEL[r.status]})</span>}
                        </span>
                      </label>
                    </td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(r.cost)}</td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(r.book.closingAccumulated)}</td>
                    <td className="px-3 py-1.5 text-right font-mono text-sky-700">{fmt(r.book.charge)}</td>
                    <td className="px-3 py-1.5 text-right font-mono text-gray-500">{fmt(r.tax.charge)}</td>
                    <td className="px-3 py-1.5 text-right font-mono font-semibold">{fmt(r.book.netBookValue)}</td>
                    <td className={`px-3 py-1.5 text-right font-mono ${r.disposal ? (r.disposal.gainLoss >= 0 ? 'text-green-600' : 'text-red-600') : 'text-gray-300'}`}>
                      {r.disposal ? fmt(r.disposal.gainLoss) : '—'}
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
                  <td className="px-3 py-2 text-right">รวม</td>
                  <td className="px-3 py-2 text-right font-mono">{fmt(data.totalCost)}</td>
                  <td className="px-3 py-2 text-right font-mono">{fmt(data.totalBookClosingAccumulated)}</td>
                  <td className="px-3 py-2 text-right font-mono text-sky-700">{fmt(data.totalBookCharge)}</td>
                  <td className="px-3 py-2 text-right font-mono text-gray-500">{fmt(data.totalTaxCharge)}</td>
                  <td className="px-3 py-2 text-right font-mono">{fmt(data.totalBookNetBookValue)}</td>
                  <td className="px-3 py-2" />
                </tr>
              </tfoot>
            </table>
          </Card>

          {/* สรุปตามประเภท (NOTE2) */}
          <Card className="mb-4 overflow-x-auto">
            <div className="border-b px-4 py-3">
              <p className="text-sm font-semibold text-slate-800">สรุปตามประเภทสินทรัพย์ (เชื่อม NOTE2) — ชุดบัญชี</p>
            </div>
            <table className="w-full text-xs">
              <thead className="bg-slate-50 text-gray-600">
                <tr>
                  <th className="px-3 py-2 text-left font-medium">ประเภท</th>
                  <th className="px-3 py-2 text-right font-medium">จำนวน</th>
                  <th className="px-3 py-2 text-right font-medium">ราคาทุน</th>
                  <th className="px-3 py-2 text-right font-medium">ค่าเสื่อมปีนี้</th>
                  <th className="px-3 py-2 text-right font-medium">ค่าเสื่อมสะสมสิ้นปี</th>
                  <th className="px-3 py-2 text-right font-medium">มูลค่าสุทธิ</th>
                </tr>
              </thead>
              <tbody>
                {data.typeSummary.map((s) => (
                  <tr key={s.assetTypeName} className="border-t border-gray-100">
                    <td className="px-3 py-1.5 text-gray-700">{s.assetTypeName}</td>
                    <td className="px-3 py-1.5 text-right text-gray-500">{s.count}</td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(s.cost)}</td>
                    <td className="px-3 py-1.5 text-right font-mono text-sky-700">{fmt(s.chargeInYear)}</td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(s.bookClosingAccumulated)}</td>
                    <td className="px-3 py-1.5 text-right font-mono font-semibold">{fmt(s.bookNetBookValue)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Card>

          {/* เทียบ GL */}
          <Card className="mb-4 overflow-x-auto">
            <div className="border-b px-4 py-3">
              <p className="text-sm font-semibold text-slate-800">เทียบยอดกับ GL (ชุดบัญชี, ปี {fiscalYear})</p>
              <p className="text-xs text-gray-500">
                {data.hasDifference
                  ? 'มีผลต่าง — ค่าเสื่อมสะสมเทียบยอดสะสมถึงสิ้นปี / ค่าเสื่อมราคาเทียบ movement ในปี'
                  : 'ไม่มีผลต่าง (schedule ตรงกับ GL)'}
              </p>
            </div>
            {data.glComparison.length === 0 ? (
              <div className="px-4 py-3 text-xs text-gray-400">ไม่มีบัญชีที่ผูก</div>
            ) : (
              <table className="w-full text-xs">
                <thead className="bg-slate-50 text-gray-600">
                  <tr>
                    <th className="px-3 py-2 text-left font-medium">บัญชี</th>
                    <th className="px-3 py-2 text-left font-medium">บทบาท</th>
                    <th className="px-3 py-2 text-right font-medium">ตาม schedule</th>
                    <th className="px-3 py-2 text-right font-medium">ตาม GL</th>
                    <th className="px-3 py-2 text-right font-medium">ผลต่าง</th>
                  </tr>
                </thead>
                <tbody>
                  {data.glComparison.map((g) => {
                    const diff = Math.round(g.diff * 100) / 100
                    return (
                      <tr key={`${g.accountId}-${g.role}`} className="border-t border-gray-100">
                        <td className="px-3 py-1.5"><span className="font-mono text-gray-500">{g.accountCode}</span> {g.accountName}</td>
                        <td className="px-3 py-1.5 text-gray-600">{ROLE_LABEL[g.role] ?? g.role}</td>
                        <td className="px-3 py-1.5 text-right font-mono">{fmt(g.scheduleAmount)}</td>
                        <td className="px-3 py-1.5 text-right font-mono">{fmt(g.glAmount)}</td>
                        <td className={`px-3 py-1.5 text-right font-mono ${diff === 0 ? 'text-green-600' : 'text-amber-600'}`}>{fmt(diff)}</td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            )}
          </Card>

          {/* generate adjustment */}
          <Card className="px-4 py-4">
            <p className="text-sm font-semibold text-slate-800">สร้างรายการปรับปรุงค่าเสื่อมราคารับรู้ในปี</p>
            <p className="mb-3 text-xs text-gray-500">
              เลือกสินทรัพย์ด้านบน → ระบบสร้าง AdjustmentEntry: Dr ค่าเสื่อมราคา / Cr ค่าเสื่อมราคาสะสม ลงวันที่ 31 ธ.ค. {fiscalYear}
            </p>
            <div className="flex flex-wrap items-center gap-3">
              <div>
                <label className="mb-1 block text-xs font-medium text-gray-600">ชุดค่าเสื่อม</label>
                <select value={set} onChange={(e) => setSet(Number(e.target.value))} className="rounded border border-gray-300 px-3 py-2 text-sm">
                  {DEP_SET_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
                </select>
              </div>
              <Button type="button" onClick={handleGenerate} disabled={selected.size === 0 || generate.isPending} className="self-end">
                {generate.isPending ? 'กำลังสร้าง...' : `สร้างรายการปรับปรุง (${selected.size} รายการ)`}
              </Button>
              {selected.size === 0 && <span className="self-end pb-2 text-xs text-gray-400">เลือกสินทรัพย์อย่างน้อย 1 รายการ</span>}
            </div>
            {result && <p className="mt-3 rounded bg-green-50 px-3 py-2 text-sm text-green-700">{result}</p>}
            {error && <p className="mt-3 rounded bg-red-50 px-3 py-2 text-sm text-red-600">{error}</p>}
          </Card>

          {/* disposal adjustment */}
          <Card className="mt-4 flex flex-wrap items-center justify-between gap-3 px-4 py-4">
            <div>
              <p className="text-sm font-semibold text-slate-800">ตัดจำหน่าย/ขายสินทรัพย์</p>
              <p className="text-xs text-gray-500">
                {disposedCount > 0
                  ? `มี ${disposedCount} รายการจำหน่าย/ขายในปี ${fiscalYear} → สร้างรายการตัดออก (กำไร/ขาดทุนอัตโนมัติ)`
                  : `ไม่มีสินทรัพย์ที่จำหน่าย/ขายในปี ${fiscalYear}`}
              </p>
            </div>
            <Button type="button" variant="secondary" onClick={() => setDisposalOpen(true)} disabled={disposedCount === 0}>
              สร้างรายการตัดจำหน่าย/ขาย
            </Button>
          </Card>
        </>
      )}

      {disposalOpen && (
        <DisposalAdjustmentModal companyId={companyId} fiscalYear={fiscalYear} onClose={() => setDisposalOpen(false)} />
      )}
    </div>
  )
}
