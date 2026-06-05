import { useState } from 'react'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import ExportMenu from '../../../../shared/components/ui/ExportMenu'
import { useVatEntries } from '../../hooks/useVat'
import { MONTH_LABEL, VAT_TYPE_LABEL } from '../../types/vat.types'
import type { ExportSection } from '../../../../shared/utils/exportTable'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  companyId: number
  year: number
}

export default function VatEntriesTab({ companyId, year }: Props) {
  const [month, setMonth] = useState(0)
  const [type, setType] = useState(0) // 0 = ทั้งหมด
  const { data, isLoading, isError } = useVatEntries(companyId, year, month, type || undefined)

  if (!companyId) {
    return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>
  }

  const rows = data ?? []

  return (
    <div>
      <Card className="mb-4 p-4">
        <div className="flex flex-wrap items-end gap-3">
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">เดือนภาษี</label>
            <select value={month} onChange={(e) => setMonth(Number(e.target.value))} className="rounded border border-gray-300 px-3 py-2 text-sm">
              <option value={0}>ทั้งปี</option>
              {Array.from({ length: 12 }, (_, i) => i + 1).map((m) => (
                <option key={m} value={m}>{MONTH_LABEL[m]}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ประเภท</label>
            <select value={type} onChange={(e) => setType(Number(e.target.value))} className="rounded border border-gray-300 px-3 py-2 text-sm">
              <option value={0}>ทั้งหมด</option>
              <option value={1}>ภาษีขาย</option>
              <option value={2}>ภาษีซื้อ</option>
            </select>
          </div>
        </div>
      </Card>

      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {data && rows.length === 0 && (
        <Card><StateMessage centered>ไม่มีรายการตามเงื่อนไขที่เลือก</StateMessage></Card>
      )}

      {rows.length > 0 && (
        <Card className="overflow-x-auto">
          <div className="flex items-start justify-between border-b px-4 py-3">
            <p className="text-sm font-semibold text-slate-800">
              รายละเอียดภาษีซื้อ/ขาย · {month === 0 ? `ทั้งปี ${year}` : `${MONTH_LABEL[month]} ${year}`} ({rows.length} รายการ)
            </p>
            <ExportMenu
              meta={{
                title: `รายละเอียดภาษีซื้อ-ขาย ปี ${year}${month ? ' เดือน ' + MONTH_LABEL[month] : ''}`,
                fileName: `vat-entries-${companyId}-${year}${month ? '-' + month : ''}`,
              }}
              getSections={(): ExportSection[] => [
                {
                  name: 'รายการภาษี',
                  columns: [
                    { key: 'taxPeriod', header: 'เดือนภาษี', value: (r) => String(r.taxPeriod).slice(0, 7) },
                    { key: 'vatType', header: 'ประเภท', value: (r) => VAT_TYPE_LABEL[r.vatType] },
                    { key: 'documentDate', header: 'วันที่', value: (r) => (r.documentDate ? String(r.documentDate).slice(0, 10) : '') },
                    { key: 'documentNo', header: 'เลขที่เอกสาร' },
                    { key: 'description', header: 'คู่ค้า/รายละเอียด' },
                    { key: 'counterpartyTaxId', header: 'เลขผู้เสียภาษี' },
                    { key: 'baseAmount', header: 'มูลค่า', align: 'right' },
                    { key: 'vatAmount', header: 'ภาษี', align: 'right' },
                  ],
                  rows,
                },
              ]}
            />
          </div>
          <table className="w-full text-xs">
            <thead className="bg-slate-50 text-gray-600">
              <tr>
                <th className="px-3 py-2 text-left font-medium">เดือนภาษี</th>
                <th className="px-3 py-2 text-left font-medium">ประเภท</th>
                <th className="px-3 py-2 text-left font-medium">วันที่</th>
                <th className="px-3 py-2 text-left font-medium">เลขที่เอกสาร</th>
                <th className="px-3 py-2 text-left font-medium">คู่ค้า / รายละเอียด</th>
                <th className="px-3 py-2 text-right font-medium">มูลค่า</th>
                <th className="px-3 py-2 text-right font-medium">ภาษี</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => (
                <tr key={r.id} className="border-t border-gray-100 hover:bg-slate-50">
                  <td className="px-3 py-1.5 font-mono text-gray-500">{String(r.taxPeriod).slice(0, 7)}</td>
                  <td className="px-3 py-1.5">
                    <span className={r.vatType === 1 ? 'text-sky-700' : 'text-amber-700'}>{VAT_TYPE_LABEL[r.vatType]}</span>
                  </td>
                  <td className="px-3 py-1.5 font-mono text-gray-500">{r.documentDate ? String(r.documentDate).slice(0, 10) : '—'}</td>
                  <td className="px-3 py-1.5 font-mono">{r.documentNo}</td>
                  <td className="px-3 py-1.5">
                    {r.description || '—'}
                    {r.counterpartyTaxId && <span className="ml-1 text-[10px] text-gray-400">{r.counterpartyTaxId}</span>}
                    {r.isLate && <span className="ml-1 text-[10px] text-red-500">(ล่าช้า)</span>}
                  </td>
                  <td className="px-3 py-1.5 text-right font-mono">{fmt(r.baseAmount)}</td>
                  <td className="px-3 py-1.5 text-right font-mono">{fmt(r.vatAmount)}</td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
                <td className="px-3 py-2" colSpan={5}>รวม {rows.length} รายการ</td>
                <td className="px-3 py-2 text-right font-mono">{fmt(rows.reduce((s, r) => s + r.baseAmount, 0))}</td>
                <td className="px-3 py-2 text-right font-mono">{fmt(rows.reduce((s, r) => s + r.vatAmount, 0))}</td>
              </tr>
            </tfoot>
          </table>
        </Card>
      )}
    </div>
  )
}
