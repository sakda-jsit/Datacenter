import { useState } from 'react'
import PageHeader from '../../../shared/components/ui/PageHeader'
import Tabs from '../../../shared/components/ui/Tabs'
import Card from '../../../shared/components/ui/Card'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import AssetsTab from './tabs/AssetsTab'
import WorkpaperTab from './tabs/WorkpaperTab'

type Tab = 'assets' | 'workpaper'

const TABS: { key: Tab; label: string }[] = [
  { key: 'assets', label: 'ทะเบียนสินทรัพย์' },
  { key: 'workpaper', label: 'กระดาษทำการ + ปรับปรุง' },
]

export default function FixedAssetsPage() {
  const currentYear = new Date().getFullYear()
  const { companyId } = useCurrentCompany()
  const [tab, setTab] = useState<Tab>('assets')
  const [year, setYear] = useState(currentYear)

  return (
    <div>
      <PageHeader
        title="สินทรัพย์ถาวร"
        description="ทะเบียนสินทรัพย์ + ค่าเสื่อมราคา 2 ชุด (บัญชี/ภาษี) + จำหน่าย/ขาย และสร้างรายการปรับปรุงค่าเสื่อมเข้า TB"
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
          <p className="pb-2 text-xs text-gray-400">
            ปีบัญชีใช้คำนวณค่าเสื่อมสะสม/มูลค่าสุทธิสิ้นปี และค่าเสื่อมที่รับรู้ในปี
          </p>
        </div>
      </Card>

      {tab === 'assets' && <AssetsTab companyId={companyId} fiscalYear={year} />}
      {tab === 'workpaper' && <WorkpaperTab companyId={companyId} fiscalYear={year} />}
    </div>
  )
}
