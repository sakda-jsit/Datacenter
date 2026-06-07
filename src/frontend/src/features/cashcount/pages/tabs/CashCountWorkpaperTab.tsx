import { useMemo, useState } from 'react'
import Button from '../../../../shared/components/ui/Button'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import SearchableSelect from '../../../../shared/components/ui/SearchableSelect'
import ExportMenu from '../../../../shared/components/ui/ExportMenu'
import type { ExportSection } from '../../../../shared/utils/exportTable'
import { useAccountList } from '../../../trial-balance/hooks/useTrialBalance'
import { useCashCountWorkpaper, useGenerateCashCountAdjustment } from '../../hooks/useCashCount'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  companyId: number
  fiscalYear: number
}

export default function CashCountWorkpaperTab({ companyId, fiscalYear }: Props) {
  const { data, isLoading, isError } = useCashCountWorkpaper(companyId, fiscalYear)
  const { data: accounts } = useAccountList(companyId)
  const generate = useGenerateCashCountAdjustment(companyId)
  const [selected, setSelected] = useState<Set<number>>(new Set())
  const [counterpartId, setCounterpartId] = useState(0)
  const [result, setResult] = useState<string | null>(null)
  const [error, setError] = useState('')

  const accountOptions = useMemo(
    () => (accounts ?? [])
      .filter((a) => a.isPostable)
      .map((a) => ({ value: a.id, label: `${a.accountCode} — ${a.accountName}`, searchText: `${a.accountCode} ${a.accountName}` })),
    [accounts],
  )

  if (!companyId) return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>

  function toggle(id: number) {
    setSelected((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id); else next.add(id)
      return next
    })
  }
  function toggleAll() {
    if (!data) return
    setSelected((prev) => (prev.size === data.rows.length ? new Set() : new Set(data.rows.map((r) => r.id))))
  }

  async function handleGenerate() {
    setError(''); setResult(null)
    if (!counterpartId) { setError('เลือกบัญชีเงินสดขาด/เกิน (คู่บัญชี) ก่อน'); return }
    try {
      const adj = await generate.mutateAsync({ fiscalYear, cashCountIds: [...selected], counterpartAccountId: counterpartId })
      setResult(`สร้างรายการปรับปรุง ${adj.documentNo} แล้ว (รวม ${fmt(adj.totalDebit)}) — ดูที่หน้า "กระดาษทำการปิดงบ"`)
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
        <Card><StateMessage centered>ยังไม่มีใบตรวจนับปีนี้ — เพิ่มที่แท็บ "ใบตรวจนับ"</StateMessage></Card>
      )}

      {data && data.rows.length > 0 && (
        <>
          <Card className="mb-4 overflow-x-auto">
            <div className="flex items-start justify-between border-b px-4 py-3">
              <div>
                <p className="text-sm font-semibold text-slate-800">กระดาษทำการตรวจนับเงินสด ปี {fiscalYear}</p>
                <p className="text-xs text-gray-500">{data.clientName}</p>
              </div>
              <ExportMenu
                meta={{ title: `กระดาษทำการตรวจนับเงินสด ปี ${fiscalYear}`, subtitle: data.clientName, fileName: `cashcount-workpaper-${data.clientCode}-${fiscalYear}` }}
                getSections={(): ExportSection[] => [
                  {
                    name: 'ใบตรวจนับ',
                    columns: [
                      { key: 'countDate', header: 'วันที่นับ', value: (r) => r.countDate.slice(0, 10) },
                      { key: 'reference', header: 'จุดเก็บ/อ้างอิง' },
                      { key: 'cashAccountCode', header: 'บัญชีเงินสด' },
                      { key: 'countedTotal', header: 'นับได้รวม', align: 'right' },
                    ],
                    rows: data.rows,
                  },
                  {
                    name: 'เทียบ GL',
                    columns: [
                      { key: 'accountCode', header: 'บัญชี' },
                      { key: 'accountName', header: 'ชื่อบัญชี' },
                      { key: 'countedTotal', header: 'นับจริง', align: 'right' },
                      { key: 'glClosing', header: 'ตาม GL', align: 'right' },
                      { key: 'diff', header: 'ขาด/เกิน', align: 'right' },
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
                    วันที่นับ
                  </th>
                  <th className="px-3 py-2 text-left font-medium">จุดเก็บ/อ้างอิง</th>
                  <th className="px-3 py-2 text-left font-medium">บัญชีเงินสด</th>
                  <th className="px-3 py-2 text-right font-medium">นับได้รวม</th>
                </tr>
              </thead>
              <tbody>
                {data.rows.map((r) => (
                  <tr key={r.id} className="border-t border-gray-100 hover:bg-slate-50">
                    <td className="px-3 py-1.5">
                      <label className="flex items-center gap-2">
                        <input type="checkbox" checked={selected.has(r.id)} onChange={() => toggle(r.id)} className="rounded" />
                        <span>{r.countDate.slice(0, 10)}</span>
                      </label>
                    </td>
                    <td className="px-3 py-1.5 text-gray-700">{r.reference}</td>
                    <td className="px-3 py-1.5 font-mono text-gray-500">{r.cashAccountCode}</td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(r.countedTotal)}</td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
                  <td className="px-3 py-2 text-right" colSpan={3}>รวมนับได้</td>
                  <td className="px-3 py-2 text-right font-mono">{fmt(data.totalCounted)}</td>
                </tr>
              </tfoot>
            </table>
          </Card>

          {/* เทียบ GL */}
          <Card className="mb-4 overflow-x-auto">
            <div className="border-b px-4 py-3">
              <p className="text-sm font-semibold text-slate-800">เทียบยอดนับจริงกับ GL (บัญชีเงินสด, สะสมถึงสิ้นปี {fiscalYear})</p>
              <p className="text-xs text-gray-500">{data.hasDifference ? 'มีผลต่าง (เงินสดขาด/เกิน) — สร้างรายการปรับปรุงด้านล่าง' : 'ไม่มีผลต่าง (นับจริงตรงกับ GL)'}</p>
            </div>
            <table className="w-full text-xs">
              <thead className="bg-slate-50 text-gray-600">
                <tr>
                  <th className="px-3 py-2 text-left font-medium">บัญชี</th>
                  <th className="px-3 py-2 text-right font-medium">นับจริง</th>
                  <th className="px-3 py-2 text-right font-medium">ตาม GL</th>
                  <th className="px-3 py-2 text-right font-medium">ขาด(−)/เกิน(+)</th>
                </tr>
              </thead>
              <tbody>
                {data.glComparison.map((g) => {
                  const diff = Math.round(g.diff * 100) / 100
                  return (
                    <tr key={g.accountId} className="border-t border-gray-100">
                      <td className="px-3 py-1.5"><span className="font-mono text-gray-500">{g.accountCode}</span> {g.accountName}</td>
                      <td className="px-3 py-1.5 text-right font-mono">{fmt(g.countedTotal)}</td>
                      <td className="px-3 py-1.5 text-right font-mono">{fmt(g.glClosing)}</td>
                      <td className={`px-3 py-1.5 text-right font-mono ${diff === 0 ? 'text-green-600' : 'text-amber-600'}`}>{fmt(diff)}</td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </Card>

          {/* generate adjustment */}
          <Card className="px-4 py-4">
            <p className="text-sm font-semibold text-slate-800">สร้างรายการปรับปรุงเงินสดขาด/เกิน</p>
            <p className="mb-3 text-xs text-gray-500">
              เลือกใบตรวจนับด้านบน → ระบบปรับยอดบัญชีเงินสดให้เท่ายอดนับจริง ลงคู่กับบัญชีเงินสดขาด/เกิน ลงวันที่ 31 ธ.ค. {fiscalYear}
            </p>
            <div className="mb-3 max-w-md">
              <label className="mb-1 block text-xs font-medium text-gray-600">บัญชีเงินสดขาด/เกิน (คู่บัญชี) *</label>
              <SearchableSelect value={counterpartId} options={accountOptions} onChange={(v) => setCounterpartId(Number(v))} placeholder="เลือกบัญชีเงินสดขาด/เกิน" />
            </div>
            <div className="flex items-center gap-3">
              <Button type="button" onClick={handleGenerate} disabled={selected.size === 0 || generate.isPending}>
                {generate.isPending ? 'กำลังสร้าง...' : `สร้างรายการปรับปรุง (${selected.size} ใบ)`}
              </Button>
              {selected.size === 0 && <span className="text-xs text-gray-400">เลือกอย่างน้อย 1 ใบ</span>}
            </div>
            {result && <p className="mt-3 rounded bg-green-50 px-3 py-2 text-sm text-green-700">{result}</p>}
            {error && <p className="mt-3 rounded bg-red-50 px-3 py-2 text-sm text-red-600">{error}</p>}
          </Card>
        </>
      )}
    </div>
  )
}
