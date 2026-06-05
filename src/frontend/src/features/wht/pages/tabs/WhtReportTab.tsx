import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import ExportMenu from '../../../../shared/components/ui/ExportMenu'
import { useWhtReport } from '../../hooks/useWht'
import { MONTH_LABEL } from '../../types/wht.types'
import type { ExportSection } from '../../../../shared/utils/exportTable'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  companyId: number
  year: number
}

export default function WhtReportTab({ companyId, year }: Props) {
  const { data, isLoading, isError } = useWhtReport(companyId, year)

  if (!companyId) {
    return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>
  }

  const hasData = data && data.months.some((m) => m.pnd3Count > 0 || m.pnd53Count > 0)

  return (
    <div>
      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {data && !hasData && (
        <Card><StateMessage centered>{`ไม่มีข้อมูลภาษีหัก ณ ที่จ่ายสำหรับปี ${year} — นำเข้าข้อมูลจาก Express (ISTAX) ที่เมนูนำเข้าข้อมูล`}</StateMessage></Card>
      )}

      {data && hasData && (
        <Card className="overflow-x-auto">
          <div className="flex items-start justify-between border-b px-4 py-3">
            <div>
              <p className="text-sm font-semibold text-slate-800">รายงานภาษีหัก ณ ที่จ่าย (ภ.ง.ด.3 / ภ.ง.ด.53) รายเดือน · ปี {year}</p>
              <p className="text-xs text-gray-500">{data.clientName} · ภ.ง.ด.3 = บุคคลธรรมดา, ภ.ง.ด.53 = นิติบุคคล</p>
            </div>
            <ExportMenu
              meta={{
                title: `รายงานภาษีหัก ณ ที่จ่าย (ภ.ง.ด.3/53) ปี ${year}`,
                subtitle: data.clientName,
                fileName: `wht-pnd3-53-${data.clientCompanyId}-${year}`,
              }}
              getSections={(): ExportSection[] => [
                {
                  name: `ภ.ง.ด.3-53 ${year}`,
                  columns: [
                    { key: 'month', header: 'เดือน', value: (r) => MONTH_LABEL[r.month] },
                    { key: 'pnd3Base', header: 'ภ.ง.ด.3 ฐาน', align: 'right' },
                    { key: 'pnd3Tax', header: 'ภ.ง.ด.3 ภาษี', align: 'right' },
                    { key: 'pnd53Base', header: 'ภ.ง.ด.53 ฐาน', align: 'right' },
                    { key: 'pnd53Tax', header: 'ภ.ง.ด.53 ภาษี', align: 'right' },
                    { key: 'totalTax', header: 'ภาษีหักรวม', align: 'right' },
                  ],
                  rows: data.months,
                },
              ]}
            />
          </div>
          <table className="w-full text-xs">
            <thead className="bg-slate-50 text-gray-600">
              <tr>
                <th rowSpan={2} className="px-3 py-2 text-left align-bottom font-medium">เดือนภาษี</th>
                <th colSpan={2} className="px-3 py-1.5 text-center font-medium border-b border-slate-200">ภ.ง.ด.3 (บุคคลธรรมดา)</th>
                <th colSpan={2} className="px-3 py-1.5 text-center font-medium border-b border-slate-200">ภ.ง.ด.53 (นิติบุคคล)</th>
                <th rowSpan={2} className="px-3 py-2 text-right align-bottom font-medium">ภาษีหักรวม</th>
              </tr>
              <tr>
                <th className="px-3 py-1.5 text-right font-medium">ฐานเงินได้</th>
                <th className="px-3 py-1.5 text-right font-medium">ภาษีหัก</th>
                <th className="px-3 py-1.5 text-right font-medium">ฐานเงินได้</th>
                <th className="px-3 py-1.5 text-right font-medium">ภาษีหัก</th>
              </tr>
            </thead>
            <tbody>
              {data.months.map((m) => {
                const empty = m.pnd3Count === 0 && m.pnd53Count === 0
                return (
                  <tr key={m.month} className={`border-t border-gray-100 ${empty ? 'text-gray-300' : 'hover:bg-slate-50'}`}>
                    <td className="px-3 py-1.5">{MONTH_LABEL[m.month]}</td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(m.pnd3Base)}</td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(m.pnd3Tax)}</td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(m.pnd53Base)}</td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(m.pnd53Tax)}</td>
                    <td className={`px-3 py-1.5 text-right font-mono font-semibold ${empty ? '' : 'text-slate-800'}`}>{fmt(m.totalTax)}</td>
                  </tr>
                )
              })}
            </tbody>
            <tfoot>
              <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
                <td className="px-3 py-2 text-left">รวมทั้งปี</td>
                <td className="px-3 py-2 text-right font-mono">{fmt(data.totalPnd3Base)}</td>
                <td className="px-3 py-2 text-right font-mono">{fmt(data.totalPnd3Tax)}</td>
                <td className="px-3 py-2 text-right font-mono">{fmt(data.totalPnd53Base)}</td>
                <td className="px-3 py-2 text-right font-mono">{fmt(data.totalPnd53Tax)}</td>
                <td className="px-3 py-2 text-right font-mono">{fmt(data.totalTax)}</td>
              </tr>
            </tfoot>
          </table>
        </Card>
      )}
    </div>
  )
}
