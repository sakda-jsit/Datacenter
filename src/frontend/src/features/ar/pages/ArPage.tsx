import { useEffect, useState } from 'react'
import PageHeader from '../../../shared/components/ui/PageHeader'
import Tabs from '../../../shared/components/ui/Tabs'
import Card from '../../../shared/components/ui/Card'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import { useArYears } from '../hooks/useAr'
import AgingTab from './tabs/AgingTab'
import InvoicesTab from './tabs/InvoicesTab'
import CustomersTab from './tabs/CustomersTab'

type Tab = 'aging' | 'invoices' | 'customers'

const TABS: { key: Tab; label: string }[] = [
  { key: 'aging', label: 'อายุหนี้' },
  { key: 'invoices', label: 'ใบแจ้งหนี้' },
  { key: 'customers', label: 'ลูกค้า' },
]

function todayIso() {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

export default function ArPage() {
  const currentYear = new Date().getFullYear()
  const { companyId } = useCurrentCompany()
  const [tab, setTab] = useState<Tab>('aging')
  const [year, setYear] = useState(currentYear)
  const [asOf, setAsOf] = useState(todayIso())
  const { data: years } = useArYears(companyId)

  useEffect(() => {
    if (years && years.length > 0 && !years.includes(year)) setYear(years[0])
  }, [years]) // eslint-disable-line react-hooks/exhaustive-deps

  return (
    <div>
      <PageHeader
        title="ลูกหนี้การค้า"
        description="รายงานอายุหนี้ ใบแจ้งหนี้ และรายชื่อลูกค้า จาก Express (ARMAS/ARTRN)"
      />

      <Tabs items={TABS} activeKey={tab} onChange={setTab} />

      <Card className="mb-5 p-4">
        <div className="flex flex-wrap items-end gap-4">
          {tab === 'aging' && (
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">ณ วันที่</label>
              <input
                type="date" value={asOf} onChange={(e) => setAsOf(e.target.value)}
                className="rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
              />
            </div>
          )}
          {tab === 'invoices' && (
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">ปีเอกสาร (AD)</label>
              {years && years.length > 0 ? (
                <select value={year} onChange={(e) => setYear(Number(e.target.value))} className="w-28 rounded border border-gray-300 px-3 py-2 text-sm">
                  {years.includes(year) ? null : <option value={year}>{year}</option>}
                  {years.map((y) => <option key={y} value={y}>{y}</option>)}
                </select>
              ) : (
                <input type="number" value={year} min={2000} max={2100} onChange={(e) => setYear(Number(e.target.value))} className="w-28 rounded border border-gray-300 px-3 py-2 text-sm" />
              )}
            </div>
          )}
          <p className="pb-2 text-xs text-gray-400">
            {tab === 'aging' && 'อายุหนี้คำนวณจากวันครบกำหนดชำระเทียบกับวันที่เลือก'}
            {tab === 'invoices' && 'ใบแจ้งหนี้ (RECTYP=IV) นำเข้าจาก ARTRN'}
            {tab === 'customers' && 'ลูกค้านำเข้าจาก ARMAS — ยอดค้างรวมจากใบแจ้งหนี้ที่ยังไม่ปิด'}
          </p>
        </div>
      </Card>

      {tab === 'aging' && <AgingTab companyId={companyId} asOf={asOf} />}
      {tab === 'invoices' && <InvoicesTab companyId={companyId} year={year} />}
      {tab === 'customers' && <CustomersTab companyId={companyId} />}
    </div>
  )
}
