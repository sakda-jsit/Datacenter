import { useState } from 'react'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import type { BalanceSheetDto, FsLineDto } from '../../types/financialStatement.types'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  data?: BalanceSheetDto
  isLoading: boolean
  isError: boolean
  queried: boolean
}

export default function BalanceSheetTab({ data, isLoading, isError, queried }: Props) {
  if (isLoading) return <StateMessage>กำลังคำนวณ...</StateMessage>
  if (isError) return <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>
  if (!queried || !data) return (
    <Card>
      <StateMessage centered>เลือกบริษัทและปีบัญชี แล้วกด "แสดงรายงาน"</StateMessage>
    </Card>
  )

  const balanced = Math.abs(data.balanceDifference) < 0.01

  return (
    <div>
      <Card className="mb-4 px-6 py-4">
        <p className="font-semibold text-slate-800 text-lg">{data.clientCode} — {data.clientName}</p>
        <p className="text-sm text-gray-500">งบแสดงฐานะการเงิน ณ วันที่ 31 ธันวาคม {data.fiscalYear}</p>
        {!balanced && (
          <p className="text-red-500 text-xs mt-1 font-medium">
            ⚠ งบไม่สมดุล: ผลต่าง {fmt(data.balanceDifference)} บาท — ตรวจสอบ Mapping
          </p>
        )}
        {balanced && (
          <p className="text-green-600 text-xs mt-1">✓ งบสมดุล</p>
        )}
      </Card>

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
        {/* Left: Assets */}
        <Card className="overflow-hidden">
          <div className="bg-blue-700 text-white px-5 py-3">
            <p className="font-semibold">สินทรัพย์</p>
          </div>
          <table className="w-full text-sm">
            <tbody>
              {/* Current assets: sort 11-16 */}
              <BsSection label="สินทรัพย์หมุนเวียน" />
              {data.assets.filter(l => l.sortOrder <= 16).map(l => <BsRow key={l.refCode} line={l} />)}
              <BsSubtotal label="รวมสินทรัพย์หมุนเวียน"
                amount={data.assets.filter(l => l.sortOrder <= 16).reduce((s, l) => s + l.amount, 0)} />

              {/* Non-current assets: sort 21+ */}
              <BsSection label="สินทรัพย์ไม่หมุนเวียน" />
              {data.assets.filter(l => l.sortOrder >= 21).map(l => <BsRow key={l.refCode} line={l} />)}
              <BsSubtotal label="รวมสินทรัพย์ไม่หมุนเวียน"
                amount={data.assets.filter(l => l.sortOrder >= 21).reduce((s, l) => s + l.amount, 0)} />
            </tbody>
            <tfoot>
              <tr className="bg-blue-700 text-white font-bold">
                <td colSpan={2} className="px-5 py-3">รวมสินทรัพย์ทั้งสิ้น</td>
                <td className="px-5 py-3 text-right font-mono">{fmt(data.totalAssets)}</td>
              </tr>
            </tfoot>
          </table>
        </Card>

        {/* Right: Liabilities + Equity */}
        <Card className="overflow-hidden">
          <div className="bg-slate-700 text-white px-5 py-3">
            <p className="font-semibold">หนี้สินและส่วนของเจ้าของ</p>
          </div>
          <table className="w-full text-sm">
            <tbody>
              {/* Current liabilities: sort 31-34 */}
              <BsSection label="หนี้สินหมุนเวียน" />
              {data.liabilities.filter(l => l.sortOrder <= 34).map(l => <BsRow key={l.refCode} line={l} />)}
              <BsSubtotal label="รวมหนี้สินหมุนเวียน"
                amount={data.liabilities.filter(l => l.sortOrder <= 34).reduce((s, l) => s + l.amount, 0)} />

              {/* Non-current liabilities: sort 41+ */}
              <BsSection label="หนี้สินไม่หมุนเวียน" />
              {data.liabilities.filter(l => l.sortOrder >= 41).map(l => <BsRow key={l.refCode} line={l} />)}
              <BsSubtotal label="รวมหนี้สินทั้งสิ้น" amount={data.totalLiabilities} />

              <BsSection label="ส่วนของเจ้าของ" />
              {data.equity.map(l => <BsRow key={l.refCode} line={l} />)}
              <BsSubtotal label="รวมส่วนของเจ้าของ" amount={data.totalEquity} />
            </tbody>
            <tfoot>
              <tr className="bg-slate-700 text-white font-bold">
                <td colSpan={2} className="px-5 py-3">รวมหนี้สินและส่วนของเจ้าของ</td>
                <td className="px-5 py-3 text-right font-mono">{fmt(data.totalLiabilitiesAndEquity)}</td>
              </tr>
            </tfoot>
          </table>
        </Card>
      </div>
    </div>
  )
}

function BsSection({ label }: { label: string }) {
  return (
    <tr className="bg-slate-100">
      <td colSpan={3} className="px-5 py-2 font-semibold text-slate-700 text-xs uppercase tracking-wide">
        {label}
      </td>
    </tr>
  )
}

function BsRow({ line }: { line: FsLineDto }) {
  const [expanded, setExpanded] = useState(false)
  const hasAccounts = line.accounts.length > 0

  return (
    <>
      <tr
        className={`border-b border-gray-100 hover:bg-slate-50 ${hasAccounts ? 'cursor-pointer' : ''}`}
        onClick={() => hasAccounts && setExpanded(v => !v)}
      >
        <td className="px-5 py-2.5 text-xs text-gray-400 font-mono w-14">{line.refCode}</td>
        <td className="px-2 py-2.5 text-gray-800">
          {hasAccounts && <span className="mr-1 text-gray-400 text-xs">{expanded ? '▼' : '▶'}</span>}
          {line.lineName}
        </td>
        <td className="px-5 py-2.5 text-right font-mono text-gray-800 w-36">
          {line.amount !== 0 ? fmt(line.amount) : '—'}
        </td>
      </tr>
      {expanded && line.accounts.map((a) => (
        <tr key={a.accountCode} className="bg-blue-50 border-b border-blue-100">
          <td />
          <td className="py-1.5 pl-8 text-xs text-gray-600">
            <span className="font-mono text-gray-400 mr-2">{a.accountCode}</span>
            {a.accountName}
          </td>
          <td className="px-5 py-1.5 text-right font-mono text-xs text-gray-600">
            {a.netBalance.toLocaleString('th-TH', { minimumFractionDigits: 2 })}
          </td>
        </tr>
      ))}
    </>
  )
}

function BsSubtotal({ label, amount }: { label: string; amount: number }) {
  return (
    <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
      <td />
      <td className="px-2 py-2 text-slate-700 text-sm">{label}</td>
      <td className="px-5 py-2 text-right font-mono text-slate-800">{fmt(amount)}</td>
    </tr>
  )
}
