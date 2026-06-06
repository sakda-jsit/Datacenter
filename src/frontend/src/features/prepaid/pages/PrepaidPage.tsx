import { useState } from 'react'
import PageHeader from '../../../shared/components/ui/PageHeader'
import Tabs from '../../../shared/components/ui/Tabs'
import Card from '../../../shared/components/ui/Card'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import PrepaidListTab from './tabs/PrepaidListTab'
import PrepaidWorkpaperTab from './tabs/PrepaidWorkpaperTab'

type Tab = 'list' | 'workpaper'

const TABS: { key: Tab; label: string }[] = [
  { key: 'list', label: 'รายการ' },
  { key: 'workpaper', label: 'กระดาษทำการ + ปรับปรุง' },
]

export default function PrepaidPage() {
  const currentYear = new Date().getFullYear()
  const { companyId } = useCurrentCompany()
  const [tab, setTab] = useState<Tab>('list')
  const [year, setYear] = useState(currentYear)

  return (
    <div>
      <PageHeader
        title="ค่าใช้จ่ายจ่ายล่วงหน้า"
        description="ตัดจ่ายตามวันเริ่ม–สิ้นสุด (เส้นตรงตามวัน) + เทียบ GL และสร้างรายการปรับปรุงตัดจ่ายเข้า TB"
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
          <p className="pb-2 text-xs text-gray-400">ปีบัญชีใช้คำนวณตัดจ่ายสะสม/คงเหลือสิ้นปี และยอดตัดจ่ายที่รับรู้ในปี</p>
        </div>
      </Card>

      {tab === 'list' && <PrepaidListTab companyId={companyId} fiscalYear={year} />}
      {tab === 'workpaper' && <PrepaidWorkpaperTab companyId={companyId} fiscalYear={year} />}
    </div>
  )
}
