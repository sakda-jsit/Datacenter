import { useState } from 'react'
import Button from '../../../../shared/components/ui/Button'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import ExportMenu from '../../../../shared/components/ui/ExportMenu'
import { useUpsertExternalInput } from '../../hooks/useFinancialStatement'
import type { FsLineDto, ProfitLossDto } from '../../types/financialStatement.types'
import type { ExportSection } from '../../../../shared/utils/exportTable'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  data?: ProfitLossDto
  isLoading: boolean
  isError: boolean
  queried: boolean
  clientId: number
  fiscalYear: number
}

export default function ProfitLossTab({ data, isLoading, isError, queried, clientId, fiscalYear }: Props) {
  const [showTaxForm, setShowTaxForm] = useState(false)
  const [taxAmount, setTaxAmount] = useState('')
  const [taxNote, setTaxNote] = useState('')
  const upsertTax = useUpsertExternalInput()

  async function saveTax(e: React.FormEvent) {
    e.preventDefault()
    await upsertTax.mutateAsync({
      clientCompanyId: clientId,
      fiscalYear,
      refCode: 'X4',
      amount: Number(taxAmount),
      note: taxNote || undefined,
    })
    setShowTaxForm(false)
  }

  if (isLoading) return <StateMessage>กำลังคำนวณ...</StateMessage>
  if (isError) return <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>
  if (!queried || !data) return (
    <Card>
      <StateMessage centered>เลือกบริษัทและงวดบัญชี แล้วกด "แสดงรายงาน"</StateMessage>
    </Card>
  )

  const isProfit = data.netProfit >= 0

  return (
    <div>
      <Card className="mb-4 flex items-center justify-between px-6 py-4">
        <div>
          <p className="font-semibold text-slate-800 text-lg">{data.clientName}</p>
          <p className="text-sm text-gray-500">
            งบกำไรขาดทุน ปี {data.fiscalYear}
            {data.monthFrom && data.monthTo ? ` เดือน ${data.monthFrom}–${data.monthTo}` : ''}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <ExportMenu
            meta={{ title: `งบกำไรขาดทุน ปี ${data.fiscalYear}`, subtitle: data.clientName, fileName: `profit-loss-${data.clientCode}-${data.fiscalYear}` }}
            getSections={(): ExportSection[] => {
              const cols = [
                { key: 'refCode', header: 'รหัส' },
                { key: 'label', header: 'รายการ' },
                { key: 'amount', header: 'จำนวนเงิน', align: 'right' as const },
              ]
              return [{
                name: 'งบกำไรขาดทุน',
                columns: cols,
                rows: [
                  ...data.incomeLines,
                  data.costOfGoods,
                  ...data.expenseLines,
                  data.financeCost,
                  data.incomeTax,
                ].filter(Boolean),
              }]
            }}
          />
          <Button
            variant="ghost"
            onClick={() => setShowTaxForm(v => !v)}
          >
            ระบุภาษีเงินได้ (X4)
          </Button>
        </div>
      </Card>

      {/* Tax input form */}
      {showTaxForm && (
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-4">
          <p className="text-sm font-medium text-blue-800 mb-3">
            ภาษีเงินได้ (X4) — มาจากแบบ ภงด.50 ไม่ใช่จากบัญชี
          </p>
          <form onSubmit={saveTax} className="flex gap-3 items-end">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">จำนวนภาษี (บาท)</label>
              <input
                type="number" min={0} step={0.01}
                value={taxAmount}
                onChange={(e) => setTaxAmount(e.target.value)}
                required
                className="border border-gray-300 rounded px-3 py-2 text-sm w-40 focus:outline-none focus:ring-2 focus:ring-blue-400"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">หมายเหตุ</label>
              <input
                type="text"
                value={taxNote}
                onChange={(e) => setTaxNote(e.target.value)}
                className="border border-gray-300 rounded px-3 py-2 text-sm w-48 focus:outline-none focus:ring-2 focus:ring-blue-400"
              />
            </div>
            <Button
              type="submit"
              disabled={upsertTax.isPending}
              className="bg-blue-600 hover:bg-blue-700"
            >
              บันทึก
            </Button>
            <Button type="button" variant="secondary" onClick={() => setShowTaxForm(false)}>
              ยกเลิก
            </Button>
          </form>
        </div>
      )}

      <Card className="overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-700 text-white">
            <tr>
              <th className="text-left px-5 py-3 font-medium">รายการ</th>
              <th className="text-right px-5 py-3 font-medium w-36">RefCode</th>
              <th className="text-right px-5 py-3 font-medium w-40">จำนวนเงิน</th>
            </tr>
          </thead>
          <tbody>
            <SectionHeader label="รายได้" />
            {data.incomeLines.map((l) => <FsRow key={l.refCode} line={l} />)}
            <SubtotalRow label="รวมรายได้" amount={data.totalIncome} />

            <SectionHeader label="ต้นทุน" />
            <FsRow line={data.costOfGoods} />
            <SubtotalRow label="กำไรขั้นต้น" amount={data.grossProfit} highlight />

            <SectionHeader label="ค่าใช้จ่ายในการดำเนินงาน" />
            {data.expenseLines.map((l) => <FsRow key={l.refCode} line={l} />)}
            <SubtotalRow label="กำไรก่อนต้นทุนทางการเงิน" amount={data.profitBeforeFinance} highlight />

            <SectionHeader label="ต้นทุนทางการเงิน / ภาษี" />
            <FsRow line={data.financeCost} indent />
            <SubtotalRow label="กำไรก่อนภาษีเงินได้" amount={data.profitBeforeTax} />
            <FsRow line={data.incomeTax} indent />
          </tbody>
          <tfoot>
            <tr className={`font-bold text-base ${isProfit ? 'bg-green-700' : 'bg-red-700'} text-white`}>
              <td colSpan={2} className="px-5 py-4">
                {isProfit ? 'กำไรสุทธิ' : 'ขาดทุนสุทธิ'}
              </td>
              <td className="px-5 py-4 text-right font-mono">{fmt(data.netProfit)}</td>
            </tr>
          </tfoot>
        </table>
      </Card>
    </div>
  )
}

