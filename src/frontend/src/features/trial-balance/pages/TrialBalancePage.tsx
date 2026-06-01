import { useEffect, useState } from 'react'
import ReportFilterBar from '../../../shared/components/report/ReportFilterBar'
import Card from '../../../shared/components/ui/Card'
import PageHeader from '../../../shared/components/ui/PageHeader'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import { useTrialBalance } from '../hooks/useTrialBalance'
import type { TrialBalanceParams, TrialBalanceRowDto } from '../types/trialBalance.types'

const ACCOUNT_TYPE_LABEL: Record<string, string> = {
  Asset: 'สินทรัพย์',
  Liability: 'หนี้สิน',
  Equity: 'ส่วนของเจ้าของ',
  Income: 'รายได้',
  Expense: 'ค่าใช้จ่าย',
}

function fmt(n: number) {
  if (n === 0) return '—'
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function fmtSigned(n: number) {
  if (n === 0) return '—'
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

export default function TrialBalancePage() {
  const currentYear = new Date().getFullYear()
  const { companyId } = useCurrentCompany()
  const [year, setYear] = useState(currentYear)
  const [monthFrom, setMonthFrom] = useState(1)
  const [monthTo, setMonthTo] = useState(12)
  const [includeZero, setIncludeZero] = useState(false)
  const [params, setParams] = useState<TrialBalanceParams | null>(null)

  const { data: report, isLoading, isError } = useTrialBalance(
    params ?? { clientCompanyId: 0, year: currentYear },
    !!params,
  )

  useEffect(() => {
    setParams(null)
  }, [companyId])

  function handleSearch() {
    if (!companyId) return
    setParams({ clientCompanyId: companyId, year, monthFrom, monthTo, includeZeroBalance: includeZero })
  }

  // Group rows by AccountType for section headers
  const sections = report
    ? Object.entries(
        report.rows.reduce<Record<string, TrialBalanceRowDto[]>>((acc, row) => {
          const key = row.accountType
          if (!acc[key]) acc[key] = []
          acc[key].push(row)
          return acc
        }, {}),
      )
    : []

  return (
    <div>
      <PageHeader title="งบทดลอง" />

      <ReportFilterBar
        clients={[]}
        clientId={companyId}
        year={year}
        monthFrom={monthFrom}
        monthTo={monthTo}
        onClientChange={() => undefined}
        onYearChange={setYear}
        onMonthFromChange={setMonthFrom}
        onMonthToChange={setMonthTo}
        onSearch={handleSearch}
        loading={isLoading}
        extra={
          <label className="flex items-center gap-2 text-sm text-gray-600 pb-2">
            <input
              type="checkbox"
              checked={includeZero}
              onChange={(e) => setIncludeZero(e.target.checked)}
              className="rounded"
            />
            รวมบัญชียอดศูนย์
          </label>
        }
      />

      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}

      {!params && !report && (
        <Card>
          <StateMessage centered>{companyId ? 'เลือกงวดบัญชี แล้วกด "แสดงรายงาน"' : 'เลือกบริษัทที่ header ก่อน แล้วจึงแสดงรายงาน'}</StateMessage>
        </Card>
      )}

      {report && (
        <>
          {/* Header */}
          <Card className="mb-4 px-6 py-4">
            <p className="font-semibold text-slate-800 text-lg">{report.clientCode} — {report.clientName}</p>
            <p className="text-sm text-gray-500">
              งบทดลอง ปี {report.year}
              {report.monthFrom && report.monthTo
                ? ` เดือน ${report.monthFrom}–${report.monthTo}`
                : ''}
              {' '}· {report.rows.length} บัญชี
            </p>
          </Card>

          <Card className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-slate-700 text-white">
                <tr>
                  <th className="text-left px-4 py-3 font-medium w-28">รหัสบัญชี</th>
                  <th className="text-left px-4 py-3 font-medium">ชื่อบัญชี</th>
                  <th className="text-right px-4 py-3 font-medium w-32">ยอดยกมา DR</th>
                  <th className="text-right px-4 py-3 font-medium w-32">ยอดยกมา CR</th>
                  <th className="text-right px-4 py-3 font-medium w-32">เดบิต</th>
                  <th className="text-right px-4 py-3 font-medium w-32">เครดิต</th>
                  <th className="text-right px-4 py-3 font-medium w-32">คงเหลือ DR</th>
                  <th className="text-right px-4 py-3 font-medium w-32">คงเหลือ CR</th>
                </tr>
              </thead>
              <tbody>
                {sections.map(([type, rows]) => (
                  <TbSection key={type} type={type} rows={rows} />
                ))}
              </tbody>
              <tfoot className="bg-slate-800 text-white font-semibold">
                <tr>
                  <td colSpan={2} className="px-4 py-3 text-right">รวมทั้งสิ้น</td>
                  <td className="px-4 py-3 text-right">{fmt(report.totalBeginDebit)}</td>
                  <td className="px-4 py-3 text-right">{fmt(report.totalBeginCredit)}</td>
                  <td className="px-4 py-3 text-right">{fmt(report.totalPeriodDebit)}</td>
                  <td className="px-4 py-3 text-right">{fmt(report.totalPeriodCredit)}</td>
                  <td className="px-4 py-3 text-right">{fmt(report.totalEndDebit)}</td>
                  <td className="px-4 py-3 text-right">{fmt(report.totalEndCredit)}</td>
                </tr>
                <tr className="border-t border-slate-600">
                  <td colSpan={2} className="px-4 py-2 text-right text-xs text-slate-300">ผลต่าง DR−CR</td>
                  <td className="px-4 py-2 text-right text-xs text-slate-300">
                    {fmtSigned(report.totalBeginDebit - report.totalBeginCredit)}
                  </td>
                  <td />
                  <td className="px-4 py-2 text-right text-xs text-slate-300">
                    {fmtSigned(report.totalPeriodDebit - report.totalPeriodCredit)}
                  </td>
                  <td />
                  <td className="px-4 py-2 text-right text-xs text-slate-300">
                    {fmtSigned(report.totalEndDebit - report.totalEndCredit)}
                  </td>
                  <td />
                </tr>
              </tfoot>
            </table>
          </Card>
        </>
      )}
    </div>
  )
}

function TbSection({ type, rows }: { type: string; rows: TrialBalanceRowDto[] }) {
  return (
    <>
      <tr className="bg-slate-100">
        <td colSpan={8} className="px-4 py-2 font-semibold text-slate-700 text-xs uppercase tracking-wide">
          {ACCOUNT_TYPE_LABEL[type] ?? type}
        </td>
      </tr>
      {rows.map((row) => (
        <TrialBalanceRow key={row.accountId} row={row} />
      ))}
      <SectionTotal label={`รวม${ACCOUNT_TYPE_LABEL[type] ?? type}`} rows={rows} />
    </>
  )
}

function TrialBalanceRow({ row }: { row: TrialBalanceRowDto }) {
  const indent = Math.max(0, row.level - 1) * 16
  return (
    <tr className="border-b border-gray-100 hover:bg-slate-50">
      <td className="px-4 py-2 font-mono text-slate-600 text-xs">{row.accountCode}</td>
      <td className="px-4 py-2 text-gray-800" style={{ paddingLeft: `${16 + indent}px` }}>
        {row.accountName}
      </td>
      <td className="px-4 py-2 text-right font-mono text-gray-700">{fmt(row.beginDebit)}</td>
      <td className="px-4 py-2 text-right font-mono text-gray-700">{fmt(row.beginCredit)}</td>
      <td className="px-4 py-2 text-right font-mono text-blue-700">{fmt(row.periodDebit)}</td>
      <td className="px-4 py-2 text-right font-mono text-blue-700">{fmt(row.periodCredit)}</td>
      <td className="px-4 py-2 text-right font-mono font-medium text-slate-800">{fmt(row.endDebit)}</td>
      <td className="px-4 py-2 text-right font-mono font-medium text-slate-800">{fmt(row.endCredit)}</td>
    </tr>
  )
}

function SectionTotal({ label, rows }: { label: string; rows: TrialBalanceRowDto[] }) {
  const bd = rows.reduce((s, r) => s + r.beginDebit, 0)
  const bc = rows.reduce((s, r) => s + r.beginCredit, 0)
  const pd = rows.reduce((s, r) => s + r.periodDebit, 0)
  const pc = rows.reduce((s, r) => s + r.periodCredit, 0)
  const ed = rows.reduce((s, r) => s + r.endDebit, 0)
  const ec = rows.reduce((s, r) => s + r.endCredit, 0)
  return (
    <tr className="bg-slate-50 border-t border-slate-200 font-semibold text-slate-700">
      <td />
      <td className="px-4 py-2 text-right text-xs">{label}</td>
      <td className="px-4 py-2 text-right font-mono text-xs">{fmt(bd)}</td>
      <td className="px-4 py-2 text-right font-mono text-xs">{fmt(bc)}</td>
      <td className="px-4 py-2 text-right font-mono text-xs">{fmt(pd)}</td>
      <td className="px-4 py-2 text-right font-mono text-xs">{fmt(pc)}</td>
      <td className="px-4 py-2 text-right font-mono text-xs">{fmt(ed)}</td>
      <td className="px-4 py-2 text-right font-mono text-xs">{fmt(ec)}</td>
    </tr>
  )
}
