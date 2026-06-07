import { useState } from 'react'
import PageHeader from '../../../shared/components/ui/PageHeader'
import Tabs from '../../../shared/components/ui/Tabs'
import Card from '../../../shared/components/ui/Card'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import CashCountListTab from './tabs/CashCountListTab'
import CashCountWorkpaperTab from './tabs/CashCountWorkpaperTab'

type Tab = 'list' | 'workpaper'

const TABS: { key: Tab; label: string }[] = [
  { key: 'list', label: 'ใบตรวจนับ' },
  { key: 'workpaper', label: 'กระดาษทำการ + ปรับปรุง' },
]

export default function CashCountPage() {
  const currentYear = new Date().getFullYear()
  const { companyId } = useCurrentCompany()
  const [tab, setTab] = useState<Tab>('list')
  const [year, setYear] = useState(currentYear)

  return (
    <div>
      <PageHeader
        title="ตรวจนับเงินสด"
        description="บันทึกชนิดธนบัตร/เหรียญ × จำนวน = มูลค่า แล้วเทียบยอดนับจริงกับบัญชีเงินสดใน GL + สร้างรายการปรับปรุงเงินสดขาด/เกิน"
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
          <p className="pb-2 text-xs text-gray-400">ใบตรวจนับและกระดาษทำการแยกตามปีบัญชี</p>
        </div>
      </Card>

      {tab === 'list' && <CashCountListTab companyId={companyId} fiscalYear={year} />}
      {tab === 'workpaper' && <CashCountWorkpaperTab companyId={companyId} fiscalYear={year} />}
    </div>
  )
}
