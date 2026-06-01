import { useEffect, useState } from 'react'
import { useAccountList } from '../../trial-balance/hooks/useTrialBalance'
import ReportFilterBar from '../../../shared/components/report/ReportFilterBar'
import Button from '../../../shared/components/ui/Button'
import Card from '../../../shared/components/ui/Card'
import PageHeader from '../../../shared/components/ui/PageHeader'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import { useGeneralLedger } from '../hooks/useGeneralLedger'
import type { GeneralLedgerAccountDto, GeneralLedgerParams } from '../types/generalLedger.types'

function fmt(n: number) {
  if (n === 0) return '—'
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function fmtBalance(n: number) {
  const abs = Math.abs(n).toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  if (n === 0) return <span className="text-gray-400">—</span>
  if (n > 0) return <span className="text-blue-700">{abs} DR</span>
  return <span className="text-rose-600">{abs} CR</span>
}

function fmtDate(iso: string) {
  return new Date(iso).toLocaleDateString('th-TH', { day: '2-digit', month: '2-digit', year: '2-digit' })
}

export default function GeneralLedgerPage() {
  const currentYear = new Date().getFullYear()
  const { companyId } = useCurrentCompany()
  const [year, setYear] = useState(currentYear)
  const [monthFrom, setMonthFrom] = useState(1)
  const [monthTo, setMonthTo] = useState(12)
  const [accountId, setAccountId] = useState<number | undefined>()
  const [params, setParams] = useState<GeneralLedgerParams | null>(null)
  const [expandedIds, setExpandedIds] = useState<Set<number>>(new Set())

  const { data: accounts } = useAccountList(companyId)
  const { data: report, isLoading, isError } = useGeneralLedger(
    params ?? { clientCompanyId: 0, year: currentYear },
    !!params,
  )

  function handleSearch() {
    if (!companyId) return
    setParams({ clientCompanyId: companyId, year, monthFrom, monthTo, accountId })
    setExpandedIds(new Set())
  }

  useEffect(() => {
    setAccountId(undefined)
    setParams(null)
    setExpandedIds(new Set())
  }, [companyId])

  function toggleExpand(id: number) {
    setExpandedIds((prev) => {
      const next = new Set(prev)
      next.has(id) ? next.delete(id) : next.add(id)
      return next
    })
  }

  function expandAll() {
    if (report) setExpandedIds(new Set(report.accounts.map((a) => a.accountId)))
  }

  function collapseAll() {
    setExpandedIds(new Set())
  }

  return (
    <div>
      <PageHeader title="บัญชีแยกประเภท" />

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
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">บัญชี (ทั้งหมด)</label>
            <select
              value={accountId ?? ''}
              onChange={(e) => setAccountId(e.target.value ? Number(e.target.value) : undefined)}
              className="border border-gray-300 rounded px-3 py-2 text-sm w-56 focus:outline-none focus:ring-2 focus:ring-slate-400"
            >
              <option value="">— ทุกบัญชี —</option>
              {accounts?.map((a) => (
                <option key={a.id} value={a.id}>{a.accountCode} {a.accountName}</option>
              ))}
            </select>
          </div>
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
          <Card className="mb-4 flex items-center justify-between px-6 py-4">
            <div>
              <p className="font-semibold text-slate-800 text-lg">{report.clientCode} — {report.clientName}</p>
              <p className="text-sm text-gray-500">
                บัญชีแยกประเภท ปี {report.year}
                {report.monthFrom && report.monthTo ? ` เดือน ${report.monthFrom}–${report.monthTo}` : ''}
                {' '}· {report.accounts.length} บัญชี
              </p>
            </div>
            <div className="flex gap-2 text-sm">
              <Button variant="ghost" onClick={expandAll} className="px-2 py-1">ขยายทั้งหมด</Button>
              <span className="text-gray-300">|</span>
              <Button variant="ghost" onClick={collapseAll} className="px-2 py-1">ย่อทั้งหมด</Button>
            </div>
          </Card>

          <div className="space-y-3">
            {report.accounts.length === 0 && (
              <Card>
                <StateMessage centered>ไม่พบรายการในงวดที่เลือก</StateMessage>
              </Card>
            )}
            {report.accounts.map((acc) => (
              <GLAccountCard
                key={acc.accountId}
                account={acc}
                expanded={expandedIds.has(acc.accountId)}
                onToggle={() => toggleExpand(acc.accountId)}
              />
            ))}
          </div>
        </>
      )}
    </div>
  )
}

