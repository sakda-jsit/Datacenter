import { useState } from 'react'
import PageHeader from '../../../shared/components/ui/PageHeader'
import Tabs from '../../../shared/components/ui/Tabs'
import Card from '../../../shared/components/ui/Card'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import InterestIncomeListTab from './tabs/InterestIncomeListTab'
import InterestIncomeWorkpaperTab from './tabs/InterestIncomeWorkpaperTab'

type Tab = 'list' | 'workpaper'

const TABS: { key: Tab; label: string }[] = [
  { key: 'list', label: 'เงินให้กู้' },
  { key: 'workpaper', label: 'กระดาษทำการ + ปรับปรุง' },
]

export default function InterestIncomePage() {
  const currentYear = new Date().getFullYear()
  const { companyId } = useCurrentCompany()
  const [tab, setTab] = useState<Tab>('list')
  const [year, setYear] = useState(currentYear)

  return (
    <div>
      <PageHeader
        title="ดอกเบี้ยรับเงินให้กู้"
        description="คำนวณดอกเบี้ยรับตามยอดเงินต้นคงเหลือ × อัตรา × วัน/ปี + ภาษีธุรกิจเฉพาะ/ส่วนท้องถิ่น + เทียบ GL และสร้างรายการปรับปรุงเข้า TB"
      />

      <Tabs items={TABS} activeKey={tab} onChange={setTab} />

      <Card className="mb-5 p-4">
        <div className="flex flex-wrap items-end gap-3">
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ปีบัญชี (AD)</label>
            <input
              type="number" value={year} min={2000} max={2100}
              onChange={(e) => setYear(Number(e.target.value))}
              className="w-24 rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
            />
          </div>
          <p className="pb-2 text-xs text-gray-400">ปีบัญชีใช้คำนวณดอกเบี้ยรับที่รับรู้ในปี (ตามยอดเงินต้นคงเหลือรายช่วง)</p>
        </div>
      </Card>

      {tab === 'list' && <InterestIncomeListTab companyId={companyId} fiscalYear={year} />}
      {tab === 'workpaper' && <InterestIncomeWorkpaperTab companyId={companyId} fiscalYear={year} />}
    </div>
  )
}
