import { useEffect, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import Card from '../../../shared/components/ui/Card'
import PageHeader from '../../../shared/components/ui/PageHeader'
import Tabs from '../../../shared/components/ui/Tabs'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import { useAdjustedTrialBalance } from '../hooks/useAdjustments'
import AdjustedTrialBalanceTab from './tabs/AdjustedTrialBalanceTab'
import AdjustmentEntriesTab from './tabs/AdjustmentEntriesTab'

type Tab = 'tb' | 'entries'

const TABS: { key: Tab; label: string }[] = [
  { key: 'tb', label: 'งบทดลองหลังปรับปรุง' },
  { key: 'entries', label: 'รายการปรับปรุง' },
]

export default function AdjustmentsPage() {
  const currentYear = new Date().getFullYear()
  const { companyId } = useCurrentCompany()
  const [tab, setTab] = useState<Tab>('tb')
  const [year, setYear] = useState(currentYear)
  const [includeZero, setIncludeZero] = useState(false)
  const [queried, setQueried] = useState(false)

  const tbQuery = useAdjustedTrialBalance(companyId, year, includeZero, queried && tab === 'tb')

  useEffect(() => {
    setQueried(false)
  }, [companyId])

  return (
    <div>
      <PageHeader title="กระดาษทำการปิดงบ" description="งบทดลองหลังปรับปรุงและรายการปรับปรุง" />

      <Tabs items={TABS} activeKey={tab} onChange={setTab} />

      {/* Year filter — shared by both tabs */}
      <Card className="mb-5 p-4">
        <div className="flex flex-wrap items-end gap-3">
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ปีบัญชี (AD)</label>
            <input
              type="number" value={year} min={2000} max={2100}
              onChange={(e) => { setYear(Number(e.target.value)); setQueried(false) }}
              className="w-24 rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
            />
          </div>
          {tab === 'tb' && (
            <>
              <label className="flex items-center gap-2 pb-2 text-sm text-gray-600">
                <input type="checkbox" checked={includeZero} onChange={(e) => { setIncludeZero(e.target.checked); setQueried(false) }} className="rounded" />
                รวมบัญชียอดศูนย์
              </label>
              <Button type="button" onClick={() => companyId && setQueried(true)} disabled={!companyId || tbQuery.isLoading}>
                {tbQuery.isLoading ? 'กำลังโหลด...' : 'แสดงรายงาน'}
              </Button>
            </>
          )}
        </div>
      </Card>

      {tab === 'tb' && (
        <AdjustedTrialBalanceTab
          data={tbQuery.data}
          isLoading={tbQuery.isLoading}
          isError={tbQuery.isError}
          queried={queried}
          companyId={companyId}
        />
      )}
      {tab === 'entries' && <AdjustmentEntriesTab companyId={companyId} fiscalYear={year} />}
    </div>
  )
}
