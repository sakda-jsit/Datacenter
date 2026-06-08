import { useEffect, useState } from 'react'
import PageHeader from '../../../shared/components/ui/PageHeader'
import Tabs from '../../../shared/components/ui/Tabs'
import Card from '../../../shared/components/ui/Card'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import { useBankYears } from '../hooks/useBank'
import BankBookTab from './tabs/BankBookTab'
import AccountsTab from './tabs/AccountsTab'
import ReconciliationTab from './tabs/ReconciliationTab'

type Tab = 'book' | 'accounts' | 'recon'

const TABS: { key: Tab; label: string }[] = [
  { key: 'book', label: 'สมุดเงินฝาก' },
  { key: 'accounts', label: 'บัญชีธนาคาร' },
  { key: 'recon', label: 'กระทบยอด (Reconciliation)' },
]

export default function BankReconciliationPage() {
  const currentYear = new Date().getFullYear()
  const { companyId } = useCurrentCompany()
  const [tab, setTab] = useState<Tab>('book')
  const [year, setYear] = useState(currentYear)
  const { data: years } = useBankYears(companyId)

  useEffect(() => {
    if (years && years.length > 0 && !years.includes(year)) setYear(years[0])
  }, [years]) // eslint-disable-line react-hooks/exhaustive-deps

  return (
    <div>
      <PageHeader
        title="ธนาคาร / สมุดเงินฝาก"
        description="สมุดเงินฝากธนาคาร (รายการเดินบัญชี + ยอดคงเหลือสะสม) จาก Express (BKMAS/BKTRN)"
      />

      <Tabs items={TABS} activeKey={tab} onChange={setTab} />

      {tab === 'book' && (
        <Card className="mb-5 p-4">
          <div className="flex flex-wrap items-end gap-3">
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">ปีบัญชี (AD)</label>
              {years && years.length > 0 ? (
                <select value={year} onChange={(e) => setYear(Number(e.target.value))} className="w-28 rounded border border-gray-300 px-3 py-2 text-sm">
                  {years.includes(year) ? null : <option value={year}>{year}</option>}
                  {years.map((y) => <option key={y} value={y}>{y}</option>)}
                </select>
              ) : (
                <input type="number" value={year} min={2000} max={2100} onChange={(e) => setYear(Number(e.target.value))} className="w-28 rounded border border-gray-300 px-3 py-2 text-sm" />
              )}
            </div>
            <p className="pb-2 text-xs text-gray-400">
              ยอดยกมาต้นปี = ยอดยกมาบัญชี (Express) + เคลื่อนไหวสุทธิก่อนปีที่เลือก · กระทบยอด statement จริงที่แท็บ "กระทบยอด"
            </p>
          </div>
        </Card>
      )}

      {tab === 'book' && <BankBookTab companyId={companyId} year={year} />}
      {tab === 'accounts' && <AccountsTab companyId={companyId} />}
      {tab === 'recon' && <ReconciliationTab companyId={companyId} />}
    </div>
  )
}
