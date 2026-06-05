import { useState } from 'react'
import PageHeader from '../../../shared/components/ui/PageHeader'
import Tabs from '../../../shared/components/ui/Tabs'
import Card from '../../../shared/components/ui/Card'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import ValuationTab from './tabs/ValuationTab'
import ItemsTab from './tabs/ItemsTab'

type Tab = 'valuation' | 'items'

const TABS: { key: Tab; label: string }[] = [
  { key: 'valuation', label: 'มูลค่า / เทียบ GL' },
  { key: 'items', label: 'รายการสินค้า' },
]

export default function StockPage() {
  const currentYear = new Date().getFullYear()
  const { companyId } = useCurrentCompany()
  const [tab, setTab] = useState<Tab>('valuation')
  const [year, setYear] = useState(currentYear)

  return (
    <div>
      <PageHeader
        title="สินค้าคงคลัง"
        description="มูลค่าสินค้าคงเหลือจาก Express (STMAS) + เทียบกับบัญชีสินค้าคงเหลือใน GL (FG ↔ TB)"
      />

      <Tabs items={TABS} activeKey={tab} onChange={setTab} />

      {tab === 'valuation' && (
        <Card className="mb-5 p-4">
          <div className="flex flex-wrap items-end gap-3">
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">ปีบัญชี (AD)</label>
              <input
                type="number" value={year} min={2000} max={2100}
                onChange={(e) => setYear(Number(e.target.value))}
                className="w-28 rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
              />
            </div>
            <p className="pb-2 text-xs text-gray-400">
              ปีบัญชีใช้ดึงยอดบัญชีสินค้าคงเหลือใน GL ณ สิ้นปีมาเทียบ (ยอดสินค้าใน STMAS เป็นยอดปัจจุบัน)
            </p>
          </div>
        </Card>
      )}

      {tab === 'valuation' && <ValuationTab companyId={companyId} fiscalYear={year} />}
      {tab === 'items' && <ItemsTab companyId={companyId} />}
    </div>
  )
}
