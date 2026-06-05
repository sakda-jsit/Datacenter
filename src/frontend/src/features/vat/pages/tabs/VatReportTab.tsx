import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import ExportMenu from '../../../../shared/components/ui/ExportMenu'
import DataAsOfBanner from '../../../../shared/components/ui/DataAsOfBanner'
import { useVatReport } from '../../hooks/useVat'
import { MONTH_LABEL } from '../../types/vat.types'
import type { ExportSection } from '../../../../shared/utils/exportTable'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  companyId: number
  year: number
}

export default function VatReportTab({ companyId, year }: Props) {
  const { data, isLoading, isError } = useVatReport(companyId, year)

  if (!companyId) {
    return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>
  }

  const hasData = data && data.months.some((m) => m.outputCount > 0 || m.inputCount > 0)

  return (
    <div>
      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {data && !hasData && (
        <Card><StateMessage centered>{`ไม่มีข้อมูลภาษีมูลค่าเพิ่มสำหรับปี ${year} — นำเข้าข้อมูลจาก Express (ISVAT) ที่เมนูนำเข้าข้อมูล`}</StateMessage></Card>
      )}

      {data && hasData && (
        <>
        <DataAsOfBanner dataAsOf={data.dataAsOf} noun="ภาษีมูลค่าเพิ่ม" />
        <Card className="overflow-x-auto">
          <div className="flex items-start justify-between border-b px-4 py-3">
            <div>
              <p className="text-sm font-semibold text-slate-800">รายงานภาษีมูลค่าเพิ่ม (ภ.พ.30) รายเดือน · ปี {year}</p>
              <p className="text-xs text-gray-500">{data.clientName} · ภาษีสุทธิ &gt;0 = ชำระเพิ่ม, &lt;0 = ชำระเกิน/ยกไป</p>
            </div>
            <ExportMenu
              meta={{
                title: `รายงานภาษีมูลค่าเพิ่ม (ภ.พ.30) ปี ${year}`,
                subtitle: data.clientName,
                fileName: `vat-pp30-${data.clientCompanyId}-${year}`,
              }}
              getSections={(): ExportSection[] => [
                {
                  name: `ภ.พ.30 ${year}`,
                  columns: [
                    { key: 'month', header: 'เดือน', value: (r) => MONTH_LABEL[r.month] },
                    { key: 'outputBase', header: 'ยอดขาย', align: 'right' },
                    { key: 'outputVat', header: 'ภาษีขาย', align: 'right' },
                    { key: 'inputBase', header: 'ยอดซื้อ', align: 'right' },
                    { key: 'inputVat', header: 'ภาษีซื้อ', align: 'right' },
                    { key: 'netVat', header: 'ภาษีสุทธิ', align: 'right' },
                  ],
                  rows: data.months,
                },
              ]}
            />
          </div>
          <table className="w-full text-xs">
            <thead className="bg-slate-50 text-gray-600">
              <tr>
                <th className="px-3 py-2 text-left font-medium">เดือนภาษี</th>
                <th className="px-3 py-2 text-right font-medium">ยอดขาย</th>
                <th className="px-3 py-2 text-right font-medium">ภาษีขาย</th>
                <th className="px-3 py-2 text-right font-medium">ยอดซื้อ</th>
                <th className="px-3 py-2 text-right font-medium">ภาษีซื้อ</th>
                <th className="px-3 py-2 text-right font-medium">ภาษีสุทธิ</th>
              </tr>
            </thead>
            <tbody>
              {data.months.map((m) => {
                const empty = m.outputCount === 0 && m.inputCount === 0
                return (
                  <tr key={m.month} className={`border-t border-gray-100 ${empty ? 'text-gray-300' : 'hover:bg-slate-50'}`}>
                    <td className="px-3 py-1.5">{MONTH_LABEL[m.month]}</td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(m.outputBase)}</td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(m.outputVat)}</td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(m.inputBase)}</td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(m.inputVat)}</td>
                    <td className={`px-3 py-1.5 text-right font-mono font-semibold ${
                      empty ? '' : m.netVat >= 0 ? 'text-red-600' : 'text-green-600'
                    }`}>
                      {fmt(m.netVat)}
                    </td>
                  </tr>
                )
              })}
            </tbody>
            <tfoot>
              <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
                <td className="px-3 py-2 text-left">รวมทั้งปี</td>
                <td className="px-3 py-2 text-right font-mono">{fmt(data.totalOutputBase)}</td>
                <td className="px-3 py-2 text-right font-mono">{fmt(data.totalOutputVat)}</td>
                <td className="px-3 py-2 text-right font-mono">{fmt(data.totalInputBase)}</td>
                <td className="px-3 py-2 text-right font-mono">{fmt(data.totalInputVat)}</td>
                <td className={`px-3 py-2 text-right font-mono ${data.totalNetVat >= 0 ? 'text-red-600' : 'text-green-600'}`}>
                  {fmt(data.totalNetVat)}
                </td>
              </tr>
            </tfoot>
          </table>
        </Card>
        </>
      )}
    </div>
  )
}
