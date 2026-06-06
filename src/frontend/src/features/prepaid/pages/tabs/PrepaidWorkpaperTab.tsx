import { useState } from 'react'
import Button from '../../../../shared/components/ui/Button'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import ExportMenu from '../../../../shared/components/ui/ExportMenu'
import type { ExportSection } from '../../../../shared/utils/exportTable'
import { useGeneratePrepaidAdjustment, usePrepaidWorkpaper } from '../../hooks/usePrepaid'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  companyId: number
  fiscalYear: number
}

export default function PrepaidWorkpaperTab({ companyId, fiscalYear }: Props) {
  const { data, isLoading, isError } = usePrepaidWorkpaper(companyId, fiscalYear)
  const generate = useGeneratePrepaidAdjustment(companyId)
  const [selected, setSelected] = useState<Set<number>>(new Set())
  const [result, setResult] = useState<string | null>(null)
  const [error, setError] = useState('')

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
    try {
      const adj = await generate.mutateAsync({ fiscalYear, prepaidIds: [...selected] })
      setResult(`สร้างรายการปรับปรุง ${adj.documentNo} แล้ว (ตัดจ่ายรวม ${fmt(adj.totalDebit)}) — ดูที่หน้า "กระดาษทำการปิดงบ"`)
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
        <Card><StateMessage centered>ยังไม่มีรายการ — เพิ่มที่แท็บ "รายการ"</StateMessage></Card>
      )}

      {data && data.rows.length > 0 && (
        <>
          <Card className="mb-4 overflow-x-auto">
            <div className="flex items-start justify-between border-b px-4 py-3">
              <div>
                <p className="text-sm font-semibold text-slate-800">กระดาษทำการ ณ สิ้นปี {fiscalYear}</p>
                <p className="text-xs text-gray-500">{data.clientName}</p>
              </div>
              <ExportMenu
                meta={{ title: `กระดาษทำการค่าใช้จ่ายจ่ายล่วงหน้า ปี ${fiscalYear}`, subtitle: data.clientName, fileName: `prepaid-workpaper-${data.clientCode}-${fiscalYear}` }}
                getSections={(): ExportSection[] => [
                  {
                    name: 'รายการ',
                    columns: [
                      { key: 'name', header: 'รายละเอียด' },
                      { key: 'reference', header: 'อ้างอิง' },
                      { key: 'totalAmount', header: 'มูลค่าตั้งต้น', align: 'right' },
                      { key: 'openingAmortized', header: 'ตัดจ่ายยกมา', align: 'right' },
                      { key: 'chargeInYear', header: 'ตัดจ่ายปีนี้', align: 'right' },
                      { key: 'closingAmortized', header: 'ตัดจ่ายสะสม', align: 'right' },
                      { key: 'remaining', header: 'คงเหลือ', align: 'right' },
                    ],
                    rows: data.rows,
                  },
                  {
                    name: 'เทียบ GL',
                    columns: [
                      { key: 'accountCode', header: 'บัญชี' },
                      { key: 'accountName', header: 'ชื่อบัญชี' },
                      { key: 'scheduleRemaining', header: 'คงเหลือ schedule', align: 'right' },
                      { key: 'glClosing', header: 'ตาม GL', align: 'right' },
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
                    รายการ
                  </th>
                  <th className="px-3 py-2 text-right font-medium">มูลค่าตั้งต้น</th>
                  <th className="px-3 py-2 text-right font-medium">ตัดจ่ายยกมา</th>
                  <th className="px-3 py-2 text-right font-medium">ตัดจ่ายปีนี้</th>
                  <th className="px-3 py-2 text-right font-medium">ตัดจ่ายสะสม</th>
                  <th className="px-3 py-2 text-right font-medium">คงเหลือ</th>
                </tr>
              </thead>
              <tbody>
                {data.rows.map((r) => (
                  <tr key={r.id} className="border-t border-gray-100 hover:bg-slate-50">
                    <td className="px-3 py-1.5">
                      <label className="flex items-center gap-2">
                        <input type="checkbox" checked={selected.has(r.id)} onChange={() => toggle(r.id)} className="rounded" />
                        <span>{r.name}{r.reference && <span className="ml-1 text-gray-400">({r.reference})</span>}</span>
                      </label>
                    </td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(r.totalAmount)}</td>
                    <td className="px-3 py-1.5 text-right font-mono text-gray-500">{fmt(r.openingAmortized)}</td>
                    <td className="px-3 py-1.5 text-right font-mono text-sky-700">{fmt(r.chargeInYear)}</td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(r.closingAmortized)}</td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(r.remaining)}</td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
                  <td className="px-3 py-2 text-right">รวม</td>
                  <td className="px-3 py-2 text-right font-mono">{fmt(data.totalAmount)}</td>
                  <td className="px-3 py-2" />
                  <td className="px-3 py-2 text-right font-mono text-sky-700">{fmt(data.totalChargeInYear)}</td>
                  <td className="px-3 py-2" />
                  <td className="px-3 py-2 text-right font-mono">{fmt(data.totalRemaining)}</td>
                </tr>
              </tfoot>
            </table>
          </Card>

          {/* เทียบ GL */}
          <Card className="mb-4 overflow-x-auto">
            <div className="border-b px-4 py-3">
              <p className="text-sm font-semibold text-slate-800">เทียบยอดคงเหลือกับ GL (บัญชีค่าใช้จ่ายจ่ายล่วงหน้า, สะสมถึงสิ้นปี {fiscalYear})</p>
              <p className="text-xs text-gray-500">{data.hasDifference ? 'มีผลต่าง — ตรวจสอบและปรับปรุงตามความเหมาะสม' : 'ไม่มีผลต่าง (schedule ตรงกับ GL)'}</p>
            </div>
            {data.glComparison.length === 0 ? (
              <div className="px-4 py-3 text-xs text-gray-400">ไม่มีบัญชีที่ผูก</div>
            ) : (
              <table className="w-full text-xs">
                <thead className="bg-slate-50 text-gray-600">
                  <tr>
                    <th className="px-3 py-2 text-left font-medium">บัญชี</th>
                    <th className="px-3 py-2 text-right font-medium">คงเหลือ schedule</th>
                    <th className="px-3 py-2 text-right font-medium">ตาม GL</th>
                    <th className="px-3 py-2 text-right font-medium">ผลต่าง</th>
                  </tr>
                </thead>
                <tbody>
                  {data.glComparison.map((g) => {
                    const diff = Math.round(g.diff * 100) / 100
                    return (
                      <tr key={g.accountId} className="border-t border-gray-100">
                        <td className="px-3 py-1.5"><span className="font-mono text-gray-500">{g.accountCode}</span> {g.accountName}</td>
                        <td className="px-3 py-1.5 text-right font-mono">{fmt(g.scheduleRemaining)}</td>
                        <td className="px-3 py-1.5 text-right font-mono">{fmt(g.glClosing)}</td>
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
            <p className="text-sm font-semibold text-slate-800">สร้างรายการปรับปรุงตัดจ่ายในปี</p>
            <p className="mb-3 text-xs text-gray-500">
              เลือกรายการด้านบน → ระบบสร้าง AdjustmentEntry (Dr ค่าใช้จ่าย / Cr ค่าใช้จ่ายจ่ายล่วงหน้า) ลงวันที่ 31 ธ.ค. {fiscalYear}
            </p>
            <div className="flex items-center gap-3">
              <Button type="button" onClick={handleGenerate} disabled={selected.size === 0 || generate.isPending}>
                {generate.isPending ? 'กำลังสร้าง...' : `สร้างรายการปรับปรุง (${selected.size} รายการ)`}
              </Button>
              {selected.size === 0 && <span className="text-xs text-gray-400">เลือกอย่างน้อย 1 รายการ</span>}
            </div>
            {result && <p className="mt-3 rounded bg-green-50 px-3 py-2 text-sm text-green-700">{result}</p>}
            {error && <p className="mt-3 rounded bg-red-50 px-3 py-2 text-sm text-red-600">{error}</p>}
          </Card>
        </>
      )}
    </div>
  )
}
