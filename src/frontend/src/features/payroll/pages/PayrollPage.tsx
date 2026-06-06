import { useState } from 'react'
import PageHeader from '../../../shared/components/ui/PageHeader'
import Tabs from '../../../shared/components/ui/Tabs'
import Card from '../../../shared/components/ui/Card'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import EmployeesTab from './tabs/EmployeesTab'
import PayrollRunsTab from './tabs/PayrollRunsTab'
import AccountMappingTab from './tabs/AccountMappingTab'

type Tab = 'employees' | 'runs' | 'mapping'
const TABS: { key: Tab; label: string }[] = [
  { key: 'employees', label: 'ทะเบียนพนักงาน' },
  { key: 'runs', label: 'งวดเงินเดือน' },
  { key: 'mapping', label: 'แมพบัญชีเงินเดือน' },
]

export default function PayrollPage() {
  const { companyId } = useCurrentCompany()
  const [tab, setTab] = useState<Tab>('employees')

  return (
    <div>
      <PageHeader
        title="เงินเดือน / ประกันสังคม"
        description="ทะเบียนพนักงาน + คลังหลักฐาน (PDPA) + งวดเงินเดือนรายเดือน (คำนวณ ปกส./ภาษีเทียบ)"
      />
      <Tabs items={TABS} activeKey={tab} onChange={setTab} />
      {!companyId ? (
        <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>
      ) : (
        <>
          {tab === 'employees' && <EmployeesTab companyId={companyId} />}
          {tab === 'runs' && <PayrollRunsTab companyId={companyId} />}
          {tab === 'mapping' && <AccountMappingTab companyId={companyId} />}
        </>
      )}
    </div>
  )
}