function GLAccountCard({
  account, expanded, onToggle,
}: {
  account: GeneralLedgerAccountDto
  expanded: boolean
  onToggle: () => void
}) {
  return (
    <Card className="overflow-hidden">
      {/* Account header — click to expand/collapse */}
      <button
        className="w-full flex items-center justify-between px-5 py-3 hover:bg-slate-50 text-left"
        onClick={onToggle}
      >
        <div className="flex items-center gap-3">
          <span className="font-mono text-sm text-slate-500 w-24 shrink-0">{account.accountCode}</span>
          <span className="font-medium text-slate-800">{account.accountName}</span>
          <span className="text-xs text-gray-400">{account.lines.length} รายการ</span>
        </div>
        <div className="flex items-center gap-6 text-sm">
          <SummaryPill label="เปิด" value={account.openingBalance} />
          <SummaryPill label="DR" value={account.totalDebit} plain />
          <SummaryPill label="CR" value={account.totalCredit} plain />
          <SummaryPill label="ปิด" value={account.closingBalance} highlight />
          <span className="text-gray-400 text-xs ml-2">{expanded ? '▲' : '▼'}</span>
        </div>
      </button>

      {/* Lines */}
      {expanded && (
        <div className="border-t border-gray-100">
          <table className="w-full text-xs">
            <thead className="bg-gray-50">
              <tr>
                <th className="text-left px-4 py-2 font-medium text-gray-500 w-20">วันที่</th>
                <th className="text-left px-4 py-2 font-medium text-gray-500 w-28">เลขที่เอกสาร</th>
                <th className="text-left px-4 py-2 font-medium text-gray-500">รายละเอียด</th>
                <th className="text-right px-4 py-2 font-medium text-gray-500 w-28">เดบิต</th>
                <th className="text-right px-4 py-2 font-medium text-gray-500 w-28">เครดิต</th>
                <th className="text-right px-4 py-2 font-medium text-gray-500 w-32">ยอดคงเหลือ</th>
              </tr>
            </thead>
            <tbody>
              {/* Opening balance row */}
              <tr className="bg-blue-50">
                <td className="px-4 py-1.5 text-gray-400">—</td>
                <td className="px-4 py-1.5 text-gray-400">—</td>
                <td className="px-4 py-1.5 text-blue-600 font-medium">ยอดยกมา</td>
                <td className="px-4 py-1.5 text-right font-mono text-gray-500">—</td>
                <td className="px-4 py-1.5 text-right font-mono text-gray-500">—</td>
                <td className="px-4 py-1.5 text-right font-mono font-medium">{fmtBalance(account.openingBalance)}</td>
              </tr>
              {account.lines.map((ln) => (
                <tr key={`${ln.journalEntryId}-${ln.documentNo}`} className="border-t border-gray-50 hover:bg-gray-50">
                  <td className="px-4 py-1.5 text-gray-500 font-mono">{fmtDate(ln.journalDate)}</td>
                  <td className="px-4 py-1.5 font-mono text-slate-600">{ln.documentNo}</td>
                  <td className="px-4 py-1.5 text-gray-700 truncate max-w-xs" title={ln.description}>
                    {ln.description}
                    {ln.sourceModule && (
                      <span className="ml-1 text-gray-400 text-xs">[{ln.sourceModule}]</span>
                    )}
                  </td>
                  <td className="px-4 py-1.5 text-right font-mono text-blue-700">{fmt(ln.debitAmount)}</td>
                  <td className="px-4 py-1.5 text-right font-mono text-rose-600">{fmt(ln.creditAmount)}</td>
                  <td className="px-4 py-1.5 text-right font-mono font-medium">{fmtBalance(ln.runningBalance)}</td>
                </tr>
              ))}
              {/* Closing balance row */}
              <tr className="bg-slate-50 border-t border-slate-200 font-semibold text-xs">
                <td colSpan={3} className="px-4 py-2 text-right text-slate-600">รวม / ยอดคงเหลือ</td>
                <td className="px-4 py-2 text-right font-mono text-blue-700">{fmt(account.totalDebit)}</td>
                <td className="px-4 py-2 text-right font-mono text-rose-600">{fmt(account.totalCredit)}</td>
                <td className="px-4 py-2 text-right font-mono">{fmtBalance(account.closingBalance)}</td>
              </tr>
            </tbody>
          </table>
        </div>
      )}
    </Card>
  )
}

function SummaryPill({ label, value, plain, highlight }: {
  label: string
  value: number
  plain?: boolean
  highlight?: boolean
}) {
  const color = highlight
    ? value >= 0 ? 'text-blue-700' : 'text-rose-600'
    : plain ? 'text-gray-700' : value >= 0 ? 'text-blue-600' : 'text-rose-500'

  return (
    <span className="flex flex-col items-end">
      <span className="text-gray-400 text-xs leading-none mb-0.5">{label}</span>
      <span className={`font-mono text-sm font-medium ${color}`}>
        {value === 0 ? '—' : Math.abs(value).toLocaleString('th-TH', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}
      </span>
    </span>
  )
}
