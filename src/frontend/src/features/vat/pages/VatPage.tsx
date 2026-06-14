import { useEffect, useState } from 'react'
import PageHeader from '../../../shared/components/ui/PageHeader'
import Tabs from '../../../shared/components/ui/Tabs'
import Card from '../../../shared/components/ui/Card'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import { useVatYears } from '../hooks/useVat'
import VatReportTab from './tabs/VatReportTab'
import VatEntriesTab from './tabs/VatEntriesTab'
import Pp30FilingTab from './tabs/Pp30FilingTab'

type Tab = 'report' | 'entries' | 'filing'

const TABS: { key: Tab; label: string }[] = [
  { key: 'report', label: 'ภ.พ.30 รายเดือน' },
  { key: 'filing', label: 'ใบกรอก ภ.พ.30 (e-Filing)' },
  { key: 'entries', label: 'รายละเอียดภาษีซื้อ/ขาย' },
]

export default function VatPage() {
  const currentYear = new Date().getFullYear()
  const { companyId } = useCurrentCompany()
  const [tab, setTab] = useState<Tab>('report')
  const [year, setYear] = useState(currentYear)
  const { data: years } = useVatYears(companyId)

  // เลือกปีล่าสุดที่มีข้อมูลโดยอัตโนมัติ (ถ้าปีปัจจุบันไม่มีข้อมูล)
  useEffect(() => {
    if (years && years.length > 0 && !years.includes(year)) {
      setYear(years[0])
    }
  }, [years]) // eslint-disable-line react-hooks/exhaustive-deps

  return (
    <div>
      <PageHeader
        title="ภาษีมูลค่าเพิ่ม (ภ.พ.30)"
        description="รายงานภาษีซื้อ/ขายรายเดือนจาก Express (ISVAT) — สรุปยอด ภ.พ.30 และรายละเอียดรายใบกำกับภาษี"
      />

      <Tabs items={TABS} activeKey={tab} onChange={setTab} />

      <Card className="mb-5 p-4">
        <div className="flex flex-wrap items-end gap-3">
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ปีภาษี (AD)</label>
            {years && years.length > 0 ? (
              <select
                value={year}
                onChange={(e) => setYear(Number(e.target.value))}
                className="w-28 rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
              >
                {years.includes(year) ? null : <option value={year}>{year}</option>}
                {years.map((y) => (
                  <option key={y} value={y}>{y}</option>
                ))}
              </select>
            ) : (
              <input
                type="number" value={year} min={2000} max={2100}
                onChange={(e) => setYear(Number(e.target.value))}
                className="w-28 rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
              />
            )}
          </div>
          <p className="pb-2 text-xs text-gray-400">
            งวดภาษีเป็นรายเดือนตามปฏิทิน (ม.ค.–ธ.ค.) — ดึงจากเดือนภาษี (VATPRD) ใน Express
          </p>
        </div>
      </Card>

      {tab === 'report' && <VatReportTab companyId={companyId} year={year} />}
      {tab === 'filing' && <Pp30FilingTab companyId={companyId} year={year} />}
      {tab === 'entries' && <VatEntriesTab companyId={companyId} year={year} />}
    </div>
  )
}
