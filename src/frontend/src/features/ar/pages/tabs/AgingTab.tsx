import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import ExportMenu from '../../../../shared/components/ui/ExportMenu'
import { useArAging } from '../../hooks/useAr'
import type { ExportSection } from '../../../../shared/utils/exportTable'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  companyId: number
  asOf: string
}

export default function AgingTab({ companyId, asOf }: Props) {
  const { data, isLoading, isError } = useArAging(companyId, asOf)

  if (!companyId) return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>

  return (
    <div>
      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {data && data.rows.length === 0 && (
        <Card><StateMessage centered>ไม่มียอดลูกหนี้คงค้าง ณ วันที่เลือก</StateMessage></Card>
      )}

      {data && data.rows.length > 0 && (
        <Card className="overflow-x-auto">
          <div className="flex items-start justify-between border-b px-4 py-3">
            <div>
              <p className="text-sm font-semibold text-slate-800">รายงานอายุหนี้ลูกหนี้การค้า ณ {data.asOfDate.slice(0, 10)}</p>
              <p className="text-xs text-gray-500">{data.clientName} · แบ่งช่วงตามวันเกินกำหนดชำระ</p>
            </div>
            <ExportMenu
              meta={{ title: `รายงานอายุหนี้ลูกหนี้ ณ ${data.asOfDate.slice(0, 10)}`, subtitle: data.clientName, fileName: `ar-aging-${companyId}-${data.asOfDate.slice(0, 10)}` }}
              getSections={(): ExportSection[] => [{
                name: 'อายุหนี้',
                columns: [
                  { key: 'customerName', header: 'ลูกค้า' },
                  { key: 'notDue', header: 'ยังไม่ถึงกำหนด', align: 'right' },
                  { key: 'days1To30', header: '1-30 วัน', align: 'right' },
                  { key: 'days31To60', header: '31-60 วัน', align: 'right' },
                  { key: 'days61To90', header: '61-90 วัน', align: 'right' },
                  { key: 'days90Plus', header: 'เกิน 90 วัน', align: 'right' },
                  { key: 'total', header: 'รวม', align: 'right' },
                ],
                rows: data.rows,
              }]}
            />
          </div>
          <table className="w-full text-xs">
            <thead className="bg-slate-50 text-gray-600">
              <tr>
                <th className="px-3 py-2 text-left font-medium">ลูกค้า</th>
                <th className="px-3 py-2 text-right font-medium">ยังไม่ถึงกำหนด</th>
                <th className="px-3 py-2 text-right font-medium">1-30 วัน</th>
                <th className="px-3 py-2 text-right font-medium">31-60 วัน</th>
                <th className="px-3 py-2 text-right font-medium">61-90 วัน</th>
                <th className="px-3 py-2 text-right font-medium">เกิน 90 วัน</th>
                <th className="px-3 py-2 text-right font-medium">รวม</th>
              </tr>
            </thead>
            <tbody>
              {data.rows.map((r) => (
                <tr key={r.customerCode} className="border-t border-gray-100 hover:bg-slate-50">
                  <td className="px-3 py-1.5">
                    {r.customerName}
                    <span className="block font-mono text-[10px] text-gray-400">{r.customerCode}</span>
                  </td>
                  <td className="px-3 py-1.5 text-right font-mono text-gray-500">{fmt(r.notDue)}</td>
                  <td className="px-3 py-1.5 text-right font-mono">{fmt(r.days1To30)}</td>
                  <td className="px-3 py-1.5 text-right font-mono">{fmt(r.days31To60)}</td>
                  <td className="px-3 py-1.5 text-right font-mono text-amber-700">{fmt(r.days61To90)}</td>
                  <td className="px-3 py-1.5 text-right font-mono text-red-600">{fmt(r.days90Plus)}</td>
                  <td className="px-3 py-1.5 text-right font-mono font-semibold">{fmt(r.total)}</td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
                <td className="px-3 py-2 text-left">รวมทั้งหมด</td>
                <td className="px-3 py-2 text-right font-mono">{fmt(data.totalNotDue)}</td>
                <td className="px-3 py-2 text-right font-mono">{fmt(data.totalDays1To30)}</td>
                <td className="px-3 py-2 text-right font-mono">{fmt(data.totalDays31To60)}</td>
                <td className="px-3 py-2 text-right font-mono text-amber-700">{fmt(data.totalDays61To90)}</td>
                <td className="px-3 py-2 text-right font-mono text-red-600">{fmt(data.totalDays90Plus)}</td>
                <td className="px-3 py-2 text-right font-mono">{fmt(data.grandTotal)}</td>
              </tr>
            </tfoot>
          </table>
        </Card>
      )}
    </div>
  )
}
