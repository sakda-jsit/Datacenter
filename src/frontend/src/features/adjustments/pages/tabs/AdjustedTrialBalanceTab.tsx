import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import ExportMenu from '../../../../shared/components/ui/ExportMenu'
import { ACCOUNT_TYPE_LABEL } from '../../types/adjustment.types'
import type { AdjustedTrialBalanceReportDto, AdjustedTrialBalanceRowDto } from '../../types/adjustment.types'
import type { ExportSection } from '../../../../shared/utils/exportTable'

function fmt(n: number) {
  if (n === 0) return '—'
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  data?: AdjustedTrialBalanceReportDto
  isLoading: boolean
  isError: boolean
  queried: boolean
  companyId: number
}

export default function AdjustedTrialBalanceTab({ data, isLoading, isError, queried, companyId }: Props) {
  if (isError) return <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>
  if (isLoading) return <StateMessage>กำลังคำนวณ...</StateMessage>
  if (!queried || !data) {
    return (
      <Card>
        <StateMessage centered>
          {companyId ? 'เลือกปีบัญชี แล้วกด "แสดงรายงาน"' : 'เลือกบริษัทที่ header ก่อน'}
        </StateMessage>
      </Card>
    )
  }

  const sections = Object.entries(
    data.rows.reduce<Record<string, AdjustedTrialBalanceRowDto[]>>((acc, row) => {
      ;(acc[row.accountType] ??= []).push(row)
      return acc
    }, {}),
  )

  const exportSections = (): ExportSection[] => [{
    name: 'งบทดลองหลังปรับปรุง',
    columns: [
      { key: 'accountCode', header: 'รหัสบัญชี' },
      { key: 'accountName', header: 'ชื่อบัญชี' },
      { key: 'balanceBeforeDebit', header: 'ก่อนปรับ DR', align: 'right' },
      { key: 'balanceBeforeCredit', header: 'ก่อนปรับ CR', align: 'right' },
      { key: 'adjustmentDebit', header: 'ปรับปรุง DR', align: 'right' },
      { key: 'adjustmentCredit', header: 'ปรับปรุง CR', align: 'right' },
      { key: 'finalDebit', header: 'หลังปรับ DR', align: 'right' },
      { key: 'finalCredit', header: 'หลังปรับ CR', align: 'right' },
    ],
    rows: data.rows,
  }]

  return (
    <div>
      <Card className="mb-4 flex items-start justify-between px-6 py-4">
        <div>
        <p className="text-lg font-semibold text-slate-800">{data.clientName}</p>
        <p className="text-sm text-gray-500">
          งบทดลองหลังปรับปรุง ปีบัญชี {data.fiscalYear} · {data.rows.length} บัญชี
        </p>
        <div className="mt-2 flex flex-wrap gap-2 text-xs">
          <BalanceBadge ok={data.balancedBefore} label="ยอดก่อนปรับสมดุล" />
          <BalanceBadge ok={data.adjustmentsBalanced} label="รายการปรับปรุงสมดุล" />
          <BalanceBadge ok={data.balancedAfter} label="ยอดหลังปรับสมดุล" />
        </div>
        </div>
        <ExportMenu
          meta={{ title: `งบทดลองหลังปรับปรุง ปี ${data.fiscalYear}`, subtitle: data.clientName, fileName: `adjusted-tb-${data.clientCode}-${data.fiscalYear}` }}
          getSections={exportSections}
        />
      </Card>

      <Card className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="bg-slate-700 text-white">
            <tr>
              <th rowSpan={2} className="px-4 py-2 text-left font-medium w-24">รหัส</th>
              <th rowSpan={2} className="px-4 py-2 text-left font-medium">ชื่อบัญชี</th>
              <th colSpan={2} className="px-4 py-2 text-center font-medium border-l border-slate-500">ก่อนปรับปรุง</th>
              <th colSpan={2} className="px-4 py-2 text-center font-medium border-l border-slate-500">รายการปรับปรุง</th>
              <th colSpan={2} className="px-4 py-2 text-center font-medium border-l border-slate-500">หลังปรับปรุง</th>
            </tr>
            <tr className="text-xs">
              <th className="px-4 py-1.5 text-right font-medium border-l border-slate-500 w-28">เดบิต</th>
              <th className="px-4 py-1.5 text-right font-medium w-28">เครดิต</th>
              <th className="px-4 py-1.5 text-right font-medium border-l border-slate-500 w-28">เดบิต</th>
              <th className="px-4 py-1.5 text-right font-medium w-28">เครดิต</th>
              <th className="px-4 py-1.5 text-right font-medium border-l border-slate-500 w-28">เดบิต</th>
              <th className="px-4 py-1.5 text-right font-medium w-28">เครดิต</th>
            </tr>
          </thead>
          <tbody>
            {sections.map(([type, rows]) => (
              <AtbSection key={type} type={type} rows={rows} />
            ))}
          </tbody>
          <tfoot className="bg-slate-800 text-white font-semibold">
            <tr>
              <td colSpan={2} className="px-4 py-3 text-right">รวมทั้งสิ้น</td>
              <td className="px-4 py-3 text-right font-mono border-l border-slate-600">{fmt(data.totalBalanceBeforeDebit)}</td>
              <td className="px-4 py-3 text-right font-mono">{fmt(data.totalBalanceBeforeCredit)}</td>
              <td className="px-4 py-3 text-right font-mono border-l border-slate-600">{fmt(data.totalAdjustmentDebit)}</td>
              <td className="px-4 py-3 text-right font-mono">{fmt(data.totalAdjustmentCredit)}</td>
              <td className="px-4 py-3 text-right font-mono border-l border-slate-600">{fmt(data.totalFinalDebit)}</td>
              <td className="px-4 py-3 text-right font-mono">{fmt(data.totalFinalCredit)}</td>
            </tr>
          </tfoot>
        </table>
      </Card>
    </div>
  )
}

