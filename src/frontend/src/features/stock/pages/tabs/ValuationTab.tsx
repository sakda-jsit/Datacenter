import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import ExportMenu from '../../../../shared/components/ui/ExportMenu'
import { useStockValuation } from '../../hooks/useStock'
import type { ExportSection } from '../../../../shared/utils/exportTable'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function fmtDateTime(s?: string) {
  if (!s) return null
  const iso = /[zZ]|[+-]\d\d:?\d\d$/.test(s) ? s : s + 'Z' // CreatedAt เป็น UTC
  return new Date(iso).toLocaleString('th-TH', { dateStyle: 'medium', timeStyle: 'short' })
}

interface Props {
  companyId: number
  fiscalYear: number
}

export default function ValuationTab({ companyId, fiscalYear }: Props) {
  const { data, isLoading, isError } = useStockValuation(companyId, fiscalYear)

  if (!companyId) return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>

  return (
    <div>
      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {data && data.items.length === 0 && (
        <Card><StateMessage centered>ไม่มีสินค้าคงเหลือ — นำเข้าข้อมูลจาก Express (STMAS) ที่เมนูนำเข้าข้อมูล</StateMessage></Card>
      )}

      {data && data.items.length > 0 && (
        <>
          {/* ความสดของข้อมูล (snapshot ตอน import — ไม่ใช่ real-time) */}
          <div className="mb-3 flex flex-wrap items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 px-4 py-2 text-xs text-amber-800">
            <span>📌 ข้อมูลสินค้าคงคลังเป็น snapshot ณ ตอนนำเข้า — </span>
            <span className="font-semibold">
              {data.dataAsOf ? `นำเข้าล่าสุด ${fmtDateTime(data.dataAsOf)}` : 'ยังไม่มีข้อมูลนำเข้า'}
            </span>
            <a href="/import" className="ml-auto rounded border border-amber-300 bg-white px-2 py-0.5 text-amber-700 no-underline hover:bg-amber-100">
              นำเข้าข้อมูลใหม่
            </a>
          </div>

          {/* KPI cards */}
          <div className="mb-4 grid grid-cols-1 gap-3 sm:grid-cols-3">
            <Card className="p-4">
              <p className="text-xs text-gray-500">มูลค่าสินค้าคงเหลือ (STMAS)</p>
              <p className="mt-1 text-xl font-bold text-slate-800">{fmt(data.totalStockValue)}</p>
              <p className="text-[11px] text-gray-400">{data.items.length} รายการ</p>
            </Card>
            <Card className="p-4">
              <p className="text-xs text-gray-500">ยอดบัญชีสินค้าคงเหลือ (GL) ณ สิ้นปี {fiscalYear}</p>
              <p className="mt-1 text-xl font-bold text-slate-800">{data.hasGlAccounts ? fmt(data.totalGlBalance) : '—'}</p>
              <p className="text-[11px] text-gray-400">{data.hasGlAccounts ? `${data.glAccounts.length} บัญชี` : 'ไม่พบบัญชีสินค้าคงเหลือในผังบัญชี'}</p>
            </Card>
            <Card className="p-4">
              <p className="text-xs text-gray-500">ผลต่าง (สินค้า − GL)</p>
              <p className={`mt-1 text-xl font-bold ${Math.abs(data.difference) < 0.005 ? 'text-green-600' : 'text-amber-600'}`}>{fmt(data.difference)}</p>
              <p className="text-[11px] text-gray-400">{Math.abs(data.difference) < 0.005 ? 'ตรงกัน' : 'ให้บันทึกปรับปรุงเอง'}</p>
            </Card>
          </div>

          {/* group summary */}
          <Card className="mb-4 overflow-x-auto">
            <div className="flex items-start justify-between border-b px-4 py-3">
              <div>
                <p className="text-sm font-semibold text-slate-800">สรุปมูลค่าสินค้าคงเหลือตามกลุ่ม</p>
                <p className="text-xs text-gray-500">{data.clientName}</p>
              </div>
              <ExportMenu
                meta={{ title: `มูลค่าสินค้าคงเหลือ ปี ${fiscalYear}`, subtitle: data.clientName, fileName: `stock-valuation-${companyId}-${fiscalYear}` }}
                getSections={(): ExportSection[] => [
                  { name: 'สรุปตามกลุ่ม', columns: [
                      { key: 'groupCode', header: 'กลุ่ม' },
                      { key: 'count', header: 'จำนวนรายการ', align: 'right' },
                      { key: 'totalValue', header: 'มูลค่า', align: 'right' },
                    ], rows: data.groups },
                  { name: 'รายการสินค้า', columns: [
                      { key: 'stockCode', header: 'รหัส' },
                      { key: 'name', header: 'ชื่อสินค้า' },
                      { key: 'onHandQty', header: 'คงเหลือ', align: 'right' },
                      { key: 'unitCost', header: 'ต้นทุน/หน่วย', align: 'right' },
                      { key: 'stockValue', header: 'มูลค่า', align: 'right' },
                    ], rows: data.items },
                ]}
              />
            </div>
            <table className="w-full text-xs">
              <thead className="bg-slate-50 text-gray-600">
                <tr>
                  <th className="px-3 py-2 text-left font-medium">กลุ่มสินค้า</th>
                  <th className="px-3 py-2 text-right font-medium">จำนวนรายการ</th>
                  <th className="px-3 py-2 text-right font-medium">มูลค่า</th>
                </tr>
              </thead>
              <tbody>
                {data.groups.map((g) => (
                  <tr key={g.groupCode} className="border-t border-gray-100 hover:bg-slate-50">
                    <td className="px-3 py-1.5">{g.groupCode}</td>
                    <td className="px-3 py-1.5 text-right text-gray-500">{g.count}</td>
                    <td className="px-3 py-1.5 text-right font-mono font-semibold">{fmt(g.totalValue)}</td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
                  <td className="px-3 py-2">รวม</td>
                  <td className="px-3 py-2 text-right">{data.items.length}</td>
                  <td className="px-3 py-2 text-right font-mono">{fmt(data.totalStockValue)}</td>
                </tr>
              </tfoot>
            </table>
          </Card>

          {/* GL comparison */}
          <Card className="overflow-x-auto">
            <div className="border-b px-4 py-3">
              <p className="text-sm font-semibold text-slate-800">เทียบบัญชีสินค้าคงเหลือใน GL (FG ↔ TB) ปี {fiscalYear}</p>
              <p className="text-xs text-gray-500">
                {data.hasGlAccounts
                  ? 'มูลค่าสินค้า (STMAS) เทียบยอดบัญชีสินค้าคงเหลือใน GL — ผลต่างให้บันทึกปรับปรุงเอง'
                  : 'ไม่พบบัญชีสินค้าคงเหลือในผังบัญชี — แสดงมูลค่าสินค้าทั้งหมดเป็นผลต่าง'}
              </p>
            </div>
            <table className="w-full text-xs">
              <thead className="bg-slate-50 text-gray-600">
                <tr>
                  <th className="px-3 py-2 text-left font-medium">บัญชี</th>
                  <th className="px-3 py-2 text-right font-medium">ยอดตาม GL</th>
                </tr>
              </thead>
              <tbody>
                {data.glAccounts.map((g) => (
                  <tr key={g.accountId} className="border-t border-gray-100">
                    <td className="px-3 py-1.5"><span className="font-mono text-gray-500">{g.accountCode}</span> {g.accountName}</td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(g.glBalance)}</td>
                  </tr>
                ))}
                <tr className="border-t border-gray-100">
                  <td className="px-3 py-1.5 text-gray-700">มูลค่าสินค้าคงเหลือ (STMAS)</td>
                  <td className="px-3 py-1.5 text-right font-mono">{fmt(data.totalStockValue)}</td>
                </tr>
              </tbody>
              <tfoot>
                <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
                  <td className="px-3 py-2">ผลต่าง (สินค้า − GL)</td>
                  <td className={`px-3 py-2 text-right font-mono ${Math.abs(data.difference) < 0.005 ? 'text-green-600' : 'text-amber-600'}`}>{fmt(data.difference)}</td>
                </tr>
              </tfoot>
            </table>
          </Card>
        </>
      )}
    </div>
  )
}