function SectionHeader({ label }: { label: string }) {
  return (
    <tr className="bg-slate-100">
      <td colSpan={3} className="px-5 py-2 font-semibold text-slate-700 text-xs uppercase tracking-wide">
        {label}
      </td>
    </tr>
  )
}

function FsRow({ line, indent }: { line: FsLineDto; indent?: boolean }) {
  const [expanded, setExpanded] = useState(false)
  const hasAccounts = line.accounts.length > 0
  const isNegative = line.amount < 0

  return (
    <>
      <tr
        className={`border-b border-gray-100 hover:bg-slate-50 ${hasAccounts ? 'cursor-pointer' : ''}`}
        onClick={() => hasAccounts && setExpanded(v => !v)}
      >
        <td className="px-5 py-2.5 text-gray-800" style={{ paddingLeft: indent ? '40px' : '20px' }}>
          {hasAccounts && (
            <span className="mr-2 text-gray-400 text-xs">{expanded ? '▼' : '▶'}</span>
          )}
          {line.lineName}
        </td>
        <td className="px-5 py-2.5 text-right font-mono text-xs text-gray-400">{line.refCode}</td>
        <td className={`px-5 py-2.5 text-right font-mono font-medium ${isNegative ? 'text-red-600' : 'text-gray-800'}`}>
          {fmt(line.amount)}
        </td>
      </tr>
      {expanded && line.accounts.map((a) => (
        <tr key={a.accountCode} className="bg-blue-50 border-b border-blue-100">
          <td className="py-1.5 text-xs text-gray-600" style={{ paddingLeft: '56px' }}>
            <span className="font-mono text-gray-400 mr-2">{a.accountCode}</span>
            {a.accountName}
          </td>
          <td />
          <td className="px-5 py-1.5 text-right font-mono text-xs text-gray-600">
            {a.netBalance.toLocaleString('th-TH', { minimumFractionDigits: 2 })}
          </td>
        </tr>
      ))}
    </>
  )
}

function SubtotalRow({ label, amount, highlight }: { label: string; amount: number; highlight?: boolean }) {
  return (
    <tr className={`border-t-2 border-slate-300 font-semibold ${highlight ? 'bg-slate-50' : ''}`}>
      <td colSpan={2} className="px-5 py-2.5 text-slate-700">{label}</td>
      <td className={`px-5 py-2.5 text-right font-mono ${amount < 0 ? 'text-red-600' : 'text-slate-800'}`}>
        {fmt(amount)}
      </td>
    </tr>
  )
}