function BalanceBadge({ ok, label }: { ok: boolean; label: string }) {
  return (
    <span className={`rounded-full px-2.5 py-1 font-medium ${ok ? 'bg-green-50 text-green-700' : 'bg-red-50 text-red-600'}`}>
      {ok ? '✓' : '⚠'} {label}
    </span>
  )
}

function AtbSection({ type, rows }: { type: string; rows: AdjustedTrialBalanceRowDto[] }) {
  const label = ACCOUNT_TYPE_LABEL[Number(type)] ?? type
  const sum = (pick: (r: AdjustedTrialBalanceRowDto) => number) => rows.reduce((s, r) => s + pick(r), 0)
  return (
    <>
      <tr className="bg-slate-100">
        <td colSpan={8} className="px-4 py-2 text-xs font-semibold uppercase tracking-wide text-slate-700">{label}</td>
      </tr>
      {rows.map((row) => {
        const hasAdj = row.adjustmentDebit !== 0 || row.adjustmentCredit !== 0
        return (
          <tr key={row.accountId} className={`border-b border-gray-100 hover:bg-slate-50 ${hasAdj ? 'bg-amber-50/40' : ''}`}>
            <td className="px-4 py-2 font-mono text-xs text-slate-600">{row.accountCode}</td>
            <td className="px-4 py-2 text-gray-800" style={{ paddingLeft: `${16 + Math.max(0, row.level - 1) * 16}px` }}>
              {row.accountName}
            </td>
            <td className="px-4 py-2 text-right font-mono text-gray-700 border-l border-gray-100">{fmt(row.balanceBeforeDebit)}</td>
            <td className="px-4 py-2 text-right font-mono text-gray-700">{fmt(row.balanceBeforeCredit)}</td>
            <td className="px-4 py-2 text-right font-mono text-amber-700 border-l border-gray-100">{fmt(row.adjustmentDebit)}</td>
            <td className="px-4 py-2 text-right font-mono text-amber-700">{fmt(row.adjustmentCredit)}</td>
            <td className="px-4 py-2 text-right font-mono font-medium text-slate-800 border-l border-gray-100">{fmt(row.finalDebit)}</td>
            <td className="px-4 py-2 text-right font-mono font-medium text-slate-800">{fmt(row.finalCredit)}</td>
          </tr>
        )
      })}
      <tr className="border-t border-slate-200 bg-slate-50 font-semibold text-slate-700">
        <td />
        <td className="px-4 py-2 text-right text-xs">รวม{label}</td>
        <td className="px-4 py-2 text-right font-mono text-xs border-l border-slate-200">{fmt(sum((r) => r.balanceBeforeDebit))}</td>
        <td className="px-4 py-2 text-right font-mono text-xs">{fmt(sum((r) => r.balanceBeforeCredit))}</td>
        <td className="px-4 py-2 text-right font-mono text-xs border-l border-slate-200">{fmt(sum((r) => r.adjustmentDebit))}</td>
        <td className="px-4 py-2 text-right font-mono text-xs">{fmt(sum((r) => r.adjustmentCredit))}</td>
        <td className="px-4 py-2 text-right font-mono text-xs border-l border-slate-200">{fmt(sum((r) => r.finalDebit))}</td>
        <td className="px-4 py-2 text-right font-mono text-xs">{fmt(sum((r) => r.finalCredit))}</td>
      </tr>
    </>
  )
}
