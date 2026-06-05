import { useState } from 'react'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import ExportMenu from '../../../../shared/components/ui/ExportMenu'
import { useCustomers } from '../../hooks/useAr'
import type { ExportSection } from '../../../../shared/utils/exportTable'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  companyId: number
}

export default function CustomersTab({ companyId }: Props) {
  const [includeInactive, setIncludeInactive] = useState(false)
  const { data, isLoading, isError } = useCustomers(companyId, includeInactive)

  if (!companyId) return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>

  const rows = data ?? []

  return (
    <div>
      <Card className="mb-4 p-4">
        <label className="flex items-center gap-2 text-sm text-gray-700">
          <input type="checkbox" checked={includeInactive} onChange={(e) => setIncludeInactive(e.target.checked)} className="rounded" />
          แสดงลูกค้าที่ปิดใช้งานด้วย
        </label>
      </Card>

      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {data && rows.length === 0 && <Card><StateMessage centered>ไม่มีลูกค้า — นำเข้าข้อมูลจาก Express (ARMAS) ที่เมนูนำเข้าข้อมูล</StateMessage></Card>}

      {rows.length > 0 && (
        <Card className="overflow-x-auto">
          <div className="flex items-start justify-between border-b px-4 py-3">
            <p className="text-sm font-semibold text-slate-800">ลูกค้า ({rows.length} ราย)</p>
            <ExportMenu
              meta={{ title: 'รายชื่อลูกค้า', fileName: `ar-customers-${companyId}` }}
              getSections={(): ExportSection[] => [{
                name: 'ลูกค้า',
                columns: [
                  { key: 'customerCode', header: 'รหัส' },
                  { key: 'name', header: 'ชื่อลูกค้า' },
                  { key: 'taxId', header: 'เลขผู้เสียภาษี' },
                  { key: 'paymentCondition', header: 'เครดิตเทอม' },
                  { key: 'email', header: 'อีเมล' },
                  { key: 'outstandingAmount', header: 'ยอดค้าง', align: 'right' },
                ],
                rows,
              }]}
            />
          </div>
          <table className="w-full text-xs">
            <thead className="bg-slate-50 text-gray-600">
              <tr>
                <th className="px-3 py-2 text-left font-medium">รหัส</th>
                <th className="px-3 py-2 text-left font-medium">ชื่อลูกค้า</th>
                <th className="px-3 py-2 text-left font-medium">เลขผู้เสียภาษี</th>
                <th className="px-3 py-2 text-left font-medium">เครดิตเทอม</th>
                <th className="px-3 py-2 text-left font-medium">อีเมล</th>
                <th className="px-3 py-2 text-right font-medium">ยอดค้าง</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => (
                <tr key={r.id} className="border-t border-gray-100 hover:bg-slate-50">
                  <td className="px-3 py-1.5 font-mono text-gray-500">{r.customerCode}</td>
                  <td className="px-3 py-1.5">
                    {r.name}
                    {!r.isActive && <span className="ml-1 text-[10px] text-gray-400">(ปิดใช้งาน)</span>}
                  </td>
                  <td className="px-3 py-1.5 font-mono text-gray-500">{r.taxId || '—'}</td>
                  <td className="px-3 py-1.5 text-gray-600">{r.paymentCondition || (r.paymentTermDays ? `${r.paymentTermDays} วัน` : '—')}</td>
                  <td className="px-3 py-1.5 text-gray-600">{r.email || '—'}</td>
                  <td className={`px-3 py-1.5 text-right font-mono ${r.outstandingAmount > 0 ? 'font-semibold text-red-600' : 'text-gray-300'}`}>
                    {fmt(r.outstandingAmount)}
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
                <td className="px-3 py-2" colSpan={5}>รวมยอดค้าง</td>
                <td className="px-3 py-2 text-right font-mono text-red-600">{fmt(rows.reduce((s, r) => s + r.outstandingAmount, 0))}</td>
              </tr>
            </tfoot>
          </table>
        </Card>
      )}
    </div>
  )
}
