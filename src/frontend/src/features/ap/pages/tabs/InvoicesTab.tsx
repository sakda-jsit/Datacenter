import { useState } from 'react'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import ExportMenu from '../../../../shared/components/ui/ExportMenu'
import { useApInvoices } from '../../hooks/useAp'
import type { ExportSection } from '../../../../shared/utils/exportTable'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  companyId: number
  year: number
}

export default function InvoicesTab({ companyId, year }: Props) {
  const [outstandingOnly, setOutstandingOnly] = useState(false)
  const { data, isLoading, isError } = useApInvoices(companyId, year, outstandingOnly)

  if (!companyId) return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>

  const rows = data ?? []

  return (
    <div>
      <Card className="mb-4 p-4">
        <label className="flex items-center gap-2 text-sm text-gray-700">
          <input type="checkbox" checked={outstandingOnly} onChange={(e) => setOutstandingOnly(e.target.checked)} className="rounded" />
          แสดงเฉพาะที่ยังค้างชำระ
        </label>
      </Card>

      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {data && rows.length === 0 && <Card><StateMessage centered>ไม่มีใบตั้งหนี้ตามเงื่อนไข</StateMessage></Card>}

      {rows.length > 0 && (
        <Card className="overflow-x-auto">
          <div className="flex items-start justify-between border-b px-4 py-3">
            <p className="text-sm font-semibold text-slate-800">
              ใบตั้งหนี้เจ้าหนี้ · ปี {year} ({rows.length} ใบ){outstandingOnly ? ' · เฉพาะค้างชำระ' : ''}
            </p>
            <ExportMenu
              meta={{ title: `ใบตั้งหนี้เจ้าหนี้ ปี ${year}`, fileName: `ap-invoices-${companyId}-${year}` }}
              getSections={(): ExportSection[] => [{
                name: 'ใบตั้งหนี้',
                columns: [
                  { key: 'documentNo', header: 'เลขที่' },
                  { key: 'documentDate', header: 'วันที่', value: (r) => String(r.documentDate).slice(0, 10) },
                  { key: 'dueDate', header: 'ครบกำหนด', value: (r) => (r.dueDate ? String(r.dueDate).slice(0, 10) : '') },
                  { key: 'supplierName', header: 'ผู้ขาย' },
                  { key: 'netAmount', header: 'รวมทั้งสิ้น', align: 'right' },
                  { key: 'paidAmount', header: 'จ่ายชำระ', align: 'right' },
                  { key: 'outstandingAmount', header: 'คงค้าง', align: 'right' },
                ],
                rows,
              }]}
            />
          </div>
          <table className="w-full text-xs">
            <thead className="bg-slate-50 text-gray-600">
              <tr>
                <th className="px-3 py-2 text-left font-medium">เลขที่</th>
                <th className="px-3 py-2 text-left font-medium">วันที่</th>
                <th className="px-3 py-2 text-left font-medium">ครบกำหนด</th>
                <th className="px-3 py-2 text-left font-medium">ผู้ขาย</th>
                <th className="px-3 py-2 text-right font-medium">รวมทั้งสิ้น</th>
                <th className="px-3 py-2 text-right font-medium">จ่ายชำระ</th>
                <th className="px-3 py-2 text-right font-medium">คงค้าง</th>
                <th className="px-3 py-2 text-center font-medium">สถานะ</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => (
                <tr key={r.id} className="border-t border-gray-100 hover:bg-slate-50">
                  <td className="px-3 py-1.5 font-mono">{r.documentNo}</td>
                  <td className="px-3 py-1.5 font-mono text-gray-500">{String(r.documentDate).slice(0, 10)}</td>
                  <td className="px-3 py-1.5 font-mono text-gray-500">{r.dueDate ? String(r.dueDate).slice(0, 10) : '—'}</td>
                  <td className="px-3 py-1.5">{r.supplierName || r.supplierCode}</td>
                  <td className="px-3 py-1.5 text-right font-mono">{fmt(r.netAmount)}</td>
                  <td className="px-3 py-1.5 text-right font-mono text-gray-500">{fmt(r.paidAmount)}</td>
                  <td className={`px-3 py-1.5 text-right font-mono ${r.outstandingAmount > 0 ? 'font-semibold text-red-600' : 'text-gray-300'}`}>{fmt(r.outstandingAmount)}</td>
                  <td className="px-3 py-1.5 text-center">
                    <span className={`inline-block rounded px-2 py-0.5 text-[11px] ${r.isCompleted ? 'bg-green-100 text-green-700' : 'bg-amber-100 text-amber-700'}`}>
                      {r.isCompleted ? 'ชำระครบ' : 'ค้างชำระ'}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
                <td className="px-3 py-2" colSpan={4}>รวม {rows.length} ใบ</td>
                <td className="px-3 py-2 text-right font-mono">{fmt(rows.reduce((s, r) => s + r.netAmount, 0))}</td>
                <td className="px-3 py-2 text-right font-mono">{fmt(rows.reduce((s, r) => s + r.paidAmount, 0))}</td>
                <td className="px-3 py-2 text-right font-mono text-red-600">{fmt(rows.reduce((s, r) => s + r.outstandingAmount, 0))}</td>
                <td className="px-3 py-2" />
              </tr>
            </tfoot>
          </table>
        </Card>
      )}
    </div>
  )
}
