import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import ExportMenu from '../../../../shared/components/ui/ExportMenu'
import type { EquityChangesDto } from '../../types/financialStatement.types'
import type { ExportSection } from '../../../../shared/utils/exportTable'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  data?: EquityChangesDto
  isLoading: boolean
  isError: boolean
  queried: boolean
}

export default function EquityChangesTab({ data, isLoading, isError, queried }: Props) {
  if (isLoading) return <StateMessage>กำลังคำนวณ...</StateMessage>
  if (isError) return <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>
  if (!queried || !data) return (
    <Card><StateMessage centered>เลือกบริษัทและปีบัญชี แล้วกด "แสดงรายงาน"</StateMessage></Card>
  )

  const exportSections = (): ExportSection[] => [{
    name: 'การเปลี่ยนแปลงส่วนผู้ถือหุ้น',
    columns: [
      { key: 'name', header: 'องค์ประกอบ' },
      { key: 'opening', header: 'ยอดต้นปี', align: 'right' },
      { key: 'netProfit', header: 'กำไรสุทธิ', align: 'right' },
      { key: 'otherChange', header: 'เปลี่ยนแปลงอื่น', align: 'right' },
      { key: 'closing', header: 'ยอดปลายปี', align: 'right' },
    ],
    rows: data.components,
  }]

  return (
    <div>
      <Card className="mb-4 flex items-start justify-between px-6 py-4">
        <div>
          <p className="text-lg font-semibold text-slate-800">{data.clientName}</p>
          <p className="text-sm text-gray-500">งบแสดงการเปลี่ยนแปลงส่วนของผู้ถือหุ้น สำหรับปีสิ้นสุด 31 ธันวาคม {data.fiscalYear}</p>
          {data.tiesToBalanceSheet
            ? <p className="mt-1 text-xs text-green-600">✓ ยอดปลายปีตรงกับส่วนของผู้ถือหุ้นในงบแสดงฐานะการเงิน</p>
            : <p className="mt-1 text-xs font-medium text-red-500">⚠ ยอดปลายปีไม่ตรงงบดุล (ผลต่าง {fmt(data.totalClosing - data.balanceSheetEquity)})</p>}
        </div>
        <ExportMenu
          meta={{ title: `งบแสดงการเปลี่ยนแปลงส่วนของผู้ถือหุ้น ปี ${data.fiscalYear}`, subtitle: data.clientName, fileName: `equity-changes-${data.clientCompanyId}-${data.fiscalYear}` }}
          getSections={exportSections}
        />
      </Card>

      <Card className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="bg-slate-100 text-gray-600">
            <tr>
              <th className="px-4 py-2.5 text-left font-medium">รายการ</th>
              {data.components.map((c) => (
                <th key={c.refCode} className="px-4 py-2.5 text-right font-medium">{c.name}</th>
              ))}
              <th className="px-4 py-2.5 text-right font-medium">รวม</th>
            </tr>
          </thead>
          <tbody>
            <Row label="ยอดคงเหลือต้นปี" vals={data.components.map((c) => c.opening)} total={data.totalOpening} />
            <Row label="กำไร (ขาดทุน) สุทธิสำหรับปี" vals={data.components.map((c) => c.netProfit)} total={data.totalNetProfit} muted />
            <Row label="เพิ่มทุน / เงินปันผล / รายการอื่น" vals={data.components.map((c) => c.otherChange)} total={data.totalOtherChange} muted />
          </tbody>
          <tfoot>
            <tr className="border-t-2 border-slate-300 bg-slate-50 font-bold">
              <td className="px-4 py-3 text-slate-800">ยอดคงเหลือปลายปี</td>
              {data.components.map((c) => (
                <td key={c.refCode} className="px-4 py-3 text-right font-mono text-slate-800">{fmt(c.closing)}</td>
              ))}
              <td className="px-4 py-3 text-right font-mono text-slate-800">{fmt(data.totalClosing)}</td>
            </tr>
          </tfoot>
        </table>
      </Card>
    </div>
  )
}

function Row({ label, vals, total, muted }: { label: string; vals: number[]; total: number; muted?: boolean }) {
  return (
    <tr className="border-b border-gray-100">
      <td className={`px-4 py-2.5 ${muted ? 'pl-8 text-gray-600' : 'text-slate-800'}`}>{label}</td>
      {vals.map((v, i) => (
        <td key={i} className="px-4 py-2.5 text-right font-mono text-gray-700">{v !== 0 ? fmt(v) : '—'}</td>
      ))}
      <td className="px-4 py-2.5 text-right font-mono text-gray-700">{total !== 0 ? fmt(total) : '—'}</td>
    </tr>
  )
}
