import { useState } from 'react'
import Button from '../../../../shared/components/ui/Button'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import { useGenerateLeaseAdjustment, useLeaseWorkpaper } from '../../hooks/useLeasing'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

const ROLE_LABEL: Record<string, string> = {
  Liability: 'หนี้สินตามสัญญา (gross)',
  DeferredInterest: 'ดอกเบี้ยรอตัดบัญชี',
  VatUndue: 'ภาษีซื้อยังไม่ถึงกำหนด',
}

interface Props {
  companyId: number
  fiscalYear: number
}

export default function WorkpaperTab({ companyId, fiscalYear }: Props) {
  const { data, isLoading, isError } = useLeaseWorkpaper(companyId, fiscalYear)
  const generate = useGenerateLeaseAdjustment(companyId)
  const [selected, setSelected] = useState<Set<number>>(new Set())
  const [result, setResult] = useState<string | null>(null)
  const [error, setError] = useState('')

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
      prev.size === data.rows.length ? new Set() : new Set(data.rows.map((r) => r.contractId)),
    )
  }

  async function handleGenerate() {
    setError('')
    setResult(null)
    try {
      const adj = await generate.mutateAsync({ fiscalYear, contractIds: [...selected] })
      setResult(`สร้างรายการปรับปรุง ${adj.documentNo} แล้ว (ดอกเบี้ยรวม ${fmt(adj.totalDebit)}) — ดูที่หน้า "กระดาษทำการปิดงบ"`)
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
        <Card><StateMessage centered>ยังไม่มีสัญญา — เพิ่มที่แท็บ "สัญญาเช่าซื้อ/เงินกู้"</StateMessage></Card>
      )}

      {data && data.rows.length > 0 && (
        <>
          {/* สรุปต่อสัญญา */}
          <Card className="mb-4 overflow-x-auto">
            <div className="border-b px-4 py-3">
              <p className="text-sm font-semibold text-slate-800">กระดาษทำการ ณ สิ้นปี {fiscalYear}</p>
              <p className="text-xs text-gray-500">{data.clientName} ({data.clientCode})</p>
            </div>
            <table className="w-full text-xs">
              <thead className="bg-slate-50 text-gray-600">
                <tr>
                  <th className="px-3 py-2 text-left font-medium">
                    <input type="checkbox" checked={selected.size === data.rows.length} onChange={toggleAll} className="mr-2 rounded" />
                    สัญญา
                  </th>
                  <th className="px-3 py-2 text-right font-medium">หนี้สิน คงเหลือ</th>
                  <th className="px-3 py-2 text-right font-medium">– ถึงกำหนด 1 ปี</th>
                  <th className="px-3 py-2 text-right font-medium">– ระยะยาว</th>
                  <th className="px-3 py-2 text-right font-medium">ดบ.รอตัด คงเหลือ</th>
                  <th className="px-3 py-2 text-right font-medium">ดอกเบี้ยรับรู้ปีนี้</th>
                </tr>
              </thead>
              <tbody>
                {data.rows.map((r) => (
                  <tr key={r.contractId} className="border-t border-gray-100 hover:bg-slate-50">
                    <td className="px-3 py-1.5">
                      <label className="flex items-center gap-2">
                        <input type="checkbox" checked={selected.has(r.contractId)} onChange={() => toggle(r.contractId)} className="rounded" />
                        <span><span className="font-mono text-gray-500">{r.contractNo}</span> {r.assetName}</span>
                      </label>
                    </td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(r.grossLiability.closing)}</td>
                    <td className="px-3 py-1.5 text-right font-mono text-gray-500">{fmt(r.grossLiability.currentPortion)}</td>
                    <td className="px-3 py-1.5 text-right font-mono text-gray-500">{fmt(r.grossLiability.longTerm)}</td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(r.deferredInterest.closing)}</td>
                    <td className="px-3 py-1.5 text-right font-mono text-sky-700">{fmt(r.interestRecognizedInYear)}</td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
                  <td className="px-3 py-2 text-right">รวม</td>
                  <td className="px-3 py-2 text-right font-mono">{fmt(data.totalGrossLiabilityClosing)}</td>
                  <td className="px-3 py-2" />
                  <td className="px-3 py-2" />
                  <td className="px-3 py-2 text-right font-mono">{fmt(data.totalDeferredInterestClosing)}</td>
                  <td className="px-3 py-2 text-right font-mono text-sky-700">{fmt(data.totalInterestRecognized)}</td>
                </tr>
              </tfoot>
            </table>
          </Card>

          {/* เทียบ GL */}
          <Card className="mb-4 overflow-x-auto">
            <div className="border-b px-4 py-3">
              <p className="text-sm font-semibold text-slate-800">เทียบยอด schedule กับ GL (สะสมถึงสิ้นปี {fiscalYear})</p>
              <p className="text-xs text-gray-500">
                {data.hasDifference
                  ? 'มีผลต่าง — ตรวจสอบและปรับปรุงตามความเหมาะสม'
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
                        <td className="px-3 py-1.5 text-right font-mono">{fmt(g.scheduleClosing)}</td>
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
            <p className="text-sm font-semibold text-slate-800">สร้างรายการปรับปรุงดอกเบี้ยรับรู้ในปี</p>
            <p className="mb-3 text-xs text-gray-500">
              เลือกสัญญาด้านบน → ระบบสร้าง AdjustmentEntry (เช่าซื้อ: Dr ดอกเบี้ยจ่าย / Cr ดอกเบี้ยรอตัด;
              เงินกู้: Dr ดอกเบี้ยจ่าย / Cr หนี้สิน) ลงวันที่ 31 ธ.ค. {fiscalYear}
            </p>
            <div className="flex items-center gap-3">
              <Button type="button" onClick={handleGenerate} disabled={selected.size === 0 || generate.isPending}>
                {generate.isPending ? 'กำลังสร้าง...' : `สร้างรายการปรับปรุง (${selected.size} สัญญา)`}
              </Button>
              {selected.size === 0 && <span className="text-xs text-gray-400">เลือกสัญญาอย่างน้อย 1 รายการ</span>}
            </div>
            {result && <p className="mt-3 rounded bg-green-50 px-3 py-2 text-sm text-green-700">{result}</p>}
            {error && <p className="mt-3 rounded bg-red-50 px-3 py-2 text-sm text-red-600">{error}</p>}
          </Card>
        </>
      )}
    </div>
  )
}
