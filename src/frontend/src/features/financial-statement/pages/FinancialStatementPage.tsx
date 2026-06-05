import { useEffect, useState } from 'react'
import ReportFilterBar from '../../../shared/components/report/ReportFilterBar'
import PageHeader from '../../../shared/components/ui/PageHeader'
import Tabs from '../../../shared/components/ui/Tabs'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import { useBalanceSheet, useEquityChanges, useProfitLoss } from '../hooks/useFinancialStatement'
import BalanceSheetTab from './tabs/BalanceSheetTab'
import ProfitLossTab from './tabs/ProfitLossTab'
import EquityChangesTab from './tabs/EquityChangesTab'
import NotesTab from './tabs/NotesTab'
import MappingTab from './tabs/MappingTab'

type Tab = 'pl' | 'bs' | 'cap' | 'notes' | 'mapping'

const TABS: { key: Tab; label: string }[] = [
  { key: 'pl', label: 'งบกำไรขาดทุน' },
  { key: 'bs', label: 'งบแสดงฐานะการเงิน' },
  { key: 'cap', label: 'งบส่วนผู้ถือหุ้น' },
  { key: 'notes', label: 'หมายเหตุประกอบงบ (NOTE2)' },
  { key: 'mapping', label: 'จัดการ Mapping' },
]

export default function FinancialStatementPage() {
  const currentYear = new Date().getFullYear()
  const [tab, setTab] = useState<Tab>('pl')
  const { companyId } = useCurrentCompany()
  const [year, setYear] = useState(currentYear)
  const [monthFrom, setMonthFrom] = useState(1)
  const [monthTo, setMonthTo] = useState(12)
  const [queried, setQueried] = useState(false)

  const bsQuery = useBalanceSheet(
    { clientCompanyId: companyId, fiscalYear: year },
    queried && tab === 'bs',
  )
  const plQuery = useProfitLoss(
    { clientCompanyId: companyId, fiscalYear: year, monthFrom, monthTo },
    queried && tab === 'pl',
  )
  const capQuery = useEquityChanges(
    { clientCompanyId: companyId, fiscalYear: year },
    queried && tab === 'cap',
  )

  function handleSearch() {
    if (companyId) setQueried(true)
  }

  function handleClientChange() {
    setQueried(false)
  }

  useEffect(() => {
    setQueried(false)
  }, [companyId])

  return (
    <div>
      <PageHeader title="งบการเงิน" />

      <Tabs
        items={TABS}
        activeKey={tab}
        onChange={(key) => {
          setTab(key)
          setQueried(false)
        }}
      />

      {/* Filter bar — report tabs only */}
      {tab !== 'mapping' && (
        <ReportFilterBar
          clients={[]}
          clientId={companyId}
          year={year}
          monthFrom={monthFrom}
          monthTo={monthTo}
          onClientChange={handleClientChange}
          onYearChange={(y) => { setYear(y); setQueried(false) }}
          onMonthFromChange={setMonthFrom}
          onMonthToChange={setMonthTo}
          onSearch={handleSearch}
          loading={bsQuery.isLoading || plQuery.isLoading || capQuery.isLoading}
        />
      )}

      {tab === 'pl' && (
        <ProfitLossTab
          data={plQuery.data}
          isLoading={plQuery.isLoading}
          isError={plQuery.isError}
          queried={queried}
          clientId={companyId}
          fiscalYear={year}
        />
      )}
      {tab === 'bs' && (
        <BalanceSheetTab
          data={bsQuery.data}
          isLoading={bsQuery.isLoading}
          isError={bsQuery.isError}
          queried={queried}
        />
      )}
      {tab === 'cap' && (
        <EquityChangesTab
          data={capQuery.data}
          isLoading={capQuery.isLoading}
          isError={capQuery.isError}
          queried={queried}
        />
      )}
      {tab === 'notes' && (
        <NotesTab companyId={companyId} fiscalYear={year} queried={queried} />
      )}
      {tab === 'mapping' && (
        <MappingTab
          clientId={companyId}
          clients={[]}
          onClientChange={handleClientChange}
        />
      )}
    </div>
  )
}
