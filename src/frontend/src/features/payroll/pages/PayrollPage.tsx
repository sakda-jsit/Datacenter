import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import PageHeader from '../../../shared/components/ui/PageHeader'
import Tabs from '../../../shared/components/ui/Tabs'
import Card from '../../../shared/components/ui/Card'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import EmployeesTab from './tabs/EmployeesTab'
import PayrollRunsTab from './tabs/PayrollRunsTab'
import YearSummaryTab from './tabs/YearSummaryTab'
import AccountMappingTab from './tabs/AccountMappingTab'
import DashboardTab from './tabs/DashboardTab'

type Tab = 'dashboard' | 'employees' | 'runs' | 'year' | 'mapping'
const TABS: { key: Tab; label: string }[] = [
  { key: 'dashboard', label: 'ภาพรวม / กระทบยอด' },
  { key: 'employees', label: 'ทะเบียนพนักงาน' },
  { key: 'runs', label: 'งวดเงินเดือน' },
  { key: 'year', label: 'รายได้ทั้งปี' },
  { key: 'mapping', label: 'แมพบัญชีเงินเดือน' },
]

// deep-link จากเมนูข้าง: ?section=pnd1 → แท็บรายได้ทั้งปี (ภ.ง.ด.1), ?section=sso → แท็บงวดเงินเดือน (สปส.1-10)
const SECTION_TAB: Record<string, Tab> = { pnd1: 'year', sso: 'runs' }

export default function PayrollPage() {
  const { companyId } = useCurrentCompany()
  const [searchParams] = useSearchParams()
  const section = searchParams.get('section') ?? ''
  const [tab, setTab] = useState<Tab>(SECTION_TAB[section] ?? 'dashboard')

  // เปลี่ยนแท็บเมื่อคลิกเมนูข้าง (section เปลี่ยน) ขณะอยู่หน้า payroll อยู่แล้ว (component ไม่ remount)
  useEffect(() => {
    if (section && SECTION_TAB[section]) setTab(SECTION_TAB[section])
  }, [section])

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
          {tab === 'dashboard' && <DashboardTab companyId={companyId} />}
          {tab === 'employees' && <EmployeesTab companyId={companyId} />}
          {tab === 'runs' && <PayrollRunsTab companyId={companyId} />}
          {tab === 'year' && <YearSummaryTab companyId={companyId} />}
          {tab === 'mapping' && <AccountMappingTab companyId={companyId} />}
        </>
      )}
    </div>
  )
}
