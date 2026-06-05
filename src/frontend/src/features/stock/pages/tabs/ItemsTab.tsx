import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import ExportMenu from '../../../../shared/components/ui/ExportMenu'
import { useStockItems } from '../../hooks/useStock'
import type { ExportSection } from '../../../../shared/utils/exportTable'

function fmtQty(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 0, maximumFractionDigits: 4 })
}
function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  companyId: number
}

export default function ItemsTab({ companyId }: Props) {
  const { data, isLoading, isError } = useStockItems(companyId)

  if (!companyId) return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>

  const rows = data ?? []

  return (
    <div>
      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {data && rows.length === 0 && <Card><StateMessage centered>ไม่มีสินค้าคงเหลือ</StateMessage></Card>}

      {rows.length > 0 && (
        <Card className="overflow-x-auto">
          <div className="flex items-start justify-between border-b px-4 py-3">
            <p className="text-sm font-semibold text-slate-800">รายการสินค้าคงเหลือ ({rows.length} รายการ)</p>
            <ExportMenu
              meta={{ title: 'รายการสินค้าคงเหลือ', fileName: `stock-items-${companyId}` }}
              getSections={(): ExportSection[] => [{
                name: 'สินค้าคงเหลือ',
                columns: [
                  { key: 'stockCode', header: 'รหัส' },
                  { key: 'name', header: 'ชื่อสินค้า' },
                  { key: 'groupCode', header: 'กลุ่ม' },
                  { key: 'unit', header: 'หน่วย' },
                  { key: 'onHandQty', header: 'คงเหลือ', align: 'right' },
                  { key: 'unitCost', header: 'ต้นทุน/หน่วย', align: 'right' },
                  { key: 'stockValue', header: 'มูลค่า', align: 'right' },
                ],
                rows,
              }]}
            />
          </div>
          <table className="w-full text-xs">
            <thead className="bg-slate-50 text-gray-600">
              <tr>
                <th className="px-3 py-2 text-left font-medium">รหัส</th>
                <th className="px-3 py-2 text-left font-medium">ชื่อสินค้า</th>
                <th className="px-3 py-2 text-left font-medium">กลุ่ม</th>
                <th className="px-3 py-2 text-right font-medium">คงเหลือ</th>
                <th className="px-3 py-2 text-left font-medium">หน่วย</th>
                <th className="px-3 py-2 text-right font-medium">ต้นทุน/หน่วย</th>
                <th className="px-3 py-2 text-right font-medium">มูลค่า</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => (
                <tr key={r.id} className="border-t border-gray-100 hover:bg-slate-50">
                  <td className="px-3 py-1.5 font-mono text-gray-500">{r.stockCode}</td>
                  <td className="px-3 py-1.5">{r.name}</td>
                  <td className="px-3 py-1.5 text-gray-600">{r.groupCode || '—'}</td>
                  <td className={`px-3 py-1.5 text-right font-mono ${r.onHandQty < 0 ? 'text-red-600' : ''}`}>{fmtQty(r.onHandQty)}</td>
                  <td className="px-3 py-1.5 text-gray-500">{r.unit || '—'}</td>
                  <td className="px-3 py-1.5 text-right font-mono text-gray-500">{fmt(r.unitCost)}</td>
                  <td className="px-3 py-1.5 text-right font-mono font-semibold">{fmt(r.stockValue)}</td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
                <td className="px-3 py-2" colSpan={6}>รวมมูลค่า</td>
                <td className="px-3 py-2 text-right font-mono">{fmt(rows.reduce((s, r) => s + r.stockValue, 0))}</td>
              </tr>
            </tfoot>
          </table>
        </Card>
      )}
    </div>
  )
}
