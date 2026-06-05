import { useEffect, useState } from 'react'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import ExportMenu from '../../../../shared/components/ui/ExportMenu'
import DataAsOfBanner from '../../../../shared/components/ui/DataAsOfBanner'
import { useBankAccounts, useBankBook } from '../../hooks/useBank'
import type { ExportSection } from '../../../../shared/utils/exportTable'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  companyId: number
  year: number
}

export default function BankBookTab({ companyId, year }: Props) {
  const { data: accounts } = useBankAccounts(companyId)
  const [code, setCode] = useState('')

  useEffect(() => {
    if (accounts && accounts.length > 0 && !accounts.some((a) => a.bankAccountCode === code)) {
      setCode(accounts[0].bankAccountCode)
    }
  }, [accounts]) // eslint-disable-line react-hooks/exhaustive-deps

  const { data, isLoading, isError } = useBankBook(companyId, code, year, !!code)

  if (!companyId) return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>

  return (
    <div>
      <Card className="mb-4 p-4">
        <div className="flex flex-wrap items-end gap-3">
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">บัญชีธนาคาร</label>
            <select value={code} onChange={(e) => setCode(e.target.value)} className="min-w-[260px] rounded border border-gray-300 px-3 py-2 text-sm">
              {(accounts ?? []).map((a) => (
                <option key={a.bankAccountCode} value={a.bankAccountCode}>
                  {a.bankName}{a.accountNumber ? ` · ${a.accountNumber}` : ''}
                </option>
              ))}
            </select>
          </div>
        </div>
      </Card>

      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {data && data.rows.length === 0 && (
        <Card><StateMessage centered>{`ไม่มีรายการเดินบัญชีในปี ${year}`}</StateMessage></Card>
      )}

      {data && data.rows.length > 0 && (
        <>
        <DataAsOfBanner dataAsOf={data.dataAsOf} noun="รายการเดินบัญชีธนาคาร" />
        <Card className="overflow-x-auto">
          <div className="flex items-start justify-between border-b px-4 py-3">
            <div>
              <p className="text-sm font-semibold text-slate-800">สมุดเงินฝากธนาคาร · {data.bankName} ปี {year}</p>
              <p className="text-xs text-gray-500">{data.clientName}{data.accountNumber ? ` · เลขที่ ${data.accountNumber}` : ''} · ยอดยกมา {fmt(data.openingBalance)}</p>
            </div>
            <ExportMenu
              meta={{
                title: `สมุดเงินฝากธนาคาร ${data.bankName} ปี ${year}`,
                subtitle: `${data.clientName} · ยอดยกมา ${fmt(data.openingBalance)}`,
                fileName: `bank-book-${companyId}-${data.bankAccountCode}-${year}`,
              }}
              getSections={(): ExportSection[] => [{
                name: 'สมุดเงินฝาก',
                columns: [
                  { key: 'transactionDate', header: 'วันที่', value: (r) => String(r.transactionDate).slice(0, 10) },
                  { key: 'chequeNo', header: 'เลขที่เช็ค' },
                  { key: 'counterpartyName', header: 'รายการ/คู่ค้า' },
                  { key: 'deposit', header: 'เงินเข้า', align: 'right' },
                  { key: 'withdrawal', header: 'เงินออก', align: 'right' },
                  { key: 'balance', header: 'คงเหลือ', align: 'right' },
                ],
                rows: data.rows,
              }]}
            />
          </div>
          <table className="w-full text-xs">
            <thead className="bg-slate-50 text-gray-600">
              <tr>
                <th className="px-3 py-2 text-left font-medium">วันที่</th>
                <th className="px-3 py-2 text-left font-medium">เลขที่เช็ค</th>
                <th className="px-3 py-2 text-left font-medium">รายการ / คู่ค้า</th>
                <th className="px-3 py-2 text-right font-medium">เงินเข้า</th>
                <th className="px-3 py-2 text-right font-medium">เงินออก</th>
                <th className="px-3 py-2 text-right font-medium">คงเหลือ</th>
              </tr>
            </thead>
            <tbody>
              <tr className="border-t border-gray-100 bg-slate-50/60">
                <td className="px-3 py-1.5 text-gray-500" colSpan={5}>ยอดยกมา</td>
                <td className="px-3 py-1.5 text-right font-mono">{fmt(data.openingBalance)}</td>
              </tr>
              {data.rows.map((r) => (
                <tr key={r.id} className="border-t border-gray-100 hover:bg-slate-50">
                  <td className="px-3 py-1.5 font-mono text-gray-500">{String(r.transactionDate).slice(0, 10)}</td>
                  <td className="px-3 py-1.5 font-mono text-gray-400">{r.chequeNo || '—'}</td>
                  <td className="px-3 py-1.5">{r.counterpartyName || r.remark || r.transactionType || '—'}</td>
                  <td className="px-3 py-1.5 text-right font-mono text-green-700">{r.deposit ? fmt(r.deposit) : ''}</td>
                  <td className="px-3 py-1.5 text-right font-mono text-red-600">{r.withdrawal ? fmt(r.withdrawal) : ''}</td>
                  <td className={`px-3 py-1.5 text-right font-mono ${r.balance < 0 ? 'text-red-600' : ''}`}>{fmt(r.balance)}</td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
                <td className="px-3 py-2" colSpan={3}>รวม / ยอดคงเหลือสิ้นปี</td>
                <td className="px-3 py-2 text-right font-mono text-green-700">{fmt(data.totalDeposit)}</td>
                <td className="px-3 py-2 text-right font-mono text-red-600">{fmt(data.totalWithdrawal)}</td>
                <td className={`px-3 py-2 text-right font-mono ${data.closingBalance < 0 ? 'text-red-600' : ''}`}>{fmt(data.closingBalance)}</td>
              </tr>
            </tfoot>
          </table>
        </Card>
        </>
      )}
    </div>
  )
}
