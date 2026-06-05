import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import ExportMenu from '../../../../shared/components/ui/ExportMenu'
import { useBankAccounts } from '../../hooks/useBank'
import type { ExportSection } from '../../../../shared/utils/exportTable'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  companyId: number
}

export default function AccountsTab({ companyId }: Props) {
  const { data, isLoading, isError } = useBankAccounts(companyId)

  if (!companyId) return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>

  const rows = data ?? []

  return (
    <div>
      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {data && rows.length === 0 && <Card><StateMessage centered>ไม่มีบัญชีธนาคาร — นำเข้าข้อมูลจาก Express (BKMAS) ที่เมนูนำเข้าข้อมูล</StateMessage></Card>}

      {rows.length > 0 && (
        <Card className="overflow-x-auto">
          <div className="flex items-start justify-between border-b px-4 py-3">
            <p className="text-sm font-semibold text-slate-800">บัญชีเงินฝากธนาคาร ({rows.length} บัญชี)</p>
            <ExportMenu
              meta={{ title: 'บัญชีเงินฝากธนาคาร', fileName: `bank-accounts-${companyId}` }}
              getSections={(): ExportSection[] => [{
                name: 'บัญชีธนาคาร',
                columns: [
                  { key: 'bankName', header: 'ธนาคาร' },
                  { key: 'accountNumber', header: 'เลขที่บัญชี' },
                  { key: 'branch', header: 'สาขา' },
                  { key: 'glAccountCode', header: 'บัญชี GL' },
                  { key: 'transactionCount', header: 'รายการ', align: 'right' },
                  { key: 'currentBalance', header: 'ยอดคงเหลือ', align: 'right' },
                ],
                rows,
              }]}
            />
          </div>
          <table className="w-full text-xs">
            <thead className="bg-slate-50 text-gray-600">
              <tr>
                <th className="px-3 py-2 text-left font-medium">ธนาคาร</th>
                <th className="px-3 py-2 text-left font-medium">เลขที่บัญชี</th>
                <th className="px-3 py-2 text-left font-medium">สาขา</th>
                <th className="px-3 py-2 text-left font-medium">บัญชี GL</th>
                <th className="px-3 py-2 text-right font-medium">รายการ</th>
                <th className="px-3 py-2 text-right font-medium">ยอดคงเหลือ</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => (
                <tr key={r.id} className="border-t border-gray-100 hover:bg-slate-50">
                  <td className="px-3 py-1.5">{r.bankName}</td>
                  <td className="px-3 py-1.5 font-mono text-gray-500">{r.accountNumber || '—'}</td>
                  <td className="px-3 py-1.5 text-gray-600">{r.branch || '—'}</td>
                  <td className="px-3 py-1.5 font-mono text-gray-500">{r.glAccountCode || '—'}</td>
                  <td className="px-3 py-1.5 text-right text-gray-500">{r.transactionCount}</td>
                  <td className={`px-3 py-1.5 text-right font-mono font-semibold ${r.currentBalance < 0 ? 'text-red-600' : ''}`}>{fmt(r.currentBalance)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}
    </div>
  )
}
